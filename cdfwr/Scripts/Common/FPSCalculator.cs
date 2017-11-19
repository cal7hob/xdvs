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
	private tk2dTextMesh textMesh;
	
	void Start()
	{
		if (!Debug.isDebugBuild)
		{
			gameObject.SetActive(false);
			return;
		}
        
        textMesh = GetComponent<tk2dTextMesh>();
		if (!textMesh)
		{
			Debug.LogError("No tk2dTextMesh component to show FPS");
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
		textMesh.text = string.Format("FPS: {0}", fps);
		if (fps < lowFPSThreshold)
			textMesh.color = Color.red;
		else if (fps < normalFPSThreshold)
			textMesh.color = Color.yellow;
		else
			textMesh.color = Color.green;

		lastCalcTime = Time.time;
		frameCount = 0;

        
	}

    public void TurnCameraOff(tk2dUIItem item)
    {
        this.InvokeRepeating(TurnCamera, 0, 10);
    }

    private void TurnCamera()
    {
        tk2dCamera.Instance.GetComponent<Camera>().enabled = !tk2dCamera.Instance.GetComponent<Camera>().enabled;
    }
}
