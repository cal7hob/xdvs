using System;
using UnityEngine;
using System.Collections;

public class FPSCalculator : MonoBehaviour
{
	public float normalFPSThreshold = 30;
	public float lowFPSThreshold = 25;
	public float refreshInterval = 1;
	
	private float lastCalcTime = 0;
	private int frameCount = 0;
	
	void Start()
	{
		if (!Debug.isDebugBuild)
		{
			gameObject.SetActive(false);
			return;
		}
        
		this.InvokeRepeating(Calc, 0, refreshInterval);
	}

	void Update()
	{
		frameCount++;
	}

	void Calc()
	{
		float deltaTime = Time.time - lastCalcTime;
		double fps = Math.Round((double)frameCount / deltaTime, 2);
		
		lastCalcTime = Time.time;
		frameCount = 0;

        
	}


    private void TurnCamera()
    {
    }
}
