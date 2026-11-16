using UnityEngine;
namespace Singray.Foundation
{
    public class XvMedioRecordTips : MonoBehaviour
    {

        public TextMesh textMesh;

        private Transform mainCamera;

        private Transform parent;

        private void Awake()
        {
            mainCamera = Camera.main != null ? Camera.main.transform : null;
            parent = transform.parent;
        }

        private void OnEnable()
        {
            HideTips();
        }
        public void ShowTips(string content, int fontSize = 60)
        {
            if (textMesh != null)
            {
                CancelInvoke(nameof(HideTips));
                transform.SetParent(null, true);
                textMesh.text = content;
                textMesh.fontSize = fontSize;

            }
        }

        public void HideTips()
        {
            CancelInvoke(nameof(HideTips));
            if (parent != null && transform.parent != parent) transform.SetParent(parent, true);
            if (textMesh != null) textMesh.text = "";
        }

        public void ShowTips(string content, float time,int fontSize= 60)
        {
            CancelInvoke(nameof(HideTips));
            transform.SetParent(null, true);
            if (textMesh != null)
            {
                textMesh.text = content;
                textMesh.fontSize = fontSize;
            }
            Invoke(nameof(HideTips), Mathf.Max(0, time));
        }

        private void Update()
        {
            if (mainCamera == null && Camera.main != null) mainCamera = Camera.main.transform;
            if (mainCamera == null) return;
            transform.position = mainCamera.position + mainCamera.forward * 1.2f;
            transform.rotation = mainCamera.rotation;

        }
        private void OnDisable() { HideTips(); }
    }
}
