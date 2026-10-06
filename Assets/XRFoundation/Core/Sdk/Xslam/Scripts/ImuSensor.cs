using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImuSensor : MonoBehaviour
{
 	public Canvas xAxis;
 	public Canvas yAxis;
 	public Canvas zAxis;
 	
 	public float xValue = 0;
 	public float yValue = 0;
 	public float zValue = 0;
 	
 	public float maxValue = 2;
 	
	public void SetValues( float x, float y, float z )
 	{
 		xValue = x;
 		yValue = y;
 		zValue = z;
 	}
 	
 	public void Set( Vector3 value )
 	{
 		xValue = value.x;
 		yValue = value.y;
 		zValue = value.z;
 	}
 	
    // Start is called before the first frame update
    void Start()
    {
        if( xAxis ) xAxis.GetComponent<Bar>().SetMaxValue(maxValue);
        if( yAxis ) yAxis.GetComponent<Bar>().SetMaxValue(maxValue);
        if( zAxis ) zAxis.GetComponent<Bar>().SetMaxValue(maxValue);
        
    }

    // Update is called once per frame
    void Update()
    {
        if( xAxis ) xAxis.GetComponent<Bar>().value = xValue;
        if( yAxis ) yAxis.GetComponent<Bar>().value = yValue;
        if( zAxis ) zAxis.GetComponent<Bar>().value = zValue;
    }
}
