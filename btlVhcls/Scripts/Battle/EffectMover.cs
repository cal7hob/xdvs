using UnityEngine;

public class EffectMover : MonoBehaviour
{
	private Transform target;
    private Vector3 localPositionToTarget;

	void Update()
	{
	    Move();
	}

    public void SetTarget(Transform target)
    {
        this.target = target;
        localPositionToTarget = target.InverseTransformPoint(transform.position);
    }

    private void Move()
    {
        if (target != null)
            transform.position = target.TransformPoint(localPositionToTarget);
    }
 }
