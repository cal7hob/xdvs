using System.Collections;
using System.Collections.Generic;
using DemetriTools.Optimizations;
using UnityEngine;
using UnityEngineInternal;

public enum BoundsVertZone
{
    None,
    Bottom,
    Center,
    Top
}

public class AimingController : System.IDisposable
{
    private struct CheckedVehicle
    {
        public readonly VehicleController vehicle;
        public readonly Bounds bounds;
        public readonly float sqrDistance;
        public readonly Vector3 centerWorldPos;

        public CheckedVehicle(VehicleController veh, Bounds bnds, float dist, Vector3 cntWorldPos)
        {
            vehicle = veh;
            bounds = bnds;
            sqrDistance = dist;
            centerWorldPos = cntWorldPos;
        }
    }

    private class CheckedVehComparer : IComparer<CheckedVehicle>
    {
        public int Compare(CheckedVehicle veh1, CheckedVehicle veh2)
        {
            return veh1.sqrDistance < veh2.sqrDistance ? -1 : 1;
        }
    }

    private VehicleController owner;
    private Transform shotPoint;
    private float maxShootAngleCos;
    private List<CheckedVehicle> checkedVehicles = new List<CheckedVehicle>(10);
    private CheckedVehComparer checkedVehsComparer = new CheckedVehComparer();
    private RepeatingOptimizer aimRepeater;

    public VehicleController Target { get; private set; }
    public Vector3 TargetPosition { get; private set; }
    public bool CritZoneAimed { get; private set; }

    public AimingController(VehicleController owner, float updateInterval, float firstTimeDelay)
    {
        this.owner = owner;
        aimRepeater = new RepeatingOptimizer(updateInterval, 0);
        aimRepeater.Reset(firstTimeDelay);
        shotPoint = owner.AimingPoint;
        maxShootAngleCos = owner.MaxShootAngleCos;
        Target = null;
        CritZoneAimed = false;
        TargetPosition = Vector3.zero;
        if (owner.IsMain)
        {
            Messenger.Subscribe(EventId.MainTankAppeared, OnMainTankAppeared, 4);
        }

        Messenger.Subscribe(EventId.TankLeftTheGame, OnTankLeftTheGame);
    }

    public void Dispose()
    {
        Messenger.Unsubscribe(EventId.TankLeftTheGame, OnTankLeftTheGame);
        Messenger.Unsubscribe(EventId.MainTankAppeared, OnMainTankAppeared);
    }

    private void OnMainTankAppeared(EventId eid, EventInfo ei)
    {
        BattleGUI.HideGunSight();
    }

    public void Aiming()
    {
        if (!aimRepeater.AskPermission())
            return;

        VehicleController target = null;
        RaycastHit hit = new RaycastHit();

        if (!owner.Blinded)
        {
            #region Отбор юнитов, которые входят в обзор прицела и сортировка их в порядке отдаления

            checkedVehicles.Clear();
            Vector3 centerWorldPos = Vector3.zero;
            foreach (VehicleController vehicle in BattleController.allVehicles.Values)
            {
                if (vehicle == owner
                    || VehicleController.AreFriends(owner, vehicle)
                    ||
                    Vector3.Dot(owner.AimingPoint.forward, vehicle.transform.position - owner.AimingPoint.position) < 0)
                    continue;

                Bounds aimBounds = vehicle.EntireBounds;
                if (BoundsInSightYZ(aimBounds, ref centerWorldPos))
                    checkedVehicles.Add(new CheckedVehicle(vehicle, aimBounds,
                        Vector3.SqrMagnitude(vehicle.transform.position - owner.transform.position), centerWorldPos));
            }
            checkedVehicles.Sort(checkedVehsComparer);

            #endregion

            #region Непосредственная проверка доступности юнитов лучами
            for (int i = 0; i < checkedVehicles.Count; i++)
            {
                CheckedVehicle checkedVeh = checkedVehicles[i];
                if (CheckVehZoneHit(checkedVeh.vehicle, checkedVeh.bounds, checkedVeh.centerWorldPos, out hit, true))
                {
                    target = checkedVeh.vehicle;
                    CritZoneAimed = true;
                    break;
                }
                if (
                    CheckVehZoneHit(checkedVeh.vehicle, checkedVeh.bounds, checkedVeh.centerWorldPos, out hit, false,
                        BoundsVertZone.Center)
                    ||
                    CheckVehZoneHit(checkedVeh.vehicle, checkedVeh.bounds, checkedVeh.centerWorldPos, out hit, false,
                        BoundsVertZone.Bottom)
                    ||
                    CheckVehZoneHit(checkedVeh.vehicle, checkedVeh.bounds, checkedVeh.centerWorldPos, out hit, false,
                        BoundsVertZone.Top)
                    )
                {
                    target = checkedVeh.vehicle;
                    CritZoneAimed = false;
                    break;
                }
            }
            #endregion
        }

        if (target != null)
        {
            TargetPosition = hit.point;
            if (owner.IsMain)
            {
                BattleGUI.ShowGunSightForBounds(target.EntireBounds);
            }
        }
        else
        {
            if (owner.IsMain)
            {
                BattleGUI.HideGunSight();
            }
        }

        if (Target == target)
        {
            return;
        }

        if (Target != null)
        {
            int targetId = Target.data.playerId;
            if (owner.IsMain)
            {
                Target.SetMarkedStatus(false);
            }

            Target = null;
            Messenger.Send(EventId.TargetAimed, new EventInfo_IIB(owner.data.playerId, targetId, false));
         }

        Target = target;
        if (target != null)
        {
            Messenger.Send(EventId.TargetAimed, new EventInfo_IIB(owner.data.playerId, Target.data.playerId, true));
            if (owner.IsMain)
            {
                Target.SetMarkedStatus(true);
            }
        }
    }

    private bool BoundsInSightYZ(Bounds bounds, ref Vector3 centerWorldPos)
    {
        Vector3 aimInShotCoord = shotPoint.InverseTransformPoint(bounds.center);
        aimInShotCoord.x = 0f;
        centerWorldPos = shotPoint.TransformPoint(aimInShotCoord);
        Ray ray = new Ray(shotPoint.position, (centerWorldPos - shotPoint.position).normalized);

        return bounds.IntersectRay(ray);
    }

    private bool CheckVehZoneHit(VehicleController vehicle, Bounds bounds, Vector3 centerPos, out RaycastHit hit, bool weCheckCrit, BoundsVertZone zone = BoundsVertZone.None)
    {
        if (weCheckCrit && vehicle.CritZonePlace != BoundsVertZone.None)
        {
            zone = vehicle.CritZonePlace;
        }
        else if (zone == owner.CritZonePlace)
        {
            hit = new RaycastHit();
            return false;
        }

        switch (zone)
        {
            case BoundsVertZone.Bottom:
                centerPos.y = bounds.min.y + 0.05f;
                break;
            case BoundsVertZone.Center:
                centerPos.y = bounds.center.y;
                break;
            case BoundsVertZone.Top:
                centerPos.y = bounds.max.y - 0.05f;
                break;
        }

        Vector3 direction = (centerPos - shotPoint.position).normalized;
        if (Vector3.Dot(shotPoint.forward, direction) < maxShootAngleCos)
        {
            hit = new RaycastHit();
            return false;
        }

        if (!Physics.Raycast(shotPoint.position, direction, out hit, owner.MaxAimDistance, owner.HitMask, QueryTriggerInteraction.Collide) ||
            hit.rigidbody != vehicle.Rb)
            return false;

        if (!weCheckCrit || hit.collider.CompareTag("CritZone"))
        {
            hit.point = centerPos;
            if (owner.IsMain) // Пока дистанция нужна только главному юниту (для 2d-прицела)
            {
                hit.distance = Vector3.Distance(centerPos, shotPoint.position);
            }
            return true;
        }

        return false;
    }

    private void OnTankLeftTheGame(EventId eid, EventInfo ei)
    {
        EventInfo_I info = ei as EventInfo_I;
        if (Target != null && Target.data.playerId == info.int1)
        {
            Target = null;
            Messenger.Send(EventId.TargetAimed, new EventInfo_IIB(owner.data.playerId, info.int1, false));
            if (owner.IsMain)
            {
                BattleGUI.HideGunSight();
            }
        }
    }
}
