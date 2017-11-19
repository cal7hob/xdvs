using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class SoldierController
{
    private const float averageSpeed = 1.831f; //Обычная скорость передвижения. Берется из анимаций заполняется вручную
    private const float averageRunSpeed = 3.243f;//Обычная скорость бега. Берется из анимаций заполняется вручную
    private const float averageReloadTime = 2.167f; //Обычное время перезарядки. Берется из анимаций заполняется вручную
    private const float runSpeedFactor = 1.5f; //по умолчанию скорость бега равна скорости ходьбы. Если хотим ускориться выставляем данное значение на величину >1


    private int turnLeft_ = Animator.StringToHash("turnLeft");
    private int turnRight_ = Animator.StringToHash("turnRight");
    private int turnLeft_90_ = Animator.StringToHash("turnLeft_90");
    private int turnRight_90_ = Animator.StringToHash("turnRight_90");
    private int aimingXAngle_ = Animator.StringToHash("aimingXAngle");
    private int aimingYAngle_ = Animator.StringToHash("aimingYAngle");
    private int xAxis_ = Animator.StringToHash("xAxis");
    private int yAxis_ = Animator.StringToHash("yAxis");
    private int isRun_ = Animator.StringToHash("isRun");
    private int isReload_ = Animator.StringToHash("isReload");
    private int isAiming_ = Animator.StringToHash("isAiming");
    private int isDead_ = Animator.StringToHash("isDead");
    private int forceResp_ = Animator.StringToHash("forceResp");
    private int speedFactor_ = Animator.StringToHash("speedFactor");
    private int runFactor_ = Animator.StringToHash("runFactor");
    private int reloadFactor_ = Animator.StringToHash("reloadFactor");
   

    private bool turn = false;

    public void StandBodyRot(Vector2 dir) 
    {
        if (dir.y < 64.28) //угол больше 50 -> требуется доворот корпуса
        {
            if (!turn)
            {
                turn = true;
                animator.SetBool(dir.x < 0 ? turnLeft_90_: turnRight_90_, true);              
            }
        }
        else //доворот не требуется
        {
            turn = false;
            animator.SetBool(turnLeft_, false);
            animator.SetBool(turnRight_, false);
            animator.SetBool(turnLeft_90_, false);
            animator.SetBool(turnRight_90_, false);
        }
    }


    public void SetAimingAngles(float x, float y) 
    {
        animator.SetFloat(aimingXAngle_, -x * 100);
        animator.SetFloat(aimingYAngle_, y * 100);
    }
    public void SetControls(float x, float y) 
    {
        animator.SetFloat(yAxis_, y);
        animator.SetFloat(xAxis_, x);
    }

    public void SetRun(bool on) 
    {
        animator.SetBool(isRun_, on);
    }
    public void SetReload(bool on) 
    {
        animator.SetBool(isReload_, on);
    }
    public void SetAiming(bool on) 
    {
        animator.SetBool(isAiming_, on);
    }
    public void SetDeath(bool on)
    {
        animator.SetBool(isDead_, on);
    }

    public void UpdateAnimatorParams(float speed, float reloadTime)
    {
        animator.SetFloat(speedFactor_, speed * 0.1f / averageSpeed);
        animator.SetFloat(runFactor_, speed * 0.1f * runSpeedFactor / averageRunSpeed);
        animator.SetFloat(reloadFactor_, averageReloadTime / reloadTime);
    }
    public void OnForceResp() 
    {
        turn = false;
        
        animator.SetBool(turnLeft_, false);
        animator.SetBool(turnRight_, false);
        animator.SetBool(turnLeft_90_, false);
        animator.SetBool(turnRight_90_, false);
        animator.SetTrigger(forceResp_);
    }

}
