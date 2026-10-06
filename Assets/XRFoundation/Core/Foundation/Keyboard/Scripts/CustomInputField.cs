using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.UI.InputField;
using System.Text.RegularExpressions;


namespace Singray.UI.Keyboard
{
    public partial interface ICustomInputField
    {
        public string text { get; }
        public abstract void SetText(string v);

        public abstract void SetCaretActive(bool isActive);

        public void OnHideEvent();

    }

    public class CustomInputField : MonoBehaviour, ICustomInputField
    {

        protected CustomInputField()
        { }
        private Button button;
        private RectTransform Caret;

        private Text textComponent;
        private bool isActive;

        [SerializeField]
        private ContentType contentType;

        public Sprite normalSprite;
        public Sprite selectedSprite;

        public UnityAction<string> onValueChanged;
        public Text LableText;

        private string inputContent;
        private string GetText()
        {
            if (string.IsNullOrEmpty(inputContent))
            {
                return "";
            }
            var pattern = "\\w";
            Regex reg = new Regex(pattern);
            //// Input string


            switch (contentType)
            {
                case ContentType.Standard:
                    break;
                case ContentType.Autocorrected:
                    break;
                case ContentType.IntegerNumber:
                    break;
                case ContentType.DecimalNumber:
                    break;
                case ContentType.Alphanumeric:
                    break;
                case ContentType.Name:
                    break;
                case ContentType.EmailAddress:
                    break;
                case ContentType.Password:
                    return reg.Replace(inputContent, "*");

                case ContentType.Pin:
                    return reg.Replace(inputContent, "*");
                    break;
                case ContentType.Custom:
                    break;
                default:
                    break;
            }
            return inputContent;

        }



        public string text
        {
            get
            {
                if (textComponent == null)
                {
                    textComponent = transform.Find("Mask/Text").GetComponent<Text>();
                }


                return inputContent;
            }
            set
            {
                if (textComponent == null)
                {
                    textComponent = transform.Find("Mask/Text").GetComponent<Text>();
                }

                inputContent = value;

                textComponent.text = GetText();
            }
        }



        protected void Awake()
        {
            button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(BtClick);
            }
            Caret = transform.Find("Mask/Text/Caret").GetComponent<RectTransform>();
            Caret.gameObject.SetActive(false);

            if (textComponent == null)
            {
                textComponent = GetComponentInChildren<Text>();
            }
            inputContent = textComponent.text;
            textComponent.text = GetText();
        }

        protected void OnEnable()
        {

            if (normalSprite != null)
            {
                GetComponent<Image>().sprite = normalSprite;
            }
            isActive = false;
            LableText?.gameObject.SetActive(string.IsNullOrEmpty(text));

        }
        private void LateUpdate()
        {

            if (isActive)
            {

                if (Time.frameCount % 50 == 0)
                {
                    Caret.gameObject.SetActive(!Caret.gameObject.activeSelf);
                }

            }
        }




        public void BtClick()
        {
            if (selectedSprite != null)
            {
                GetComponent<Image>().sprite = selectedSprite;

            }
            OnClick();
        }
        public void OnPointerClick(PointerEventData eventData)
        {
            if (selectedSprite != null)
            {
                GetComponent<Image>().sprite = selectedSprite;

            }
            OnClick();
        }
        private void OnClick()
        {

            Vector3 pos = (transform.position - (transform.up * (GetComponent<RectTransform>().rect.height * 0.0004f / 2 + 10 * 0.0005f)));

            MyDebugTool.Log(transform.position + "  " + (GetComponent<RectTransform>().rect.height));
            pos.z = transform.position.z;
            Pose pose = new Pose()
            {
                position = pos,
                rotation = transform.rotation
            };


            //pos = transform.position;
            MRKeyboard.Instance.SetInputField(this, pose, contentType);
            MRKeyboard.Instance.Show();

        }

        string ICustomInputField.text
        {
            get
            {
                if (textComponent == null)
                {
                    textComponent = GetComponentInChildren<Text>();
                }

                return inputContent;
            }
        }


        void ICustomInputField.SetText(string v)
        {


            LableText?.gameObject.SetActive(string.IsNullOrEmpty(v));

            onValueChanged?.Invoke(v);

            inputContent = v;
            textComponent.text = GetText();
            float width = preferredWidth + 2;


            Caret.localPosition = Vector3.right * Mathf.Clamp(width, 0, textComponent.rectTransform.rect.size.x);
            Caret.sizeDelta = new Vector2(3, preferredHeight - 10);
            //MyDebugTool.Log(preferredWidth + "      " + preferredHeight);
        }

        public void SetCaretActive(bool isActive)
        {
            this.isActive = isActive;
            if (Caret != null)
            {

                Caret.gameObject.SetActive(isActive);
            }
        }

        public void OnHideEvent()
        {
            MyDebugTool.Log("CustomInputfiles OnHideEvent");
            if (normalSprite != null)
            {
                GetComponent<Image>().sprite = normalSprite;
            }
        }

        public float preferredWidth
        {
            get
            {
                if (textComponent == null)
                    return 0;
                var settings = textComponent.GetGenerationSettings(Vector2.zero);
                return textComponent.cachedTextGeneratorForLayout.GetPreferredWidth(textComponent.text, settings) / textComponent.pixelsPerUnit;
            }
        }
        public float preferredHeight
        {
            get
            {
                if (textComponent == null)
                    return 0;
                var settings = textComponent.GetGenerationSettings(new Vector2(textComponent.rectTransform.rect.size.x, 0.0f));
                return textComponent.cachedTextGeneratorForLayout.GetPreferredHeight(textComponent.text, settings) / textComponent.pixelsPerUnit;
            }
        }

    }
}

