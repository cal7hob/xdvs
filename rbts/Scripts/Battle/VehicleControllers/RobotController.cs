using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotController : TankController
{
    private const float MIN_TURN_ROTATION = 0.005f;
    [Header("Анимации")]
    public bool animateWalking = true;
    public float walkSpeedRatio = 1f;
    public float rotationSpeedRatio = 1f;

    [Header("Звуки")]

    public AudioClip[] stepClips;
    
    // Ids для параметров аниматора
    private int animID_walk;
    private int animID_rotate;
    private int animID_walkSpeed;
    private int animID_turretAngle;
    private int animID_flagAngle;

    private float animSpeed;
    private int walkState;
    private int rotState;

    private AudioSource stepAudio;

    protected override void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        base.OnPhotonInstantiate(info);
        GetAnimatorIds();
        weaponController = new HotGun(this);
        if (IsMain)
        {
            SetupStepAudio();
        }
    }

    protected override void Update()
    {
        base.Update();
        if (animator != null)
        {
            CalcAnimState();
        }
    }

    public override void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        base.OnPhotonSerializeView(stream, info);
        if (stream.isWriting)
        {
            if (isExploded)
                return;

            if (animateWalking)
            {
                stream.SendNext(animator.GetInteger(animID_walk));
                stream.SendNext(animator.GetInteger(animID_rotate));
            }
        }
        else
        {
            if (animateWalking)
            {
                walkState = (int) stream.ReceiveNext();
                rotState = (int) stream.ReceiveNext();
            }
        }
    }

    private void GetAnimatorIds()
    {
        animID_walk = Animator.StringToHash("walk");
        animID_rotate = Animator.StringToHash("rot");
        animID_walkSpeed = Animator.StringToHash("WalkSpeed");
        animID_flagAngle = Animator.StringToHash("flag_angle");
        animID_turretAngle = Animator.StringToHash("turret_angle");
    }

    private void CalcAnimState()
    {
        #region Поворот башни в аниматор
        float turretAngle = turret.localEulerAngles.y;

        float flagAngleParam =
            turretAngle < 180f
                ? turretAngle / 180f
                : (360 - turretAngle) / -180f;
        float turretAngleParam = 1f - turretAngle / 360f;

        animator.SetFloat(animID_flagAngle, flagAngleParam);
        animator.SetFloat(animID_turretAngle, turretAngleParam);
        #endregion

        #region Анимация ходьбы
        if (animateWalking)
        {
            float speedForward = PhotonView.isMine ? curMaxSpeed : LocalVelocity.z;
            walkState = Mathf.Abs(speedForward) < 0.02f ? 0 : (int) Mathf.Sign(speedForward);
            if (walkState != 0)
            {
                // Идём
                animSpeed = Mathf.Approximately(walkState, 0f)
                    ? 1f
                    : Mathf.Abs(walkSpeedRatio * Vector3.Dot(transform.forward, Velocity));
                rotState = 0;
            }
            else
            {
                //Стоим или поворачиваемся на месте
                float angularY = PhotonView.isMine
                    ? transform.InverseTransformDirection(rb.angularVelocity).y
                    : rotSyncSpeed * Mathf.Deg2Rad;
                if (!OnGround || Mathf.Abs(angularY) < MIN_TURN_ROTATION)
                {
                    // Нет разворота
                    rotState = 0;
                    animSpeed = 1f;
                }
                else
                {
                    //Разворот на месте
                    rotState = (int) Mathf.Sign(angularY);
                    animSpeed = Mathf.Abs(angularY * rotationSpeedRatio);
                }
            }

            animator.SetInteger(animID_walk, walkState);
            animator.SetFloat(animID_walkSpeed, animSpeed);
            animator.SetInteger(animID_rotate, rotState);
        }
        #endregion
    }

    public override VehicleInfo.VehicleType VehicleType
    {
        get { return VehicleInfo.VehicleType.Robot; }
    }

    private void Step()
    {
        if (!IsMain)
            return;

        stepAudio.PlayOneShot(stepClips.GetRandomItem(), Settings.SoundVolume * SoundSettings.ROBOT_STEPS_VOLUME);
    }

    private void SetupStepAudio()
    {
        stepAudio = gameObject.AddComponent<AudioSource>();
        stepAudio.loop = false;
        stepAudio.rolloffMode = AudioRolloffMode.Linear;
        stepAudio.volume = 1f;
        stepAudio.maxDistance = 25;
        stepAudio.dopplerLevel = 0;
    }
}
