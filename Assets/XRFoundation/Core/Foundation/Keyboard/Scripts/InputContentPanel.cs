using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
namespace Singray.UI.Keyboard
{

    public sealed class InputContentPanel : MonoBehaviour
    {
        public List<Button> buttonList = new List<Button>();
        private ICustomInputField inputField;
        public CustomInputField customInputField;
        private InputContentPanel() { }

        private Text InputLetterText;
        private Transform hanziParent;

        private Button previousBt;
        private Button nextBt;
        private string key;

        private string hanzi;
        private int showLength = 8;

        StringBuilder tmpInputSb = new StringBuilder();
        StringBuilder finalInputSb = new StringBuilder();
       
        internal void Initialized()
        {
            InputLetterText = transform.Find("InputContent/InputTip").GetComponent<Text>();
            hanziParent = transform.Find("InputContent/Content");
            previousBt = transform.Find("InputContent/Content/SwitchPage/Previous").GetComponent<Button>();
            nextBt = transform.Find("InputContent/Content/SwitchPage/Next").GetComponent<Button>();

            previousBt.onClick.AddListener(PreviousPage);
            nextBt.onClick.AddListener(NextPage);
        }

        internal void SetInputField(ICustomInputField inputField)
        {

            if (this.inputField  !=null) {
                this.inputField.SetCaretActive(false);
            }
            this.inputField = inputField;
            this.inputField.SetCaretActive(true);


            if (customInputField!=null) {
                this.customInputField.SetCaretActive(false);
            }
          
            this.customInputField.SetCaretActive(true);

            finalInputSb.Clear();
            finalInputSb.Append(inputField.text);

            ((ICustomInputField)customInputField).SetText(finalInputSb.ToString());
        }

        internal void Clear() {

            if (this.inputField!=null) {
                finalInputSb.Clear();
                this.inputField.SetText("");

                ((ICustomInputField)customInputField).SetText("");
            }
        
        }

        internal void InputEvent(KeyboardIputType keyboardIputType, string content)
        {
            SetInputFieldText(content);
        }
        internal void BackSpaceEvent()
        {
            BackSpace();
        }
        internal void OKEvent()
        {
            finalInputSb.Clear();

            tmpInputSb.Clear();

            InputLetterText.text = "";

            MRKeyboard.Instance.OKEvent?.Invoke();

            if (this.inputField != null)
            {
                this.inputField?.OnHideEvent();
                this.inputField.SetCaretActive(false);
            }

            inputField = null;



        }
        internal void InputKeyboardChange(KeyboardIputType keyboardIputType)
        {
            HideHanZiLable(); ;
            tmpInputSb.Clear();
            InputLetterText.text = "";
            previousBt.gameObject.SetActive(false);
            nextBt.gameObject.SetActive(false);

        }

        private void SetInputFieldText(string letter)
        {
            tmpInputSb.Append(letter);
            if (MRKeyboard.Instance.keyboardIputType == KeyboardIputType.HanZi)
            {
                InputLetterText.text = tmpInputSb.ToString();
                key = InputLetterText.text.ToLower();

                //Compose the input characters
                string hanzi = PinYin.Instance.GetHanZi(key);
                if (hanzi != null)
                {
                    SplitCharacter(hanzi, true);

                }
                else
                {

                    SplitCharacter(tmpInputSb.ToString());
                }
            }
            else
            {

                SplitCharacter(tmpInputSb.ToString());
                finalInputSb.Append(letter);

                //For non-Chinese input, update the input field directly
                UpdateFinalText();
            }


        }
        private void BackSpace()
        {
            if (tmpInputSb.Length > 0)
            {
                tmpInputSb.Remove(tmpInputSb.Length - 1, 1);
                if (MRKeyboard.Instance.keyboardIputType == KeyboardIputType.HanZi)
                {
                    InputLetterText.text = tmpInputSb.ToString();
                    key = tmpInputSb.ToString().ToLower();
                    string hanzi = PinYin.Instance.GetHanZi(key);
                    if (hanzi != null)
                    {
                        SplitCharacter(hanzi, true);
                        return;
                    }
                    else
                    {
                        previousBt.gameObject.SetActive(false);
                        nextBt.gameObject.SetActive(false);
                        SplitCharacter(tmpInputSb.ToString());

                    }
                }
                else
                {
                    if (finalInputSb.Length > 0)
                    {
                        finalInputSb.Remove(finalInputSb.Length - 1, 1);
                        UpdateFinalText();
                    }

                    SplitCharacter(tmpInputSb.ToString());
                }
            }
            else
            {

                if (finalInputSb.Length > 0)
                {
                    finalInputSb.Remove(finalInputSb.Length - 1, 1);
                    UpdateFinalText();


                }

            }

        }
        private void UpdateFinalText()
        {
            if (inputField!=null) {
                inputField.SetText(finalInputSb.ToString());
                ((ICustomInputField)customInputField).SetText(finalInputSb.ToString());

            }
        }
        private void HideHanZiLable()
        {

            for (int i = 0; i < buttonList.Count; i++)
            {
                buttonList[i].onClick.RemoveAllListeners();
                buttonList[i].gameObject.SetActive(false);
            }
        }

        private void SplitCharacter(string zifu, bool isHanZi = false)
        {
            HideHanZiLable();
            if (string.IsNullOrEmpty(zifu))
            {
                return;
            }

            if (isHanZi)
            {
                hanzi = zifu;
                totalPage = Mathf.CeilToInt(hanzi.Length / (showLength * 1.0f));
                currentPage = 0;
               // MyDebugTool.LogError(zifu + "  " + totalPage);
                SetPageContent();
            }
            else
            {
                Button bt = buttonList[0];

                SetCharacter(bt, zifu);
            }
            StartCoroutine(ForceRebuildLayoutImmediate());
        }
        WaitForEndOfFrame WaitForEndOfFrame = new WaitForEndOfFrame();
        private IEnumerator ForceRebuildLayoutImmediate()
        {
            yield return WaitForEndOfFrame;
            LayoutRebuilder.ForceRebuildLayoutImmediate(hanziParent.GetComponent<RectTransform>());

        }
        private void SetCharacter(Button bt, string zifu)
        {
            bt.gameObject.SetActive(true);
            bt.GetComponentInChildren<Text>().text = zifu;
            bt.onClick.AddListener(delegate
            {
                InputLetterText.text = "";
                HideHanZiLable();
                tmpInputSb.Clear();
                if (MRKeyboard.Instance.keyboardIputType == KeyboardIputType.HanZi)
                {
                    finalInputSb.Append(zifu);
                    UpdateFinalText();
                    if (int.TryParse(bt.name, out int index))
                    {
                        PinYin.Instance.UpdateHotWord(key, index, zifu);
                    }
                    previousBt.gameObject.SetActive(false);
                    nextBt.gameObject.SetActive(false);
                   
                }
                else
                {
                    UpdateFinalText();
                    HideHanZiLable();
                }
            });
        }

        private void PreviousPage()
        {
            currentPage--;
            currentPage = Mathf.Clamp(currentPage, 0, totalPage - 1);
            SetPageContent();
        }

        private void NextPage()
        {
            currentPage++;
            currentPage = Mathf.Clamp(currentPage, 0, totalPage - 1);
            SetPageContent();
        }

        private int totalPage;
        private int currentPage;
        private void SetPageContent()
        {
            HideHanZiLable();
            previousBt.gameObject.SetActive(true);
            nextBt.gameObject.SetActive(true);

            SetButtonState(previousBt);
            SetButtonState(nextBt);

            if (hanzi.Length < showLength)
            {
                previousBt.gameObject.SetActive(false);
                nextBt.gameObject.SetActive(false);
            }
            else
            {
                if (currentPage == 0)
                {

                    SetButtonState(previousBt, false);
                    SetButtonState(nextBt, true);
                }
                else if (currentPage == totalPage - 1)
                {

                    SetButtonState(previousBt, true);
                    SetButtonState(nextBt, false);
                }
            }

           // MyDebugTool.LogError(totalPage + "   " + currentPage + "" + hanzi.Length);
            Button bt1 = buttonList[0];

            SetCharacter(bt1, tmpInputSb.ToString());

            int index = currentPage * showLength;
            int length = Mathf.Clamp(index + showLength, 0, hanzi.Length);
            int btIndex = 1;

            //MyDebugTool.LogError(index + "   " + length + "   " + currentPage);
            for (int i = index; i < length; i++)
            {
                //MyDebugTool.LogError(hanzi[i]);

                Button bt;
                bt = buttonList[btIndex];
                bt.name = i.ToString();
                SetCharacter(bt, hanzi[i].ToString());
                btIndex++;

            }
        }

        private void SetButtonState(Button bt, bool interactable = true)
        {
            bt.interactable = interactable;

            if (!interactable)
            {
                bt.GetComponent<Image>().color = Color.gray;
            }
            else
            {
                bt.GetComponent<Image>().color = new Color(0.1529412f, 0.1568628f, 0.172549f,1);

            }
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                PinYin.Instance.SaveHotWord();
            }
        }
        private void OnDisable()
        {
   
            PinYin.Instance.SaveHotWord();
            
        }

    }

}
