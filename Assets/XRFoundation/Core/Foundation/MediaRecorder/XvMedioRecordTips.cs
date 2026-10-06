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
            mainCamera = Camera.main.transform;
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
                transform.SetParent(null);
                textMesh.text = content;
                textMesh.fontSize = fontSize;

            }
        }

        public void HideTips()
        {
            transform.SetParent(parent);
            textMesh.text = "";
        }

        public void ShowTips(string content, float time,int fontSize= 60)
        {
            if (textMesh != null)
            {
                textMesh.text = content;
                textMesh.fontSize = fontSize;
            }
            transform.SetParent(null);

            Invoke("HideTips", time);
        }

        private void Update()
        {
            transform.position = mainCamera.position + mainCamera.forward * 1.2f;
            transform.rotation = mainCamera.rotation;

        }
    }
}
