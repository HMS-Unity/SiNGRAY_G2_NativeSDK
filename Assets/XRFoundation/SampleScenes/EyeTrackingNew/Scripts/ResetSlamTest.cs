using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResetSlamTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ResetSlam()
    {
        bool b = API.xslam_reset_slam();
        Debug.Log($"xslam_reset_slam:{b}");
    }
}
