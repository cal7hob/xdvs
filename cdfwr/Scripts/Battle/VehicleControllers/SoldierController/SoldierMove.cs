using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class SoldierController
{
    private float rotationSpeed = 10f;

    public bool isMove { get; private set; }
  //  private bool isRun = false;
    private bool tryRun = false;

    private float waitinTime = 1.0f;
    private Coroutine waitForRun = null;

    bool IsTryRun
    {
        get { return tryRun; }
        set
        {
            if (value == tryRun)
            {
                return;
            }

            tryRun = value;
            if (tryRun)
            {
                if (waitForRun != null)
                {
                    StopCoroutine(waitForRun);
                }
                waitForRun = StartCoroutine(RunRoutine());
            }
            else
            {
                PhotonView.RPC("Run", PhotonTargets.All, data.playerId, false);
                StopCoroutine(waitForRun);
               // isRun = false;
            }
        }
    }

    private IEnumerator RunRoutine()
    {
        yield return new WaitForSeconds(waitinTime);
        PhotonView.RPC("Run", PhotonTargets.All, data.playerId, true);
    }

    [PunRPC]
    public void Run(int playerId, bool on)
    {
        //   if (data.playerId == playerId)
        {
            SetRun(on);
        }
    }

    public override void MovePlayer()
    {
        if (!IsMine)
        {
            MoveClone();
            return;
        }
       
        rb.MovePosition(Body.position);
        Body.localPosition = Vector3.zero;
        if (IsBot) 
        {
            if (Mathf.Abs(XAxisControl) > 0.1f || Mathf.Abs(YAxisControl) > 0.1f) 
            {
                isMove = true;
            }
            else 
            {
                isMove = false;
                ikController.Stop();
            }
        }
        else
        {
            if (YAxisControl < -0.1)//движемся назад
            {
                IsTryRun = false;
                isMove = true;
            }
            else if (YAxisControl > 0.1 || Mathf.Abs(XAxisControl) > 0.1f)//движемся в любую другую сторону
            {
                IsTryRun = true;
                isMove = true;
            }
            else
            {
                IsTryRun = false;
                isMove = false;
                ikController.Stop();
            }
        }

        //------------------------------body--------------------------------------------------------
        if (isMove)
        {
            MoveBodyRot();
        }
        else 
        {
            StandBodyRot(camSightPoint);
        }
        //------------------------------spine-------------------------------------------------------
        SpineRot(camSightPoint);
        //------------------------------gun---------------------------------------------------------
        if (isAiming)
        {
            weapon.LookAt(camSightPoint);
        }
        //------------------------------------------------------------------------------------------

        rb.centerOfMass = centerOfMass;

        SetControls(XAxisControl, YAxisControl);

        curMaxSpeed = maxSpeed * yAxisAcceleration;
        curMaxRotationSpeed = BattleCamera.Instance.IsZoomed ? ZoomRotationSpeed : RotationSpeed;

        if (Mathf.Abs(curMaxSpeed) > MOVEMENT_SPEED_THRESHOLD || Mathf.Abs(curMaxRotationSpeed) > MOVEMENT_SPEED_THRESHOLD)
        {
            MarkActivity();
        }
    }

    public override void MoveClone() 
    {
        if (IsDead)
        {
            //transform.position = correctPosition;
            Body.localPosition = Vector3.zero;
            return;
        }
        isMove = Mathf.Abs(correctControls.y) > 0.1f || Mathf.Abs(correctControls.x) > 0.1f;

        camSightPoint = correctCamSinghtPoint;
        transform.position = Vector3.SmoothDamp(transform.position, correctPosition, ref posSyncVelocity, syncTime);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, correctRotation, rotSyncSpeed * Time.deltaTime);
        Body.localPosition = Vector3.zero;
        Body.localRotation = zeroRot;
     
        //------------------------------body--------------------------------------------------------
        if (!isMove)
        {
            StandBodyRot(correctCamSinghtPoint);
        }
        //------------------------------spine-------------------------------------------------------
        SpineRot(correctCamSinghtPoint);
        //------------------------------gun---------------------------------------------------------
        if (isAiming)
        {
            weapon.LookAt(correctGunSinghtPoint);
        }
        //------------------------------------------------------------------------------------------
        rb.centerOfMass = centerOfMass;
        SetControls(correctControls.x, correctControls.y);
        
        curMaxSpeed = maxSpeed * yAxisAcceleration;
        curMaxRotationSpeed = BattleCamera.Instance.IsZoomed ? ZoomRotationSpeed : RotationSpeed;

        if (Mathf.Abs(curMaxSpeed) > MOVEMENT_SPEED_THRESHOLD || Mathf.Abs(curMaxRotationSpeed) > MOVEMENT_SPEED_THRESHOLD)
        {
            MarkActivity();
        }
    }

    private void MoveBodyRot() 
    {
        var bodyDir = camSightPoint - Body.transform.position;
        bodyDir.y = 0;
        Vector3 newDir = Vector3.RotateTowards(Body.transform.forward, bodyDir, rotationSpeed * Time.deltaTime, 0.0F);
        Body.rotation = Quaternion.LookRotation(newDir);
    }

    private void StandBodyRot(Vector3 point)
    {
        var dir_ = Body.transform.InverseTransformPoint(point);
        StandBodyRot(new Vector2(dir_.x, dir_.z).normalized * 100f);
    }

    private void SpineRot(Vector3 point)
    {
        Vector2 angles;
        var dir_ = Body.transform.InverseTransformDirection(point - weaponSpawner.transform.position);//weapon.transform.position


        if (IsMine)
        {
            angles = GetAimingDirection(ref dir_);
            aimingAngles = angles;
        }
        else
        {
            angles = correctAimingAngles;
        }
        SetAimingAngles(angles.x, angles.y);
    }

    private Vector2 GetAimingDirection(ref Vector3 localDir)
    {
        Vector2 angles;

        if (localDir.z <= 0)
        {
            angles.x = 90 * localDir.x > 0 ? 1 : -1;
        }
        else
        {
            angles.x = Mathf.Atan(localDir.x / localDir.z);
        }
        angles.y = Mathf.Atan(localDir.y / new Vector2(localDir.x, localDir.z).magnitude);
        return angles;
    }
}
