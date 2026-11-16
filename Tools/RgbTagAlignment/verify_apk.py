"""Read-only standalone APK, native dependency and preservation audit."""
import hashlib,json,pathlib,re,struct,zipfile,sys
ROOT=pathlib.Path(__file__).resolve().parents[2];OUT=ROOT/'Builds/RgbTagAlignment_20261116'
def sha(data):return hashlib.sha256(data).hexdigest()
apk=pathlib.Path(sys.argv[1]).resolve() if len(sys.argv)>1 else OUT/'G2_RgbTagAlignment.apk'
with zipfile.ZipFile(apk) as package:
    names=package.namelist();native=[n for n in names if n.startswith('lib/') and n.endswith('.so')]
    assert native and all(n.startswith('lib/arm64-v8a/') for n in native)
    assert not any('singray_rgb_tag.dll' in n for n in names)
    assert 'assets/bin/Data/level0' in names and 'assets/bin/Data/level1' not in names
    assert 'lib/arm64-v8a/libil2cpp.so' in names
    source=ROOT/'Assets/XRFoundation/SampleScenes/RgbTagAlignment/Plugins/Android/arm64-v8a/libsingray_rgb_tag.so'
    data=package.read('lib/arm64-v8a/libsingray_rgb_tag.so');assert sha(data)==sha(source.read_bytes())
    assert data[:6]==b'\x7fELF\x02\x01' and struct.unpack_from('<H',data,18)[0]==183
    phoff=struct.unpack_from('<Q',data,32)[0];phsize,phnum=struct.unpack_from('<HH',data,54)
    alignments=[struct.unpack_from('<IIQQQQQQ',data,phoff+i*phsize)[-1] for i in range(phnum) if struct.unpack_from('<I',data,phoff+i*phsize)[0]==1]
    assert min(alignments)>=16384
    preserved=[]
    for rel in ['Assets/XRFoundation/Core/Sdk/Xslam/Plugins/Android/xvsdk-release.aar','Assets/XRFoundation/Core/Sdk/XR/Plugins/Android/xvxrlib-release.aar']:
        with zipfile.ZipFile(ROOT/rel) as archive:
            for entry in archive.namelist():
                if entry.startswith('jni/arm64-v8a/') and entry.endswith('.so'):
                    path='lib/arm64-v8a/'+entry.split('/')[-1]
                    assert path in names and sha(archive.read(entry))==sha(package.read(path)),(rel,entry)
                    preserved.append(path)
    assert 'lib/arm64-v8a/libopencv_java4.so' in preserved and 'lib/arm64-v8a/libc++_shared.so' in preserved
sample=ROOT/'Assets/XRFoundation/SampleScenes/RgbTagAlignment'
runtime='\n'.join(p.read_text(encoding='utf8') for p in (sample/'Scripts').glob('*.cs'))+(sample/'Native~/rgb_tag.cpp').read_text()
assert not re.search(r'xslam_\w*detect_tags|xslam_getTagDetectionrgbImage|XvTagRecognizerManager|XvAprilTag|xv_get_rgb_pixel_pose|TofDepthCameraStream|TofIRCameraStream',runtime)
baseline_path=OUT/'baseline.json'
baseline=json.loads(baseline_path.read_text()) if baseline_path.exists() else {}
changed=[p for p,h in baseline.items() if not (ROOT/p).is_file() or sha((ROOT/p).read_bytes())!=h]
newfiles={str(p.relative_to(ROOT)).replace('\\','/'):sha(p.read_bytes()) for p in sample.rglob('*') if p.is_file()}
receipt=dict(status='PASS',apk=str(apk),bytes=apk.stat().st_size,sha256=sha(apk.read_bytes()),nativeLibraryCount=len(native),
             nativeBridgeLoadAlignments=alignments,vendorNativeEntriesPreserved=len(preserved),baselineFiles=len(baseline),preservationBaseline='AVAILABLE' if baseline_path.exists() else 'NOT_AVAILABLE - preservation not checked',
             changedExistingFilesObserved=changed,newSampleHashes=newfiles,hardwareTest=False)
receipt_path=apk.with_name(apk.stem+'-verification.json') if len(sys.argv)>1 else OUT/'apk-verification.json'
receipt_path.write_text(json.dumps(receipt,indent=2),encoding='utf8')
print(json.dumps({k:v for k,v in receipt.items() if k!='newSampleHashes'},indent=2))
