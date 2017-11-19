using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngineInternal;

public enum BoundsVertZone
{
    None,
    Bottom,
    Center,
    Top
}

public class AimingController
{
    protected struct CheckedVehicle
    {
        public VehicleController vehicle;
        public Bounds bounds;
        public float sqrDistance;
        public Vector3 centerWorldPos;

        public CheckedVehicle(VehicleController veh, Bounds bnds, float dist, Vector3 cntWorldPos)
        {
            vehicle = veh;
            bounds = bnds;
            sqrDistance = dist;
            centerWorldPos = cntWorldPos;
        }
    }

    protected class CheckedVehComparer : IComparer<CheckedVehicle>
    {
        public int Compare(CheckedVehicle veh1, CheckedVehicle veh2)
        {
            return veh1.sqrDistance < veh2.sqrDistance ? -1 : 1;
        }
    }

    protected VehicleController owner;
    protected Transform shotPoint;
    protected float maxShootAngleCos;
    protected int hitMask;
    protected List<CheckedVehicle> checkedVehicles = new List<CheckedVehicle>(10);
    protected CheckedVehComparer checkedVehsComparer = new CheckedVehComparer();

    public VehicleController Target;
    public Vector3 TargetPosition;
    public bool CritZoneAimed { get; protected set; }
    public float turretRotationAutoaim = 0f;


    private static int ms_cnt = 0;
    private static List<int> ms_ids = new List<int>();
    private int m_id = 0;

    public AimingController(VehicleController owner)
    {
        this.owner = owner;
        shotPoint = owner.ShotPoint;
        maxShootAngleCos = owner.MaxShootAngleCos;
        hitMask = owner.HitMask;
        Target = null;
        CritZoneAimed = false;
        TargetPosition = Vector3.zero;
        ms_cnt++;
        m_id = owner.data.playerId;
        ms_ids.Add(m_id);
        UberDebug.LogChannel("AimingController", "+++ AimingController for {0}, all objects = {1}", m_id, ms_cnt);
    }

    ~AimingController()
    {
        ms_cnt--;
        ms_ids.Remove(m_id);
        string ids = string.Join(",", ms_ids.Select(id => id.ToString()).ToArray());
        UberDebug.LogChannel("AimingController", "--- AimingController for {0}, all objects = {1}, left = {2}", m_id, ms_cnt, ids);
    }

    virtual public void Aiming()
    {
        #region Отбор вехов, которые входят в обзор прицела и сортировка их в порядке отдаления
        CollectAimedVehicles ();
        #endregion

        VehicleController target = null;
        RaycastHit hit = new RaycastHit();

        for (int i = 0; i < checkedVehicles.Count; i++)
        {
            CheckedVehicle checkedVeh = checkedVehicles[i];

            if (CheckVehZoneHit (checkedVeh.vehicle, checkedVeh.bounds, checkedVeh.centerWorldPos, out hit, true)) {
                target = checkedVeh.vehicle;
                CritZoneAimed = true;
                break;
            }
            if (
                CheckVehZoneHit (checkedVeh.vehicle, checkedVeh.bounds, checkedVeh.centerWorldPos, out hit, false, BoundsVertZone.Center)
                || CheckVehZoneHit (checkedVeh.vehicle, checkedVeh.bounds, checkedVeh.centerWorldPos, out hit, false, BoundsVertZone.Bottom)
                || CheckVehZoneHit (checkedVeh.vehicle, checkedVeh.bounds, checkedVeh.centerWorldPos, out hit, false, BoundsVertZone.Top)
            ) {
                target = checkedVeh.vehicle;
                CritZoneAimed = false;
                break;
            }
        }

        if (target != null)
        {
            TargetPosition = hit.point;
            if (owner.IsMain)
                BattleGUI.ShowGunSightForWorld(hit.point, hit.distance);
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
                BattleGUI.HideGunSight();
                Target.SetMarkedStatus(false);
            }

            Target = null;
            Dispatcher.Send(EventId.TargetAimed, new EventInfo_IIB(owner.data.playerId, targetId, false));
         }

        Target = target;
        if (target != null)
        {
            TargetPosition = hit.point;
            Dispatcher.Send(EventId.TargetAimed, new EventInfo_IIB(owner.data.playerId, Target.data.playerId, true));
            if (owner.IsMain)
            {
                Target.SetMarkedStatus(true);
            }
        }
    }

    virtual protected void CollectAimedVehicles () {
        checkedVehicles.Clear ();
        foreach (VehicleController vehicle in BattleController.allVehicles.Values) {
            if (vehicle == owner || VehicleController.AreFriends (owner, vehicle))
                continue;

            if (Vector3.Dot (owner.Turret.transform.forward, vehicle.transform.position - owner.Turret.transform.position) < 0)
                continue;

            Vector3 centerWorldPos;
            Bounds aimBounds = vehicle.GetEntireAimBounds ();
            if (BoundsInSightYZ (vehicle.GetEntireAimBounds (), out centerWorldPos)) {
                checkedVehicles.Add (new CheckedVehicle (vehicle, aimBounds, Vector3.SqrMagnitude (vehicle.transform.position - owner.transform.position), centerWorldPos));
            }
        }
        checkedVehicles.Sort (checkedVehsComparer);
    }

    protected bool BoundsInSightYZ(Bounds bounds, out Vector3 centerWorldPos)
    {
        Vector3 aimInShotCoord = shotPoint.InverseTransformPoint(bounds.center);
        aimInShotCoord.x = 0f;
        centerWorldPos = shotPoint.TransformPoint(aimInShotCoord);
        Ray ray = new Ray(shotPoint.position, (centerWorldPos - shotPoint.position).normalized);

        return bounds.IntersectRay(ray);
    }

    protected bool CheckVehZoneHit(VehicleController vehicle, Bounds bounds, Vector3 centerPos, out RaycastHit hit, bool weCheckCrit, BoundsVertZone zone = BoundsVertZone.None)
    {
        if (weCheckCrit)
        {
            zone = vehicle.CritZonePlace;
        }
        else if (zone == vehicle.CritZonePlace) {
            hit = new RaycastHit ();
            return false;
        }

        switch (zone)
        {
            case BoundsVertZone.Bottom:
                centerPos.y = bounds.min.y + 0.1f;
                break;
            case BoundsVertZone.Center:
                centerPos.y = bounds.center.y;
                break;
            case BoundsVertZone.Top:
                centerPos.y = bounds.max.y - 0.1f;
                break;
        }

        Vector3 direction = (centerPos - shotPoint.position).normalized;
        if (Vector3.Dot(shotPoint.forward, direction) < maxShootAngleCos)
        {
            hit = new RaycastHit();
            return false;
        }

        if (!Physics.Raycast(shotPoint.position, direction, out hit, owner.MaxAimDistance, hitMask, QueryTriggerInteraction.Ignore) || hit.rigidbody == null ||
            hit.rigidbody != vehicle.Rb)
            return false;

        return !weCheckCrit || hit.collider.tag == "CritZone";
    }

    public virtual void ResetAutoaim() { }

#if UNITY_EDITOR
    virtual protected bool CheckVehZoneHitGizmo (VehicleController vehicle, Bounds bounds, Vector3 centerPos, out RaycastHit hit, bool weCheckCrit, BoundsVertZone zone = BoundsVertZone.None) {
        if (weCheckCrit) {
            zone = vehicle.CritZonePlace;
        }
        else if (zone == vehicle.CritZonePlace) {
            hit = new RaycastHit ();
            return false;
        }

        Color gizmoColor = Color.black;
        switch (zone) {
            case BoundsVertZone.Bottom:
                centerPos.y = bounds.min.y + 0.1f;
                gizmoColor = Color.green;
                break;
            case BoundsVertZone.Center:
                centerPos.y = bounds.center.y;
                gizmoColor = Color.yellow;
                break;
            case BoundsVertZone.Top:
                centerPos.y = bounds.max.y - 0.1f;
                gizmoColor = Color.red;
                break;
        }

        Gizmos.color = gizmoColor;

        Debug.DrawRay (shotPoint.position, centerPos - shotPoint.position, Color.blue);
        Vector3 direction = (centerPos - shotPoint.position).normalized;
        if (Vector3.Dot (shotPoint.forward, direction) < maxShootAngleCos) {
            hit = new RaycastHit ();
            return false;
        }

        Debug.DrawRay (shotPoint.position, centerPos - shotPoint.position, Color.green);
        if (!Physics.Raycast (shotPoint.position, direction, out hit, owner.MaxAimDistance, hitMask, QueryTriggerInteraction.Ignore) || hit.rigidbody == null ||
            hit.rigidbody != vehicle.Rb) 
        {
            Gizmos.DrawLine (shotPoint.position, hit.point);
            Gizmos.DrawSphere (hit.point, .7f);
            return false;
        }
        Gizmos.DrawLine (shotPoint.position, hit.point);
        Gizmos.DrawSphere (hit.point, .7f);

        Vector3 reflectVec = Vector3.Reflect(hit.point - shotPoint.position, hit.normal);
        Debug.DrawRay (hit.point, reflectVec, Color.green);

        return !weCheckCrit || hit.collider.tag == "CritZone";
    }

    virtual public void DrawGizmos () {
        checkedVehicles.Clear ();
        foreach (VehicleController vehicle in BattleController.allVehicles.Values) {
            if (vehicle == owner || VehicleController.AreFriends (owner, vehicle))
                continue;

            if (Vector3.Dot (owner.Turret.transform.forward, vehicle.transform.position - owner.Turret.transform.position) < 0)
                continue;

            Vector3 centerWorldPos;
            Bounds aimBounds = vehicle.GetEntireAimBounds();

            Gizmos.color = Color.blue;
            if (BoundsInSightYZ (aimBounds, out centerWorldPos)) {
                Gizmos.color = Color.red;
                checkedVehicles.Add (new CheckedVehicle (vehicle, aimBounds, Vector3.SqrMagnitude (vehicle.transform.position - owner.transform.position), centerWorldPos));
            }
            Gizmos.DrawWireCube (aimBounds.center, aimBounds.size);

        }

        RaycastHit hit = new RaycastHit();

        for (int i = 0; i < checkedVehicles.Count; i++) {
            CheckedVehicle checkedVeh = checkedVehicles[i];


            Gizmos.color = Color.black;
            Gizmos.DrawLine (owner.ShotPoint.position, checkedVeh.centerWorldPos);

            if (CheckVehZoneHitGizmo (checkedVeh.vehicle, checkedVeh.bounds, checkedVeh.centerWorldPos, out hit, true)) {
                break;
            }
            if (
                CheckVehZoneHitGizmo (checkedVeh.vehicle, checkedVeh.bounds, checkedVeh.centerWorldPos, out hit, false, BoundsVertZone.Center)
                || CheckVehZoneHitGizmo (checkedVeh.vehicle, checkedVeh.bounds, checkedVeh.centerWorldPos, out hit, false, BoundsVertZone.Bottom)
                || CheckVehZoneHitGizmo (checkedVeh.vehicle, checkedVeh.bounds, checkedVeh.centerWorldPos, out hit, false, BoundsVertZone.Top)
            ) {
                break;
            }
        }

    }
#endif
}
