using UnityEditor;
using UnityEngine;

namespace Singray.UI.Keyboard
{
    public class CreateInputField : MonoBehaviour
    {
        [MenuItem("GameObject/UI/CustomInputField", false, 1)]

        static void InstantiateInputFiled()
        {
            if (Selection.gameObjects != null && Selection.gameObjects.Length > 0)
            {
                Instantiate(Resources.Load<GameObject>("CustomInputField"), Selection.gameObjects[0].transform).name = "CustomInputField";

            }
            else
            {

                Canvas canvas = FindObjectOfType<Canvas>();

                Instantiate(Resources.Load<GameObject>("CustomInputField"), canvas.transform).name = "CustomInputField";
            }
        }

        [MenuItem("Component/UI/CustomInputField", false, 1)]

        static void InstantiateInputFiledComponent()
        {
            if (Selection.gameObjects != null && Selection.gameObjects.Length > 0)
            {
                Instantiate(Resources.Load<GameObject>("CustomInputField"), Selection.gameObjects[0].transform).name = "CustomInputField";

            }
            else
            {

                Canvas canvas = FindObjectOfType<Canvas>();

                Instantiate(Resources.Load<GameObject>("CustomInputField"), canvas.transform).name = "CustomInputField";
            }
        }
    }
}
