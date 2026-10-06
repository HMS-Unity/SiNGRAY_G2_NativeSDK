using UnityEngine;
using UnityEngine.UI;
namespace Singray.UI.Keyboard {
public class UpdateHanZiHotWord : MonoBehaviour
{
    //public TextAsset textAsset;
    public InputField inputField;
    public Button update;


    private void Awake()
    {
        update.onClick.AddListener(()=> {
           string str= inputField.text;

            if (!string.IsNullOrEmpty(str)) {
                foreach (var item in str)
                {
                    PinYin.Instance.UpdateHotWord(item);
                }
                PinYin.Instance.SaveHotWord();
            }

           


        });
    }


   
}
}
