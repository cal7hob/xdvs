using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class SoldierController
{
    private Transform firePoint;
    private Vector3 fireDirPoint;
    private Vector3 targetDir;
    public Vector2 aimingAngles;

    public RaycastHit AimingHit
    {
        get { return gunsightHit; }
    }
    protected RaycastHit gunsightHit;
    public bool HasHit
    {
        get { return hasHit; }
    }
    protected bool hasHit;
    protected bool hasAimed;
    private VehicleController target;

    public Vector3 camSightPoint;
    public Vector3 gunSightPoint;

    public override Vector3 CamSightPoint
    {
        get { return camSightPoint; }
    }

    protected virtual void Gunsight()
    {
        camSightPoint = BattleCamera.Instance.Cam.transform.position + BattleCamera.Instance.Cam.transform.forward * 50f;
        MoveStaticGunsight(camSightPoint);

        if (!IsAiming)
        {
            gunSightPoint = camSightPoint;
            hasHit = false;
            return;
        }

        if (Physics.Raycast(BattleCamera.Instance.Cam.transform.position, BattleCamera.Instance.Cam.transform.forward, out gunsightHit, 500f, HitMask))
        {
            OnWeaponAimed();
            gunSightPoint = gunsightHit.point;
            targetDir = (gunSightPoint - weapon.position).normalized;

            if (Physics.Raycast(weaponSpawner.transform.position, targetDir, out gunsightHit, 500f, HitMask))
            {
                gunSightPoint = gunsightHit.point;
            }
        }
        else 
        {
            gunSightPoint = camSightPoint;
            hasHit = false;
        }
    }

    protected void OnWeaponAimed()
    {

        target = gunsightHit.collider.GetComponentInParent<SoldierController>();
        if (BattleController.Instance.BattleMode == GameData.GameMode.Team && target != null && target.data.teamId == data.teamId)
        {
            target = null;
        }
        hasHit = true;
        if (target != null && !target.IsDead)
        {
            Target = target;
            if (!hasAimed)
            {
                Dispatcher.Send(EventId.TargetAimed, new EventInfo_IIB(data.playerId, target.PlayerId, true));
                hasAimed = true;
                TargetAimed = true;
            }
        }
        else
        {
            if (hasAimed)
            {
                Dispatcher.Send(EventId.TargetAimed, new EventInfo_IIB(data.playerId, data.playerId, false));
                hasAimed = false;
                TargetAimed = false;
            }
        }
    }

    protected virtual void MoveStaticGunsight(Vector3 point)
    {
        if (BattleGUI.Instance.StaticGunsight == null)
        {
            return;
        }
        Vector3 sightPoint = Camera.main.WorldToViewportPoint(point);
        sightPoint = BattleGUI.Instance.GuiCamera.ViewportToWorldPoint(sightPoint);
        BattleGUI.Instance.StaticGunsight.transform.position = sightPoint;
    }
    public override void OnAimingStatusChange(bool on)
    {
        ikController.SetAimingStatus(on);
        //начали целиться. Перемещаем оружие в нужное место в иерархии, сообщаем аниматору и включаем IK
        // прекращаем целиться. Перемещаем оружие в правую руку, отключаем IK и прицеливание 
        isAiming = on;
        SetAiming(on);

        weapon.SetParent(on ? gunLocation : rightHand);
        if (on)
        {
            weapon.localPosition = Vector3.zero;
        }
    }

    public override void SetAimingPoint(Vector3 aimingPoint) // пришлось так сделать
    {
        camSightPoint = aimingPoint;
    }

    /*
    public override bool IsAiming
    {
        get { return isAiming; }
        set
        {
            if (value != isAiming)
            {
                //начали целиться. Перемещаем оружие в нужное место в иерархии, сообщаем аниматору и включаем IK
                // прекращаем целиться. Перемещаем оружие в правую руку, отключаем IK и прицеливание 
                isAiming = value;
                SetAiming(value);

                weapon.SetParent(value ? gunLocation : rightHand);
                if (value)
                {
                    weapon.localPosition = Vector3.zero;
                }
            }
        }
    }*/
}
