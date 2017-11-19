using UnityEngine;
using System.Collections;

public class EffectMover : MonoBehaviour
{
	public Transform target;
	/* UNITY SECTION */
	
	void Update()
	{
		if (target)
		{
			transform.position = target.position;
			transform.rotation = target.rotation;
		}
	}
	
	/* PUBLIC SECTION */
	
		
	/* PRIVATE SECTION */
}
