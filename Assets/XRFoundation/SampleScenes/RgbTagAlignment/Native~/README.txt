Independent RGB tag bridge ABI 1

Inputs: tightly packed top-left-origin RGBA32; positive image width/height;
k = [fx, fy, cx, cy, k1, k2, p1, p2, k3]; detection edge in metres (sample 0.08).
Only DICT_APRILTAG_36h11 / ID 0 is accepted. Multiple visible ID 0 tags are rejected.
No xslam tag detector/pose interface is linked or called.

rta_detect output (31 doubles):
0 status: 1 found, 0 absent, -1 bad input, -2 exception,
          -3 duplicate ID0, -4 invalid/behind-camera pose.
1 selected corner RMSE pixels; 2 alternate candidate RMSE or -1.
3..11 row-major object-to-OpenCV-camera rotation.
12..14 camera-relative translation metres.
15..22 measured canonical TL/TR/BR/BL image corners (x,y).
23..30 corresponding reprojected corners including distortion.
Object basis/order follows OpenCV SOLVEPNP_IPPE_SQUARE.

rta_project accepts the same calibration and object-to-OpenCV-camera R/t and
returns the four distorted image corners, rejecting corners behind the camera.
The C# wrapper explicitly converts both camera and object bases; do not just
flip one quaternion component or add an unrotated camera offset.

Android links the SDK's existing ARM64 libopencv_java4.so and libc++_shared.so.
Use matching OpenCV 4.10.0 public headers. The build helper checks the source
archive SHA256 and links with --no-undefined. A successful link is not a G2 load
or accuracy test. Windows uses a separate minimal static OpenCV 4.10.0 build.
