using Microsoft.MixedReality.Toolkit;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ETInteraction : MonoBehaviour
{
    //public XvEyeFlowManage xvEyeFlowManage;
    public XvEyeInit xvEyeInit;

    public Transform head;

    [Header("UI button gaze interaction")]
    [Tooltip("Gaze at the same button for this duration in seconds to click it. Add a 3D Collider, such as a BoxCollider matching the RectTransform, to the button GameObject so raycasts can detect it.")]
    public float gazeDwellTime = 1.5f;

    private Button _gazedButton;
    private float _gazeDwellTimer;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        //Debug.DrawRay(head.position, head.forward*20);
        //RaycastHit lefthit;
        ////Vector3 leftgazeDirection = leftgaze.position - head.position;
        //Vector3 leftgazeDirection = head.forward;
        //Ray leftRay = new Ray(head.position, leftgazeDirection);
        //if (Physics.Raycast(leftRay, out lefthit))
        //{
        //    if (lefthit.collider.gameObject.name == "plane")
        //    {
        //        GameObject.Find("leftHitpoint").transform.position = lefthit.point;
        //    }

        //}

        //if (xvEyeFlowManage.Recomgaze!=null)
        if (xvEyeInit.Recomgaze!=null)
        {
            //Vector3 recomgazeDirection = xvEyeFlowManage.Recomgaze.position - head.position;
            Vector3 recomgazeDirection = xvEyeInit.Recomgaze.position - head.position;

            //Vector3 leftgazeDirection = leftgaze.position - head.position;
            //Vector3 rightgazeDirection = rightgaze.position - head.position;

            #region Interact with scene cubes using the eye-gaze ray
            Ray recomRay = new Ray(head.position, recomgazeDirection);
            //Ray leftRay = new Ray(head.position, leftgazeDirection);
            //Ray rightRay = new Ray(head.position, rightgazeDirection);

            RaycastHit recomhit;
            //RaycastHit lefthit;
            //RaycastHit righthit;

            bool didHit = Physics.Raycast(recomRay, out recomhit);
            if (didHit)
            {
                for (int i = 0; i < transform.childCount; i++)
                {
                    if (transform.GetChild(i).name == recomhit.collider.gameObject.name)
                    {
                        transform.GetChild(i).localScale = new Vector3(1, 1, 0.05f);
                    }
                    else
                    {
                        transform.GetChild(i).localScale = new Vector3(0.6f, 0.6f, 0.05f);
                    }
                }

            }
            else
            {
                for (int i = 0; i < transform.childCount; i++)
                {
                    if (transform.GetChild(i).localScale.x != 0.6f)
                    {
                        transform.GetChild(i).localScale = new Vector3(0.6f, 0.6f, 0.05f);
                    }
                }
            }
            #endregion

            #region Trigger UI button clicks with eye gaze (a Collider is required on the button)
            Button hitButton = didHit ? recomhit.collider.GetComponent<Button>() : null;
            if (hitButton != null)
            {
                if (hitButton == _gazedButton)
                {
                    _gazeDwellTimer += Time.deltaTime;
                    if (_gazeDwellTimer >= gazeDwellTime)
                    {
                        hitButton.onClick.Invoke();
                        _gazeDwellTimer = 0f;
                        _gazedButton = null; // Look away and back before triggering another click
                    }
                }
                else
                {
                    _gazedButton = hitButton;
                    _gazeDwellTimer = 0f;
                }
            }
            else
            {
                _gazedButton = null;
                _gazeDwellTimer = 0f;
            }
            #endregion
        }

        if (API.xslam_ready())
        {
            //Debug.Log($"head left pos:{Math.Round(head.GetChild(0).GetChild(0).position.x, 3)},{Math.Round(head.GetChild(0).GetChild(0).position.y, 3)},{Math.Round(head.GetChild(0).GetChild(0).position.z, 3)}");
            //Debug.Log($"head left rot:{head.GetChild(0).GetChild(0).eulerAngles}");
            //Debug.Log($"head right:{Math.Round(head.GetChild(0).GetChild(1).position.x, 3)},{Math.Round(head.GetChild(0).GetChild(1).position.y, 3)},{Math.Round(head.GetChild(0).GetChild(1).position.z, 3)}");
            //Debug.Log($"head right rot:{head.GetChild(0).GetChild(1).eulerAngles}");
        }
    }
}
