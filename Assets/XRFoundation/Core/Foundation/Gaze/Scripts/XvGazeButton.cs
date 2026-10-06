using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Xvisio.Input.UI
{
    public class XvGazeButton :MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private bool enter;
        private float timer;
        private float gapTimer=2;
        [SerializeField]
         private Image progress;

        [SerializeField]
        private Button bt;
        protected   void Awake()
        {
            if (progress==null) { 
            progress=GetComponent<Image>();
            
            }

            if (bt==null) {
                bt=GetComponent<Button>();
            }


            progress.fillAmount = 0;
            timer = 0;

            enter = false;
        }
        public  void OnPointerEnter(PointerEventData eventData)
        {
            progress.fillAmount = 0;
            timer = 0;

            enter = true;
        }

        public  void OnPointerExit(PointerEventData eventData)
        {
            
            enter = false;
            progress.fillAmount = 0;
            timer = 0;

        }

        private void Update()
        {
            if (enter)
            {
                timer += Time.deltaTime;
                progress.fillAmount = timer / gapTimer;

                if (timer> gapTimer) {
                    enter = false;
                    progress.fillAmount = 0;
                    timer = 0;
                    bt.onClick?.Invoke();

                }
            }
            
        }

      
    }
}
