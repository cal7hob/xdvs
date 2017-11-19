using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngineInternal;

//public enum BoundsVertZone
//{
//    None,
//    Bottom,
//    Center,
//    Top
//}

public class AimingController
{
    //    private struct CheckedVehicle
    //    {
    //        public readonly VehicleController vehicle;
    //        public readonly Bounds bounds;
    //        public readonly float sqrDistance;
    //        public readonly Vector3 centerWorldPos;

    //        public CheckedVehicle(VehicleController veh, Bounds bnds, float dist, Vector3 cntWorldPos)
    //        {
    //            vehicle = veh;
    //            bounds = bnds;
    //            sqrDistance = dist;
    //            centerWorldPos = cntWorldPos;
    //        }
    //    }

    //    private class CheckedVehComparer : IComparer<CheckedVehicle>
    //    {
    //        public int Compare(CheckedVehicle veh1, CheckedVehicle veh2)
    //        {
    //            return veh1.sqrDistance < veh2.sqrDistance ? -1 : 1;
    //        }
    //    }

    //    private VehicleController owner;
    //    private Transform shotPoint;
    //    private int hitMask;
    //    private List<CheckedVehicle> checkedVehicles = new List<CheckedVehicle>(10);
    //    private CheckedVehComparer checkedVehsComparer = new CheckedVehComparer();

    //    public VehicleController Target { get; private set; }
    //    public Vector3 TargetPosition { get; private set; }
    //    public bool CritZoneAimed { get; private set; }


    //    public AimingController(VehicleController owner)
    //    {
    //        this.owner = owner;
    //        shotPoint = owner.ShotPoint;
    //        ResetTarget();
    //        CritZoneAimed = false;
    //    }

    //    public void ResetTarget()
    //    {
    //        Target = null;
    //    }

    public void Aiming()
    {
        //#region Отбор вехов, которые входят в обзор прицела и сортировка их в порядке отдаления

        //checkedVehicles.Clear();
        //foreach (VehicleController vehicle in BattleController.allVehicles.Values)
        //{
        //    if (vehicle == owner || StaticContainer.AreFriends(owner, vehicle))
        //    {
        //        continue;
        //    }

        //    Vector3 centerWorldPos;
        //    Bounds aimBounds = vehicle.GetEntireAimBounds();

        //    if (BoundsInSightYZ(vehicle, aimBounds, out centerWorldPos))
        //    {
        //        checkedVehicles.Add(new CheckedVehicle(vehicle, aimBounds, Vector3.SqrMagnitude(vehicle.transform.position - owner.transform.position), centerWorldPos));
        //    }
        //}
        //checkedVehicles.Sort(checkedVehsComparer);
        //#endregion

        //CheckedVehicle checkedVeh;
        //VehicleController target = null;
        //RaycastHit hit = new RaycastHit();

        //for (int i = 0; i < checkedVehicles.Count; i++)
        //{
        //    checkedVeh = checkedVehicles[i];
        //    if (CheckVehZoneHit(checkedVeh.vehicle, checkedVeh.bounds, checkedVeh.centerWorldPos, out hit, true))
        //    {
        //        target = checkedVeh.vehicle;
        //        CritZoneAimed = true;
        //        break;
        //    }
        //    if (
        //        CheckVehZoneHit(checkedVeh.vehicle, checkedVeh.bounds, checkedVeh.centerWorldPos, out hit, false, BoundsVertZone.Center)
        //        || CheckVehZoneHit(checkedVeh.vehicle, checkedVeh.bounds, checkedVeh.centerWorldPos, out hit, false, BoundsVertZone.Bottom)
        //        || CheckVehZoneHit(checkedVeh.vehicle, checkedVeh.bounds, checkedVeh.centerWorldPos, out hit, false, BoundsVertZone.Top)
        //    )
        //    {
        //        target = checkedVeh.vehicle;
        //        CritZoneAimed = false;
        //        break;
        //    }
        //}

        //if (Target == target)
        //{
        //    return;
        //}

        //if (Target != null)
        //{
        //    int targetId = Target.data.playerId;
        //    if (owner.IsMain)
        //    {
        //        owner.turretController.ResetAimingState();

        //        BattleGUI.HideGunSight();
        //        Target.SetMarkedStatus(false);
        //    }

        //    Target = null;
        //    Dispatcher.Send(EventId.TargetAimed, new EventInfo_IIB(owner.data.playerId, targetId, false));
        //}

        //Target = target;
        //if (target != null)
        //{
        //    TargetPosition = hit.point;
        //    Dispatcher.Send(EventId.TargetAimed, new EventInfo_IIB(owner.data.playerId, Target.data.playerId, true));
        //    if (owner.IsMain)
        //    {
        //        Target.SetMarkedStatus(true);
        //    }
        //}
    }

//    public void SetMask(int hitMask)
//    {
//        this.hitMask = hitMask;
//    }

//    private bool BoundsInSightYZ(VehicleController veh, Bounds bounds, out Vector3 centerWorldPos)
//    {

//        Vector3 aimInShotCoord = (owner.weapon != null)
//            ? owner.weapon.InverseTransformPoint(bounds.center)
//            : shotPoint.InverseTransformPoint(bounds.center);
//        aimInShotCoord.x = 0f;
//        centerWorldPos = shotPoint.TransformPoint(aimInShotCoord);
//        if (owner.weapon != null)
//        {
//#if UNITY_EDITOR
//            Debug.DrawRay(BattleCamera.Instance.Cam.transform.position, BattleCamera.Instance.Cam.transform.forward,
//                Color.cyan);
//#endif
//            //  Ray ray = new Ray(BattleCamera.Instance.Cam.transform.position, BattleCamera.Instance.Cam.transform.forward);[
//            //    return bounds.IntersectRay(ray);
//            var _info = new RaycastHit();
//            if (Physics.Raycast(BattleCamera.Instance.Cam.transform.position,
//                BattleCamera.Instance.Cam.transform.forward, out _info, 500f, hitMask))
//            {
//                Debug.DrawLine(_info.collider.bounds.min, _info.collider.bounds.max);
//                Debug.Log(_info.collider.name);
//                Debug.Log(_info.collider.tag);
//                Debug.Log(_info.collider.gameObject.layer);
//                if (_info.collider.CompareTag("Enemy"))
//                {
//                    Debug.Log("ВРАГ НА ПРИЦЕЛЕ");
//                    return true;
//                }
//            }
//        }
//        else
//        {
//            return false;
//        }
//        return false;
//    }

//    private bool CheckVehZoneHit(VehicleController vehicle, Bounds bounds, Vector3 centerPos, out RaycastHit hit)
//    {
//        if (!BaseCheckZoneHit(vehicle, bounds, centerPos, out hit, vehicle.CritZonePlace))
//        {
//            return false;
//        }
//        return hit.collider.tag == "CritZone";
//    }

//    private bool CheckVehZoneHit(VehicleController vehicle, Bounds bounds, Vector3 centerPos, out RaycastHit hit, BoundsVertZone zone)// = BoundsVertZone.None
//    {

//        if (zone == owner.CritZonePlace)
//        {
//            hit = new RaycastHit();
//            return false;
//        }

//        if (!BaseCheckZoneHit(vehicle, bounds, centerPos, out hit, zone))
//        {
//            return false;
//        }
//        return true;
//    }

//    private bool BaseCheckZoneHit(VehicleController vehicle, Bounds bounds, Vector3 centerPos, out RaycastHit hit, BoundsVertZone zone)
//    {
//        Color gizmoColor = Color.black;
//        switch (zone)
//        {
//            case BoundsVertZone.Bottom:
//                centerPos.y = bounds.min.y + 0.05f;
//                gizmoColor = Color.green;
//                break;
//            case BoundsVertZone.Center:
//                centerPos.y = bounds.center.y;
//                gizmoColor = Color.yellow;
//                break;
//            case BoundsVertZone.Top:
//                centerPos.y = bounds.max.y - 0.05f;
//                gizmoColor = Color.red;
//                break;
//        }

//        Vector3 direction = (centerPos - shotPoint.position).normalized;
//        if (Vector3.Dot(shotPoint.forward, direction) < owner.turretController.MaxShootAngleCos)
//        {
//            hit = new RaycastHit();
//            return false;
//        }

//        if (!Physics.Raycast(shotPoint.position, direction, out hit, owner.MaxAimDistance, hitMask, QueryTriggerInteraction.Ignore) || hit.rigidbody == null ||
//            hit.rigidbody != vehicle.Rb)
//        {
//            return false;
//        }
//        return true;
//    }

//    private bool CheckVehZoneHit(VehicleController vehicle, Bounds bounds, Vector3 centerPos, out RaycastHit hit, bool weCheckCrit, BoundsVertZone zone = BoundsVertZone.None)
//    {
//        hit = new RaycastHit();
//        return true;
//        // Всё это внизу не работает после правки солдата. Если у нас будут разные зоны попадания - будем смотреть. 
//        if (weCheckCrit)
//        {
//            zone = vehicle.CritZonePlace;
//        }
//        else if (zone == owner.CritZonePlace)
//        {
//            hit = new RaycastHit();
//            return false;
//        }

//        Color gizmoColor = Color.black;
//        switch (zone)
//        {
//            case BoundsVertZone.Bottom:
//                centerPos.y = bounds.min.y + 0.05f;
//                gizmoColor = Color.green;
//                break;
//            case BoundsVertZone.Center:
//                centerPos.y = bounds.center.y;
//                gizmoColor = Color.yellow;
//                break;
//            case BoundsVertZone.Top:
//                centerPos.y = bounds.max.y - 0.05f;
//                gizmoColor = Color.red;
//                break;
//        }

//        Vector3 direction = (centerPos - shotPoint.position).normalized;
//        if (Vector3.Dot(shotPoint.forward, direction) < owner.turretController.MaxShootAngleCos)
//        {
//            hit = new RaycastHit();
//            return false;
//        }

//        if (!Physics.Raycast(shotPoint.position, direction, out hit, owner.MaxAimDistance, hitMask, QueryTriggerInteraction.Ignore) || hit.rigidbody == null ||
//            hit.rigidbody != vehicle.Rb)
//        {
//            return false;
//        }

//        return !weCheckCrit || hit.collider.tag == "CritZone";
//    }
}
