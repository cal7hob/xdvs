using UnityEngine;
using System.Collections;

public class Odometer : MonoBehaviour
{
	private tk2dTextMesh textMesh;
	
	void Awake()
	{
		textMesh = GetComponent<tk2dTextMesh>();
	}

	void Update()
	{
		if (BattleController.MyVehicle)
			textMesh.text = string.Format("Odometer: {0}m", BattleController.MyVehicle.Odometer);
	}
}
