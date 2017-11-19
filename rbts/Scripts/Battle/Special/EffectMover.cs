using UnityEngine;
using System.Collections;

public class EffectMover : MonoBehaviour
{
	private Transform target;

    public Transform Target
    {
        get { return target; }
        set
        {
            target = value;
            enabled = target != null;
        }
    }
    /* UNITY SECTION */

    void OnEnable()
    {
        Target = null;
    }

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
