using UnityEngine;
using System.Collections;

public class Centerer : MonoBehaviour
{
	public MeshRenderer[] renderers;
	private Vector3 center;


	/*	PUBLIC SECTION	*/
	
	public void AlignObjects()
	{
		CalcCenter();
		Vector3 delta = transform.position - center;
		foreach (MeshRenderer rend in renderers)
			rend.transform.position += delta;
	}


	/*	PRIVATE SECTION	*/
	
	private void CalcCenter()
	{
		Vector3 min = Vector3.one * float.MaxValue;
		Vector3 max = -min;
		foreach (MeshRenderer rend in renderers)
		{
			if (rend.bounds.min.x < min.x)
				min.x = rend.bounds.min.x;
			if (rend.bounds.min.y < min.y)
				min.y = rend.bounds.min.y;
			if (rend.bounds.max.x > max.x)
				max.x = rend.bounds.max.x;
			if (rend.bounds.max.y > max.y)
				max.y = rend.bounds.max.y;
			min.z = transform.position.z;
			max.z = min.z;
		}

		center = (min + max) / 2;
	}
}
