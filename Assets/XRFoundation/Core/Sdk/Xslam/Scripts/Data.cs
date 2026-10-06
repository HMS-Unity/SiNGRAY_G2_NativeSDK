using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Data
{
    [System.Serializable]
    public class HandData
    {
        public Vector3 origin;
        public List<Vector3> joints;
        public float distX;
        public float distY;
        public float vert;
    }

    public HandData dataL;
    public HandData dataR;
    public int frameWidth;
    public int frameHeight;
}