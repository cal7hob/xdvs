using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IKHangarController : MonoBehaviour 
{
    [Header("Looking")]  
    public float lookIKWeight;
    public float bodyWeight;
    public float headWeight;
    public float eyesWeight;
    public float clampWeight;
    [Header("Hands")]


    public float offsetY;
    [SerializeField]
    private Animator animator;

    private Transform leftHandTarget = null;
    private Transform rightHandTarget = null;
    private Transform lookTarget = null;

    public void SetTargets(Transform leftHandTarget, Transform rightHandTarget, Transform lookTarget) 
    {
        this.leftHandTarget = leftHandTarget;
        this.rightHandTarget = rightHandTarget;
        this.lookTarget = lookTarget;
    }

    //a callback for calculating IK
    void OnAnimatorIK()
    {
        if ( animator == null) 
        {
            return;
        }
       /* if (shoulderTarget != null) 
        {
            animator.SetIKHintPositionWeight(AvatarIKGoal., 1);

            animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandTarget.position);
            animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandTarget.rotation);
        }*/

        //-------------------Head-------------------------------------------
        if(lookTarget != null)
        {
            animator.SetLookAtWeight(lookIKWeight, bodyWeight, headWeight, eyesWeight, clampWeight);//lookIKWeight, bodyWeight, headWeight, eyesWeight, clampWeight);
            animator.SetLookAtPosition(lookTarget.position);
        }
        //-------------------Hands------------------------------------------
        if (leftHandTarget != null)
        {
            //Debug.Log("**");
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1);

            animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandTarget.position);
            animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandTarget.rotation);
        }
        else
        {
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0);
            animator.SetLookAtWeight(0);
        }

        if (rightHandTarget != null)
        {
            //Debug.Log("**");
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1);

            animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandTarget.position);
            animator.SetIKRotation(AvatarIKGoal.RightHand, rightHandTarget.rotation);
        }
        else
        {
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0);
            animator.SetLookAtWeight(0);
        }    
    }
}
