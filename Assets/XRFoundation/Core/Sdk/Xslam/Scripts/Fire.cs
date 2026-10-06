using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class Fire
{
    public readonly Hand hand;
    public readonly GameObject fire;
    public readonly GameObject cube;
    public  GameObject line;
    public Ray ray;
    public LayerMask layerMask;
    private bool isGrabing = false;
    private bool isTapped = false;
    private Vector3 originPos = new Vector3(0, 0f, 5f);
    private static Vector3 lastPos = new Vector3(0, 0f, 0f);
    private VideoPlayer video;
    GameObject red = GameObject.Find("Vigilant Red");
    public Fire(Hand hand, GameObject fire, GameObject cube)
    {
        this.hand = hand;
        this.fire = fire;
        this.cube = cube;
        this.line = GameObject.Find("Cylinder");
        this.ray = new Ray(hand.spheres[8].transform.position, hand.spheres[8].transform.position - hand.spheres[7].transform.position);
        video = GameObject.Find("RawImage").GetComponent<VideoPlayer>();
        video.Play();
    }

    public void Draw(int type)
    {
        //if (hand.show.Equals(1f) )
        //{

        //}
        //else
        //{
        //    fire.SetActive(false);
        //}
        cube.SetActive(true);
        if (type == 9 && Vector3.Distance(cube.transform.position, hand.spheres[8].transform.position) < 0.05)//
        {
            isGrabing = true;
      
        }
        if (isGrabing && (type == 4 || type == 5))
        {
            isGrabing = false;
      
        }
        if(type == 9)
        {
      
         //   cube.transform.position = new Vector3(hand.spheres[8].transform.position.x, hand.spheres[8].transform.position.y,cube.transform.position.z) ;
     
            //   cube.transform.position = Vector3.Lerp(cube.transform.position, hand.spheres[8].transform.position, 1f);
            cube.transform.localEulerAngles += new Vector3(0, 1, 0) * Time.deltaTime * 0;
            video.Pause();

        }
        else if (type == 8)
        {

            //   cube.transform.position = new Vector3(hand.spheres[8].transform.position.x, hand.spheres[8].transform.position.y,cube.transform.position.z) ;

            //   cube.transform.position = Vector3.Lerp(cube.transform.position, hand.spheres[8].transform.position, 1f);
            cube.transform.localEulerAngles -= new Vector3(0, 1, 0) * Time.deltaTime * (4 + type * 3);
            video.Play();

        }
        else if (type == 5 || type == 4)
        {

            //   cube.transform.position = new Vector3(hand.spheres[8].transform.position.x, hand.spheres[8].transform.position.y,cube.transform.position.z) ;

            //   cube.transform.position = Vector3.Lerp(cube.transform.position, hand.spheres[8].transform.position, 1f);
            float dx = hand.spheres[8].transform.position.x - lastPos.x;
            if(Mathf.Abs(dx) > 0.05)
            {
              
            }
            else
            {
                cube.transform.localEulerAngles -= new Vector3(0, 1, 0) * dx * 200;
                video.Play();
             
            }
            lastPos = hand.spheres[8].transform.position;
            //  float rotation = Input.GetAxis("Horizontal") * 100.0F;


        }
        else
        {
            cube.transform.localEulerAngles += new Vector3(0, 1, 0) * Time.deltaTime * (4+type*3);
            video.Play();
        }
      

        if (type == 1)
        {
            fire.SetActive(true);
            fire.transform.position = hand.spheres[8].transform.position;// Vector3.Lerp(fire.transform.position ,hand.spheres[8].transform.position,1f);
         
        } else
        {
            fire.SetActive(false);
        //    line.SetActive(false);
            // cube.SetActive(false);
        }
        
        if (type == 1 && !isTapped)
        {
            if (Vector3.Distance(red.transform.position, hand.spheres[8].transform.position) < 0.1)
            {
                isTapped = true;
            }
            if (isTapped)
            {
                if (video.isPaused)
                {

                    video.Play();

                }
                else
                {
                    video.Pause();
                }
            }
            isTapped = false;

        }
        if (Vector3.Distance(red.transform.position, hand.spheres[8].transform.position) > 0.2)
        {
            isTapped = false;
        }
    }
    public void drawLine(int type, Matrix4x4 mt)
    {
        if (type != 9 && type !=0)
        {
             line.SetActive(true);
            //      0,5,17
          
            Quaternion rot = mt.rotation;
            //rot.x = float.Parse(mt.rotation.x.ToString("#0.0"));
            //rot.y = float.Parse(mt.rotation.y.ToString("#0.0")) ;
            //rot.z = float.Parse(mt.rotation.z.ToString("#0.0")) ;
            //rot.w = float.Parse(mt.rotation.w.ToString("#0.0")) ;
            //float angle = Quaternion.Angle(line.transform.rotation,rot);
            //if(angle > 30)
            //{
            //    line.transform.rotation = Quaternion.Slerp(line.transform.rotation, rot, 0.3f);
            //}
            Vector3 v3 = rot.eulerAngles;

            //    Quaternion.Slerp
        //    if(v3.x > -4)
        
            Vector3.Slerp(line.transform.eulerAngles, new Vector3(v3.x, v3.y, 0),8f);
    //        line.transform.eulerAngles = new Vector3(v3.x, v3.y, v3.z);
            var target = new Vector3(mt[0, 3], mt[1, 3], mt[2, 3]) + originPos;
            var actualTarget = Vector3.Lerp(line.transform.position, target,
              Vector3.Distance(line.transform.position, target) * 8);
            line.transform.position = actualTarget;
            //   line.transform.position = hand.spheres[13].transform.position;
            Debug.DrawRay(hand.spheres[0].transform.position, line.transform.forward, Color.red);
            if (Physics.Raycast(line.transform.position, line.transform.forward, out RaycastHit hit, 100, layerMask))
            {
                Debug.DrawLine(hand.spheres[0].transform.position, hit.point, Color.red);
                line.transform.localScale = new Vector3(0.01f, 0.005f, hit.distance);
                line.transform.position = new Vector3(mt[0, 3], mt[1, 3], mt[2, 3] + hit.distance / 2) + originPos;

            }
            else
            {
                line.transform.localScale = new Vector3(0.01f, 0.005f, 10);
           
                //  line.transform.position = hand.spheres[13].transform.position;// + new Vector3(0, 0, 30 / 2);
            }
        }
        else
        {
            line.SetActive(false);
        }


        //      line.transform.forward = hand.spheres[8].transform.position - hand.spheres[7].transform.position;
       
    }
}
