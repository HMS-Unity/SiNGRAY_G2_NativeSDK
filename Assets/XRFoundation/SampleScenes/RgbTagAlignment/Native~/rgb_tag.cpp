#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/calib3d.hpp>
#include <opencv2/objdetect/aruco_detector.hpp>
#include <cmath>
#include <vector>
#include <cstring>

#if defined(_WIN32)
#define EXPORT extern "C" __declspec(dllexport)
#else
#define EXPORT extern "C" __attribute__((visibility("default")))
#endif

// ABI 1. No vendor tag detector/pose API is used. All images are top-left RGBA.
// output: [0] status, [1] corner RMSE px, [2] alternate RMSE px (-1 absent),
// [3..11] object-to-OpenCV-camera rotation (row major), [12..14] translation m,
// [15..22] measured TL/TR/BR/BL corners, [23..30] reprojected corners.
// Status: 1 found; 0 ID 0 absent; -1 invalid input; -2 exception;
// -3 ambiguous duplicate ID 0; -4 invalid/behind-camera pose.
EXPORT int rta_abi_version() { return 1; }
EXPORT int rta_detect(const unsigned char* rgba, int bytes, int width, int height,
                      const double* k, double edge, double* output, int capacity) {
    if (!output || capacity < 31) return -1;
    std::memset(output, 0, sizeof(double) * capacity);
    output[2] = -1;
    if (!rgba || !k || width < 16 || height < 16 || width > 4096 || height > 4096 ||
        bytes != static_cast<long long>(width) * height * 4 || !std::isfinite(edge) || edge <= 0) {
        output[0] = -1; return -1;
    }
    for (int i = 0; i < 9; ++i) if (!std::isfinite(k[i])) { output[0] = -1; return -1; }
    if (k[0] <= 0 || k[1] <= 0) { output[0] = -1; return -1; }
    try {
        cv::Mat image(height, width, CV_8UC4, const_cast<unsigned char*>(rgba));
        cv::Mat gray; cv::cvtColor(image, gray, cv::COLOR_RGBA2GRAY);
        auto dictionary = cv::aruco::getPredefinedDictionary(cv::aruco::DICT_APRILTAG_36h11);
        cv::aruco::DetectorParameters params;
        params.cornerRefinementMethod = cv::aruco::CORNER_REFINE_SUBPIX;
        cv::aruco::ArucoDetector detector(dictionary, params);
        std::vector<std::vector<cv::Point2f>> corners;
        std::vector<int> ids;
        detector.detectMarkers(gray, corners, ids);
        int selected = -1;
        for (size_t i = 0; i < ids.size(); ++i) if (ids[i] == 0) {
            if (selected >= 0) { output[0] = -3; return -3; }
            selected = static_cast<int>(i);
        }
        if (selected < 0) return 0;
        const auto& measured = corners[selected];
        cv::Matx33d camera(k[0],0,k[2], 0,k[1],k[3], 0,0,1);
        cv::Vec<double,5> distortion(k[4],k[5],k[6],k[7],k[8]);
        const float half = static_cast<float>(edge * 0.5);
        // Required IPPE_SQUARE order. Tag x right, y up, z toward camera.
        std::vector<cv::Point3f> object = {{-half,half,0},{half,half,0},{half,-half,0},{-half,-half,0}};
        std::vector<cv::Mat> rotations, translations;
        cv::solvePnPGeneric(object, measured, camera, distortion, rotations, translations,
                            false, cv::SOLVEPNP_IPPE_SQUARE);
        int best = -1; double bestError = 1e100, secondError = 1e100;
        cv::Mat bestR; std::vector<cv::Point2f> bestProjection;
        for (size_t i = 0; i < rotations.size(); ++i) {
            cv::Mat r; cv::Rodrigues(rotations[i], r);
            const cv::Mat& t = translations[i]; bool valid = cv::checkRange(r) && cv::checkRange(t);
            for (const auto& p : object)
                valid = valid && (r.at<double>(2,0)*p.x+r.at<double>(2,1)*p.y+t.at<double>(2)) > 0;
            if (!valid) continue;
            std::vector<cv::Point2f> projected;
            cv::projectPoints(object, rotations[i], t, camera, distortion, projected);
            double sum = 0;
            for (int j = 0; j < 4; ++j) { auto d = projected[j]-measured[j]; sum += d.dot(d); }
            double error = std::sqrt(sum / 4);
            if (!std::isfinite(error)) continue;
            if (error < bestError) {
                secondError = bestError; bestError = error; best = static_cast<int>(i);
                bestR = r; bestProjection = projected;
            } else if (error < secondError) secondError = error;
        }
        if (best < 0) { output[0] = -4; return -4; }
        output[0] = 1; output[1] = bestError;
        output[2] = secondError < 1e99 ? secondError : -1;
        for (int i = 0; i < 9; ++i) output[3+i] = bestR.at<double>(i/3,i%3);
        for (int i = 0; i < 3; ++i) output[12+i] = translations[best].at<double>(i);
        for (int i = 0; i < 4; ++i) {
            output[15+2*i] = measured[i].x; output[16+2*i] = measured[i].y;
            output[23+2*i] = bestProjection[i].x; output[24+2*i] = bestProjection[i].y;
        }
        return 1;
    } catch (...) { output[0] = -2; return -2; }
}

// Project a locked tag into a later RGB frame, including lens distortion.
// rowMajor is tag-local (same IPPE basis above) to OpenCV-camera 3x3.
EXPORT int rta_project(const double* k, const double* rowMajor, const double* translation,
                       double edge, double* pixels) {
    if (!k || !rowMajor || !translation || !pixels || !std::isfinite(edge) || edge <= 0) return -1;
    try {
        cv::Mat r(3,3,CV_64F,const_cast<double*>(rowMajor)); cv::Mat rv; cv::Rodrigues(r,rv);
        cv::Vec3d t(translation[0],translation[1],translation[2]);
        cv::Matx33d camera(k[0],0,k[2],0,k[1],k[3],0,0,1);
        cv::Vec<double,5> d(k[4],k[5],k[6],k[7],k[8]);
        float h = static_cast<float>(edge/2);
        std::vector<cv::Point3f> object={{-h,h,0},{h,h,0},{h,-h,0},{-h,-h,0}};
        for (auto p:object) if (r.at<double>(2,0)*p.x+r.at<double>(2,1)*p.y+t[2] <= 0) return 0;
        std::vector<cv::Point2f> output; cv::projectPoints(object,rv,t,camera,d,output);
        for (int i=0;i<4;++i) { pixels[2*i]=output[i].x; pixels[2*i+1]=output[i].y; }
        return cv::checkRange(cv::Mat(output)) ? 1 : 0;
    } catch (...) { return -2; }
}
