using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Singray.Foundation.SampleScenes
{
    public class MRVideoCaptureDemo : MonoBehaviour
    {
        [SerializeField] private XvMRVideoCaptureManager captureManager;
        public RawImage videoRender;
        private bool previewRequested, originalVisibility;
        private GameObject[] panelGraphics;
        private int[] originalLayers;
        private void Awake() { if (videoRender != null) originalVisibility = videoRender.enabled; CachePanel(); }
        private void CachePanel()
        {
            if (panelGraphics != null) return;
            var panel = transform.Find("UI");
            if (panel == null) return;
            var objects = new HashSet<GameObject>();
            foreach (var r in panel.GetComponentsInChildren<Renderer>(true)) objects.Add(r.gameObject);
            foreach (var r in panel.GetComponentsInChildren<Graphic>(true)) objects.Add(r.gameObject);
            foreach (var r in panel.GetComponentsInChildren<Canvas>(true)) objects.Add(r.gameObject);
            panelGraphics = new GameObject[objects.Count]; objects.CopyTo(panelGraphics);
            originalLayers = new int[panelGraphics.Length];
            for (int i=0;i<panelGraphics.Length;i++) originalLayers[i]=panelGraphics[i].layer;
        }
        private void OnEnable()
        {
            CachePanel();
            // Only renderable panel parts move to the built-in UI layer; interaction roots stay intact.
            if (panelGraphics != null) foreach (var part in panelGraphics) part.layer=5;
            XvMRVideoCaptureManager.RegisterDemoUi(this);
        }
        private XvMRVideoCaptureManager ResolveCapture()
        {
            if (captureManager == null) captureManager = GetComponentInChildren<XvMRVideoCaptureManager>(true);
            if (captureManager == null) captureManager = FindObjectOfType<XvMRVideoCaptureManager>();
            if (captureManager == null)
                captureManager = Instantiate(Resources.Load<GameObject>("XvMRVideoCaptureManager")).GetComponent<XvMRVideoCaptureManager>();
            return captureManager;
        }
        public void StartMRCaptureCamera()
        {
            if (previewRequested) return;
            ResolveCapture().AcquireCapture(this); previewRequested=true; UpdatePreview();
        }
        public void StopMRCaptureCamera()
        {
            previewRequested=false;
            if (videoRender != null) { videoRender.texture=null; videoRender.enabled=originalVisibility; }
            if (captureManager != null) captureManager.ReleaseCapture(this,true);
        }
        private void LateUpdate() => UpdatePreview();
        private void UpdatePreview()
        {
            if (!previewRequested || videoRender == null || captureManager == null) return;
            bool ready=captureManager.IsOn && captureManager.HasFrame;
            videoRender.enabled=originalVisibility && ready;
            videoRender.texture=ready ? captureManager.CameraRenderTexture : null;
        }
        private void OnDisable()
        {
            XvMRVideoCaptureManager.UnregisterDemoUi(this); StopMRCaptureCamera();
            if (panelGraphics != null) for (int i=0;i<panelGraphics.Length;i++) if (panelGraphics[i]!=null) panelGraphics[i].layer=originalLayers[i];
        }
    }
}
