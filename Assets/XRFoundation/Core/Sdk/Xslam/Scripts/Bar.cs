using System;
﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Bar : MonoBehaviour
{
    [Tooltip("Value"), Range(-25, 25)]
	public float value = 0;

 	public Slider negativeBar;
 	public Slider positiveBar;
 	
 	public float maxValue = 2;
 
  	public void SetMaxValue( float max )
 	{
 		maxValue = max;
        if( negativeBar ) negativeBar.maxValue = maxValue;
        if( positiveBar ) positiveBar.maxValue = maxValue;
 	}
 	
    // Start is called before the first frame update
    void Start()
    {
	    SetMaxValue( maxValue );
    }

    // Update is called once per frame
    void Update()
    {
    	if( negativeBar ) negativeBar.value = Math.Max( -value, 0.0f );
    	if( positiveBar ) positiveBar.value = Math.Max(  value, 0.0f );
    }
}
