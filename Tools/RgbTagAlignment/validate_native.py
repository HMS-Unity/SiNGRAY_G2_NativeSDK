"""Exercise the actual Windows C ABI with synthetic camera images; not hardware acceptance."""
import ctypes as ct, json, pathlib, time
import cv2
import numpy as np

ROOT=pathlib.Path(__file__).resolve().parents[2]
OUT=ROOT/'Builds/RgbTagAlignment_20261116';OUT.mkdir(parents=True,exist_ok=True)
lib=ct.CDLL(str(ROOT/'Assets/XRFoundation/SampleScenes/RgbTagAlignment/Plugins/x86_64/singray_rgb_tag.dll'))
dp=np.ctypeslib.ndpointer(dtype=np.float64,flags='C_CONTIGUOUS')
bp=np.ctypeslib.ndpointer(dtype=np.uint8,flags='C_CONTIGUOUS')
lib.rta_detect.argtypes=[bp,ct.c_int,ct.c_int,ct.c_int,dp,ct.c_double,dp,ct.c_int]
lib.rta_project.argtypes=[dp,dp,dp,ct.c_double,dp]
assert lib.rta_abi_version()==1
k=np.array([1100.,1090.,640.,360.,0.,0.,0.,0.,0.])
camera=np.array([[k[0],0,k[2]],[0,k[1],k[3]],[0,0,1.]])
obj=np.array([[-.04,.04,0],[.04,.04,0],[.04,-.04,0],[-.04,-.04,0]],np.float32)
dictionary=cv2.aruco.getPredefinedDictionary(cv2.aruco.DICT_APRILTAG_36h11)
marker=cv2.aruco.generateImageMarker(dictionary,0,512)
checks=[]
def detect(gray,params=k):
    rgba=np.ascontiguousarray(cv2.cvtColor(gray,cv2.COLOR_GRAY2RGBA))
    result=np.zeros(31,np.float64);start=time.perf_counter()
    state=lib.rta_detect(rgba,rgba.nbytes,gray.shape[1],gray.shape[0],params,.08,result,31)
    return state,result,(time.perf_counter()-start)*1000,rgba
def image_for(r,t,params=k):
    corners=cv2.projectPoints(obj,cv2.Rodrigues(r)[0],t,camera,np.zeros(5))[0].reshape(4,2)
    h=cv2.getPerspectiveTransform(np.float32([[0,0],[511,0],[511,511],[0,511]]),corners.astype(np.float32))
    image=cv2.warpPerspective(marker,h,(1280,720),flags=cv2.INTER_LINEAR,borderValue=255)
    if np.any(params[4:]):
        grid=np.stack(np.meshgrid(np.arange(1280),np.arange(720)),axis=-1).astype(np.float32)
        source=cv2.undistortPoints(grid.reshape(-1,1,2),camera,params[4:],P=camera).reshape(720,1280,2)
        image=cv2.remap(image,source[:,:,0],source[:,:,1],cv2.INTER_LINEAR,borderValue=255)
    return image
base=np.diag([1.,-1.,-1.])
for i,(distance,tilt,offset,roll) in enumerate([
    (.4,.25,0,0),(.7,.35,.06,0),(1.1,.30,-.1,0),(.55,.55,.08,0),
    (.65,.3,0,1.57),(.65,.3,0,3.14),(.65,.3,0,-1.57),(.8,-.45,-.1,.2)]):
    r=cv2.Rodrigues(np.array([tilt,.2,roll]))[0]@base;t=np.array([offset,.02,distance])
    image=image_for(r,t);state,result,ms,rgba=detect(image)
    assert state==1,(i,state)
    translation_error=float(np.linalg.norm(result[12:15]-t))
    rotation_error=float(np.linalg.norm(cv2.Rodrigues(result[3:12].reshape(3,3)@r.T)[0])*180/np.pi)
    assert translation_error<.008,(i,translation_error)
    assert rotation_error<8,(i,rotation_error)
    assert result[1]<1,(i,result[1])
    checks.append(dict(case='pose_'+str(i),translationErrorMetres=translation_error,rotationErrorDegrees=rotation_error,rmsePixels=result[1],milliseconds=ms))
    if i==1:
        cv2.imwrite(str(OUT/'synthetic-rgb.png'),image);(OUT/'synthetic-rgb.rgba').write_bytes(rgba.tobytes())
        (OUT/'synthetic-result.json').write_text(json.dumps(dict(result=result.tolist(),calibration=k.tolist()),indent=2))
params=k.copy();params[4:]=[-.16,.035,.001,-.001,.002]
r=cv2.Rodrigues(np.array([.3,.35,.1]))[0]@base;t=np.array([.14,.04,.6])
state,result,ms,_=detect(image_for(r,t,params),params)
assert state==1 and np.linalg.norm(result[12:15]-t)<.008
checks.append(dict(case='distorted_offcentre',rmsePixels=result[1]))
projection=np.zeros(8,np.float64)
assert lib.rta_project(params,r.reshape(9).copy(),t,.08,projection)==1
expected=cv2.projectPoints(obj,cv2.Rodrigues(r)[0],t,camera,params[4:])[0].reshape(8)
assert np.max(np.abs(projection-expected))<.001
checks.append(dict(case='locked_projection_matches_independent_reference',maxPixels=float(np.max(np.abs(projection-expected)))))
blank=np.full((720,1280),255,np.uint8);assert detect(blank)[0]==0
checks.append(dict(case='blank_rejected'))
other=blank.copy();other[200:456,400:656]=cv2.aruco.generateImageMarker(dictionary,1,256)
assert detect(other)[0]==0;checks.append(dict(case='id_1_rejected'))
duplicate=blank.copy();duplicate[100:356,100:356]=cv2.resize(marker,(256,256));duplicate[350:606,800:1056]=cv2.resize(marker,(256,256))
assert detect(duplicate)[0]==-3;checks.append(dict(case='duplicate_id_0_rejected'))
wrong=blank.copy();wrong[200:456,400:656]=cv2.aruco.generateImageMarker(cv2.aruco.getPredefinedDictionary(cv2.aruco.DICT_4X4_50),0,256)
assert detect(wrong)[0]==0;checks.append(dict(case='wrong_dictionary_rejected'))
bad=k.copy();bad[0]=float('nan');assert detect(blank,bad)[0]==-1;checks.append(dict(case='invalid_intrinsics_rejected'))
receipt=dict(status='PASS',bridgeOpenCV='4.10.0',generatorOpenCV=cv2.__version__,checks=checks,hardwareTest=False)
(OUT/'native-tests.json').write_text(json.dumps(receipt,indent=2),encoding='utf8')
print(json.dumps(receipt,indent=2))
