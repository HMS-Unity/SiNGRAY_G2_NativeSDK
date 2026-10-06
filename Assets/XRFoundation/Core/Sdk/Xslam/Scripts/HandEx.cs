using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HandEx
{

    public readonly float xMult;

    public readonly GameObject spheresParent;
    public readonly GameObject[] spheres;
    public readonly LineRenderer[] lineRenderers;
    public readonly Queue<float> waySumQueue;
    public float show = 0f;
    public float indexDist;
    public const int JointsNum = 50;
    public float sphereScale = 0.015f;//0.015f
    public float zAlpha = 1f;
    public float zBeta = 1f;
    public float speed = 5f;
    public float avgSecondsToHide = 2f;
    public float showSpeed = 0.1f;
    public float maxAvgWaySum = 30f;
    public float minDist = 2f;
    public float maxDist = 10f;
    public float indexDistMin = 8f;

    public HandEx(float xMult)
    {
        this.xMult = xMult;

        spheresParent = new GameObject("Hand");
        spheresParent.transform.rotation = new Quaternion(-90, 0, 0, -1); ;
        spheres = new GameObject[HandParams.JointsExNum];
        lineRenderers = new LineRenderer[JointsNum];
        foreach (var i in Enumerable.Range(0, HandParams.JointsExNum))
        {
            spheres[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            spheres[i].transform.parent = spheresParent.transform;
            spheres[i].transform.localScale =
             new Vector3(sphereScale, sphereScale, sphereScale);
            spheres[i].transform.position = new Vector3(-1,-1,-1);
            lineRenderers[i] = spheres[i].AddComponent<LineRenderer>();
            lineRenderers[i].widthMultiplier = 0.01f;
            lineRenderers[i].SetPosition(0, spheres[i].transform.position);

            lineRenderers[i].SetPosition(1, spheres[i].transform.position);
        }

        waySumQueue = new Queue<float>();
    }

    public void Process(Data.HandData data)
    {
        float scaleX = 1;// data.vert * data.distX + (1 - data.vert) * data.distY;
        float scaleY = 1;//data.vert * data.distY + (1 - data.vert) * data.distX;
        float dist = (scaleX + scaleY) / 2;

        // Calc total way and move the spheres
        var waySum = 0f;
        foreach (var i in Enumerable.Range(0, HandParams.JointsExNum))
        {
            var target = new Vector3(data.joints[i].x, -data.joints[i].y, data.joints[i].z);
            var actualTarget = Vector3.Lerp(spheres[i].transform.position, target,
                Vector3.Distance(spheres[i].transform.position, target) * speed);

            //waySum += Vector3.Distance(spheres[i].transform.position, actualTarget);
         //   var tartget = actualTarget;
            // new Vector3(data.joints[i].x , -data.joints[i].y ,data.joints[i].z);
                                       //      spheres[i].transform.position = tartget;
            spheres[i].transform.position = target;// Vector3.Lerp(spheres[i].transform.position, tartget, 0.3f);
            //    spheres[i].transform.rotation = new Quaternion(-90, 0, 0, 0); ;
        }
        waySumQueue.Enqueue(waySum);
        //

        // Calc show
        var avgFramesToHide = Mathf.Max(1f, avgSecondsToHide / Time.deltaTime);
        while (waySumQueue.Count > avgFramesToHide) waySumQueue.Dequeue();
        var avgWaySum = waySumQueue.Average();
        if (avgWaySum > maxAvgWaySum || dist <minDist || dist > maxDist)
        {
            show = 0f;
        }
        else
        {
            show = Mathf.Min(show + showSpeed / avgWaySum, 1f);
        }
        spheresParent.SetActive(true);//show.Equals(1f)
                                      //

        // Move lines
        foreach (
                 var i in Enumerable.Empty<int>()
                      .Concat(Enumerable.Range(0, 3))
                      .Concat(Enumerable.Range(4, 7))
                      .Concat(Enumerable.Range(8, 11))
                      .Concat(Enumerable.Range(12, 15))
                      .Concat(Enumerable.Range(16, 20))
                      .Concat(Enumerable.Range(22, 24))
                     .Concat(Enumerable.Range(25, 28))
                     .Concat(Enumerable.Range(29, 32))
                     .Concat(Enumerable.Range(33, 36))
                     .Concat(Enumerable.Range(37, 40))
                     .Concat(Enumerable.Range(41, 45))
                     .Concat(Enumerable.Range(47, 49))
             )
        {
            if (i != 3 && i != 7 && i != 11 && i != 15 && i != 20 && i != 24 && i != 21 && i != 46 && i != 28 && i != 32 && i != 36 && i != 40  && i != 45 && i != 49 && i < 50)
            {
                lineRenderers[i].SetPosition(0, spheres[i].transform.position);

                lineRenderers[i].SetPosition(1, spheres[i + 1].transform.position);
            }


        }
        //


        indexDist =
            Vector3.Distance(spheres[8].transform.position, spheres[12].transform.position) +
            Vector3.Distance(spheres[8].transform.position, spheres[16].transform.position) +
            Vector3.Distance(spheres[8].transform.position, spheres[20].transform.position);
    }
}