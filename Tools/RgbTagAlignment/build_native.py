"""Rebuild the optional RGB alignment sample bridge; never replace vendor libraries."""
import argparse, hashlib, json, pathlib, subprocess, zipfile, urllib.request

ROOT = pathlib.Path(__file__).resolve().parents[2]
WORK = ROOT / 'Builds/RgbTagAlignment_20261116/native'
SAMPLE = ROOT / 'Assets/XRFoundation/SampleScenes/RgbTagAlignment'
SOURCE = WORK / 'opencv-4.10.0'
UNITY = pathlib.Path('D:/Unity/2022.3.62f3/Editor')

def source():
    WORK.mkdir(parents=True, exist_ok=True)
    archive=WORK/'opencv-4.10.0.zip'
    if not archive.exists(): urllib.request.urlretrieve('https://codeload.github.com/opencv/opencv/zip/refs/tags/4.10.0',archive)
    if hashlib.sha256(archive.read_bytes()).hexdigest()!='3810bca2b1d1c572912df0ac3888126341f3762dfd28e91068c805fb656d0e51':
        raise RuntimeError('OpenCV source archive checksum mismatch')
    if not (SOURCE/'CMakeLists.txt').exists():
        with zipfile.ZipFile(archive) as z:
            for item in z.namelist():
                if not (WORK/item).resolve().is_relative_to(WORK.resolve()): raise RuntimeError('Invalid archive path')
            z.extractall(WORK)

def android():
    source()
    WORK.mkdir(parents=True, exist_ok=True)
    aar = ROOT / 'Assets/XRFoundation/Core/Sdk/Xslam/Plugins/Android/xvsdk-release.aar'
    with zipfile.ZipFile(aar) as z:
        for name in ['libopencv_java4.so', 'libc++_shared.so']:
            (WORK / name).write_bytes(z.read('jni/arm64-v8a/' + name))
    clang = UNITY / 'Data/PlaybackEngines/AndroidPlayer/NDK/toolchains/llvm/prebuilt/windows-x86_64/bin/clang++.exe'
    output = SAMPLE / 'Plugins/Android/arm64-v8a/libsingray_rgb_tag.so'
    output.parent.mkdir(parents=True, exist_ok=True)
    args = [str(clang), '--target=aarch64-linux-android26', '-std=c++17', '-O2', '-fPIC', '-fvisibility=hidden', '-shared']
    generated = WORK / 'include/opencv2'; generated.mkdir(parents=True, exist_ok=True)
    (generated / 'opencv_modules.hpp').write_text('#pragma once\n' + ''.join(
        '#define HAVE_OPENCV_' + module.upper() + '\n' for module in ['core','imgproc','calib3d','features2d','flann','objdetect']), encoding='utf8')
    args += ['-I', str(WORK / 'include')]
    for module in ['core','imgproc','calib3d','features2d','flann','objdetect']:
        args += ['-I', str(SOURCE / 'modules' / module / 'include')]
    args += [str(SAMPLE / 'Native~/rgb_tag.cpp'), '-L', str(WORK), '-lopencv_java4', '-nostdlib++', '-lc++_shared',
             '-Wl,--no-undefined', '-Wl,-z,max-page-size=16384', '-Wl,-soname,libsingray_rgb_tag.so', '-o', str(output)]
    run = subprocess.run(args, capture_output=True, text=True)
    (WORK / 'android-compile.txt').write_text(run.stdout + run.stderr, encoding='utf8')
    if run.returncode: raise RuntimeError(run.stderr)
    print(json.dumps({'output':str(output), 'bytes':output.stat().st_size,
                      'sha256':hashlib.sha256(output.read_bytes()).hexdigest()}, indent=2))

def windows():
    source()
    vs = pathlib.Path('C:/Program Files/Microsoft Visual Studio/18/Community')
    cmake = vs / 'Common7/IDE/CommonExtensions/Microsoft/CMake/CMake/bin/cmake.exe'
    ninja = vs / 'Common7/IDE/CommonExtensions/Microsoft/CMake/Ninja/ninja.exe'
    build = WORK / 'win-opencv'
    bridge = WORK / 'win-bridge'
    commands = [f'call "{vs}/VC/Auxiliary/Build/vcvars64.bat"',
      f'"{cmake}" -S "{SOURCE}" -B "{build}" -G Ninja -DCMAKE_MAKE_PROGRAM="{ninja}" '
      '-DCMAKE_BUILD_TYPE=Release -DBUILD_SHARED_LIBS=OFF -DBUILD_LIST=core,imgproc,calib3d,features2d,flann,objdetect '
      '-DBUILD_TESTS=OFF -DBUILD_PERF_TESTS=OFF -DBUILD_EXAMPLES=OFF -DBUILD_opencv_apps=OFF '
      '-DWITH_IPP=OFF -DWITH_OPENCL=OFF -DWITH_ITT=OFF -DWITH_FFMPEG=OFF -DWITH_MSMF=OFF '
      '-DBUILD_JAVA=OFF -DBUILD_opencv_python3=OFF -DOPENCV_DNN_OPENCL=OFF -DCV_ENABLE_INTRINSICS=ON',
      f'"{cmake}" --build "{build}" --parallel 8',
      f'"{cmake}" -S "{SAMPLE}/Native~" -B "{bridge}" -G Ninja -DCMAKE_MAKE_PROGRAM="{ninja}" '
      f'-DCMAKE_BUILD_TYPE=Release -DOpenCV_DIR="{build}"',
      f'"{cmake}" --build "{bridge}" --parallel 8']
    script=WORK/'windows-build.cmd'
    script.write_text('@echo off\n'+'\nif errorlevel 1 exit /b 1\n'.join(commands)+'\n', encoding='utf8')
    with (WORK/'windows-compile.txt').open('w',encoding='utf8') as log:
        result=subprocess.run(['cmd.exe','/c',str(script)],stdout=log,stderr=subprocess.STDOUT)
    if result.returncode: raise RuntimeError('See '+str(WORK/'windows-compile.txt'))
    import shutil
    output=SAMPLE/'Plugins/x86_64/singray_rgb_tag.dll';output.parent.mkdir(parents=True,exist_ok=True)
    shutil.copy2(bridge/'singray_rgb_tag.dll', output)
    print('Windows bridge:',output,output.stat().st_size)

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('platform',choices=['android','windows']);args=parser.parse_args()
    {'android':android,'windows':windows}[args.platform]()
