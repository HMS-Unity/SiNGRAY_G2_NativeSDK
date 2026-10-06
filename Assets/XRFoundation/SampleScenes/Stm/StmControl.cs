using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StmControl : MonoBehaviour
{
    API.StmData stm;

    bool isReady = false;

    [Header("Magnetometer direction arrow")]
    public Transform stmArrow;
    public Transform arrowPos;

    //public TextMeshProUGUI stmOffSetTxt;
    public TextMeshProUGUI stmAnglesSetTxt;
    //public TextMeshProUGUI stmMagneticSetTxt;
    public TextMeshProUGUI stmLevelSetTxt;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
#if UNITY_EDITOR
        stmArrow.position = arrowPos.position;
        stmArrow.eulerAngles = new Vector3(0, arrowPos.transform.parent.eulerAngles.y, 0);

        return;
#endif
        if (API.xslam_ready() && !isReady)
        {
            isReady = true;
            API.xv_stm_start();
        }

        if (isReady)
        {
            API.xv_stm_get_stream(ref stm);//Magnetometer

            //stmOffSetTxt.text = "";
            stmAnglesSetTxt.text = "";
            //stmMagneticSetTxt.text = "";

            /*for (int i = 0; i < stm.offset.Length; i++)
            {
                stmOffSetTxt.text += stm.offset[i].ToString() + "\n";
                //Debug.Log($"wayland xv_get_gpsStream offset.gpsData[{i}]:{gps.offset[i]}");
            }*/

            for (int i = 0; i < stm.angles.Length; i++)
            {
                stmAnglesSetTxt.text += stm.angles[i].ToString() + "\n";
                //Debug.Log($"wayland xv_get_gpsStream angles.gpsData[{i}]:{gps.angles[i]}");
            }
            /*for (int i = 0; i < stm.magnetic.Length; i++)
            {
                stmMagneticSetTxt.text += stm.magnetic[i].ToString() + "\n";
                //Debug.Log($"wayland xv_get_gpsStream gps.gpsData[{i}]:{gps.magnetic[i]}");
            }*/
            stmLevelSetTxt.text = stm.level.ToString();

            //Assign the magnetometer direction arrow
            stmArrow.position = arrowPos.position;
            stmArrow.eulerAngles = new Vector3(0, arrowPos.transform.parent.eulerAngles.y - stm.angles[0], 0);
            
        }
    }


    private void OnApplicationQuit()
    {
        API.xv_stm_stop();
    }
}
