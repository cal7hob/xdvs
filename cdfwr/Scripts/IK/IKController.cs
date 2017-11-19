using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(Animator))]
public class IKController : MonoBehaviour
{
    private Step stat = Step.None;
    private enum Step
    {
        Right,
        Left,
        None
    }

    [Header("Looking")]  
    public float lookIKWeight;
    public float bodyWeight;
    public float headWeight;
    public float eyesWeight;
    public float clampWeight;
    
    [SerializeField]
    private SoldierController soldier;
    [SerializeField]
    private Animator animator;

    private AvatarIKGoal leftHand;
    private AvatarIKGoal rightHand;

    private Transform lookTarget;
    private Transform leftTarget;
    private Transform rightTarget;

    public void AssignSoldierController(SoldierController soldier)
    {
        this.soldier = soldier;
    }

    private void Awake() 
    {
        leftHand = AvatarIKGoal.LeftHand; 
        rightHand = AvatarIKGoal.RightHand;
    }

    #region IK
    public void SetTargets(Transform leftTarget, Transform rightTarget, Transform lookTarget) 
    {
        this.leftTarget = leftTarget;
        this.rightTarget = rightTarget;
        this.lookTarget = lookTarget;
    }
    public void SetAimingStatus(bool on)
    {
        /*changeWeight = true;
        
        if (animator == null) 
        {
            return;
        }
        if (on) 
        { 
            EnableIK(); 
        } 
        else 
        { 
            DisableIK(); 
        }*/
    }
    private void OnAnimatorIK()
    {

        if (soldier == null || animator == null) 
        {
            return;
        }
        if (!soldier.IsAiming)
        {
            DisableIK();
            return;
        }

        EnableIK();
        //-------------------Head-------------------------------------------
        if (lookTarget != null)
        {
            animator.SetLookAtPosition(lookTarget.position);
        }

        //-------------------Hands------------------------------------------
        if (leftTarget != null)
        {
            animator.SetIKPosition(leftHand, leftTarget.position);
            animator.SetIKRotation(leftHand, leftTarget.rotation);
        }

        if (rightTarget != null)
        {
            animator.SetIKPosition(rightHand, rightTarget.position);
            animator.SetIKRotation(rightHand, rightTarget.rotation);
        }
    }

    private void EnableIK() 
    {
        if (lookTarget != null)
        {
            animator.SetLookAtWeight(lookIKWeight, bodyWeight, headWeight, eyesWeight, clampWeight);
        }

        //-------------------Hands------------------------------------------
        if (leftTarget != null)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1f);//leftHand
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1f);//leftHand
        }

        if (rightTarget != null)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1f);//rightHand
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1f);//rightHand
        }
    }

    private void DisableIK() 
    {
        animator.SetLookAtWeight(0);
        //-------------------Hands------------------------------------------
       
        animator.SetIKPositionWeight(leftHand, 0);
        animator.SetIKRotationWeight(leftHand, 0);
       
        animator.SetIKPositionWeight(rightHand, 0);
        animator.SetIKRotationWeight(rightHand, 0);
    }

    #endregion
    //-----------------------AnimationEvents-------------------------
    #region Steps

    public void Stop() 
    {
        stat = Step.None;
    }

    private void LeftStep()
    {
        if (stat == Step.Left)
        {
            return;
        }
        stat = Step.Left;
        soldier.Step();
    }

    private void RightStep()
    {
        if (stat == Step.Right)
        {
            return;
        }
        stat = Step.Right;
        soldier.Step();
    }
    #endregion

    #region Recharging
    private void Recharging() 
    {
        soldier.PlayReloadSound(false);
    }
    #endregion
}