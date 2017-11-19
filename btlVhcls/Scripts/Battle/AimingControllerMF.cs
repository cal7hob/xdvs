using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngineInternal;
using Rewired;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class AimingControllerMF : AimingController
{

    enum LockingStates
    {
        Idle, Locking, Locked
    }

    private bool isBot = false;
    private float aimEverySeconds = .15f;
    private float elapsed = 0f;

    private float horizAngle = 3f;
    private float lockingTime = .5f;
    private VehicleController lockedTarget = null;
    private Vector3 lockedTargetPosition = Vector3.zero;
    private LockingStates LockingState {
        get { return lockingState; }
        set {
            if (value == lockingState) return;
            var prev = lockingState;
            lockingState = value;
            //Debug.LogFormat("Locking state chaged to {0}", lockingState);
            if (value == LockingStates.Locked) { // Target Locked
                Debug.LogFormat("Target {0} locked", lockedTarget.data.playerId);
                Dispatcher.Send(EventId.TargetLockChanged, new EventInfo_IB(lockedTarget.data.playerId, true));
            }
            if (prev == LockingStates.Locked) { // Target Lost
                Debug.LogFormat("Target lost");
                Dispatcher.Send(EventId.TargetLockChanged, new EventInfo_IB(0, false));
            }
        }
    }
    private LockingStates lockingState = LockingStates.Idle;
    private float lockingElapsed = 0f;

    public AimingControllerMF (VehicleController owner) : base (owner)
    {
        isBot = owner.IsBot;

        if (owner.IsMain)
        {
            Dispatcher.Subscribe(EventId.TankKilled, OnMyTankRespawned);
        }
    }

    ~AimingControllerMF ()
    {
        Dispatcher.Unsubscribe(EventId.TankKilled, OnMyTankRespawned);
    }

    private void OnMyTankRespawned(EventId id, EventInfo ei)
    {
        var info = ei as EventInfo_III;
        var victimId = info.int1;

        if (victimId != BattleController.MyPlayerId) { // Не наше событие, выходим
            return;
        }

        ResetAutoaim();
        BattleGUI.HideGunSight();
    }

    public override void Aiming()
    {
        if (isBot) {
            elapsed += Time.deltaTime;
            if (elapsed < aimEverySeconds) {
                return;
            }
            elapsed = 0f;
        }

        base.Aiming();

        // Lock target
        turretRotationAutoaim = 0f;
        if (!isBot) {
            if ((SystemInfo.deviceType == DeviceType.Handheld) && ProfileInfo.AutoAimingType == AutoAimingType.DefaultAutoAiming) {
                switch (LockingState)
                {
                    case LockingStates.Idle:
                        if (Target != null) { // Locking start
                            lockingElapsed = lockingTime;
                            lockedTarget = Target;
                            LockingState = LockingStates.Locking;
                        }
                        break;

                    case LockingStates.Locking:
                        if ((lockedTarget != null) && (Target == lockedTarget)) {
                            lockingElapsed -= Time.deltaTime;
                            if (lockingElapsed <= 0f) { // Target succesfully locked
                                lockedTargetPosition = lockedTarget.transform.InverseTransformPoint(TargetPosition);
                                LockingState = LockingStates.Locked;
                            }
                            break;
                        }
                        ResetAutoaim();
                        break;

                    case LockingStates.Locked:
                        if (lockedTarget == null) {
                            ResetAutoaim();
                            break;
                        }
                        Vector3 aimPos = lockedTarget.transform.TransformPoint(lockedTargetPosition) - owner.Turret.transform.position;
                        float angle = HelpTools.AngleSigned(owner.Turret.transform.forward, aimPos, owner.Turret.transform.up);
                        float maxTurretRotationAngle = owner.Speed * owner.TurretRotationSpeedQualifier * Time.deltaTime;
                        turretRotationAutoaim = Mathf.Clamp(angle / maxTurretRotationAngle, -1f, 1f);
                        //Debug.LogFormat("A: {0}, MaxA: {1}, Rot: {2}", angle, maxTurretRotationAngle, turretRotationAutoaim);
                        break;
                }

                if (lockedTarget != null && !lockedTarget.IsAvailable) { // Reset autoaim when target is dead
                    ResetAutoaim();
                }
            }
            else if (lockingState != LockingStates.Idle) {
                ResetAutoaim();
            }
        }
    }

    public override void ResetAutoaim()
    {
        base.ResetAutoaim();
        LockingState = LockingStates.Idle;
        lockedTarget = null;
    }

    protected override void CollectAimedVehicles () {
        checkedVehicles.Clear ();
        foreach (VehicleController vehicle in BattleController.allVehicles.Values) {
            if (vehicle == owner || VehicleController.AreFriends (owner, vehicle) || !vehicle.IsAvailable)
                continue;

            if (Vector3.Dot (owner.Turret.transform.forward, vehicle.transform.position - owner.Turret.transform.position) < 0)
                continue;

            Vector3 centerWorldPos;
            Bounds aimBounds = vehicle.GetEntireAimBounds ();

            bool found = BoundsInSightYZ (aimBounds, out centerWorldPos);

            if (found) {
                Vector3 center = centerWorldPos;
                if (!isBot) {
                    bool found2 = false;
                    foreach (var col in vehicle.BoundColliders) {
                        if (BoundsInSightYZ (col.bounds, out center)) {
                            if (!found2) {
                                aimBounds = col.bounds;
                                found2 = true;
                            }
                            else {
                                aimBounds.Encapsulate (col.bounds);
                            }
                        }
                    }
                }
                checkedVehicles.Add (new CheckedVehicle (vehicle, aimBounds, Vector3.SqrMagnitude (center - owner.transform.position), center));
            }

            if (!isBot && (SystemInfo.deviceType == DeviceType.Handheld)) {
                //Vector3 closestPoint = aimBounds.ClosestPoint (centerWorldPos);
                float angle = HelpTools.AngleSigned (owner.Turret.transform.forward, vehicle.transform.position - owner.Turret.transform.position, owner.Turret.transform.up);
                if (angle < horizAngle && angle > -horizAngle) {
                    centerWorldPos = centerWorldPos.RotatePointAroundPivot (
                        owner.Turret.transform.position,
                        new Vector3 (0f, angle, 0f)
                    );
                    checkedVehicles.Add (new CheckedVehicle (vehicle, aimBounds, Vector3.SqrMagnitude (centerWorldPos - owner.transform.position) + 1f, centerWorldPos));
                }
            }

        }
        checkedVehicles.Sort (checkedVehsComparer);
    }

#if UNITY_EDITOR
    override public void DrawGizmos () {
        if (!owner.IsMain) {
            return;
        }
        //base.DrawGizmos ();

        //checkedVehicles.Clear ();
        foreach (VehicleController vehicle in BattleController.allVehicles.Values) {
            //if (vehicle == owner || VehicleController.AreFriends (owner, vehicle))
            //    continue;

            //if (Vector3.Dot (owner.Turret.transform.forward, vehicle.transform.position - owner.Turret.transform.position) < 0)
            //    continue;

            //Vector3 centerWorldPos;
            //Bounds aimBounds = vehicle.GetEntireAimBounds();

            //bool found = BoundsInSightYZ (aimBounds, out centerWorldPos);

            //if (found) {
            //    Gizmos.color = Color.red;
            //    Gizmos.DrawWireCube (aimBounds.center, aimBounds.size);

            //    Gizmos.color = Color.magenta;
            //    bool found2 = false;
            //    Vector3 center;
            //    foreach (var col in vehicle.BoundColliders) {
            //        if (BoundsInSightYZ (col.bounds, out center)) {
            //            if (!found2) {
            //                aimBounds = col.bounds;
            //                found2 = true;
            //            }
            //            else {
            //                aimBounds.Encapsulate (col.bounds);
            //            }
            //        }
            //    }
            //    Gizmos.DrawWireCube (aimBounds.center, aimBounds.size);

            //    checkedVehicles.Add (new CheckedVehicle (vehicle, aimBounds, Vector3.SqrMagnitude (vehicle.transform.position - owner.transform.position), centerWorldPos));
            //    Handles.Label (centerWorldPos, string.Format ("{0}", Vector3.SqrMagnitude (vehicle.transform.position - owner.transform.position)));
            //}
            //else {
            //    Gizmos.color = Color.blue;
            //    Gizmos.DrawWireCube (aimBounds.center, aimBounds.size);
            //}

            //if (!isBot) {
            //    //Vector3 closestPoint = aimBounds.ClosestPoint (centerWorldPos);
            //    float angle = HelpTools.AngleSigned (owner.Turret.transform.forward, vehicle.transform.position - owner.Turret.transform.position, owner.Turret.transform.up);
            //    if (angle < horizAngle && angle > -horizAngle) {
            //        centerWorldPos = centerWorldPos.RotatePointAroundPivot (
            //            owner.Turret.transform.position,
            //            new Vector3 (0f, angle, 0f)
            //        );
            //        checkedVehicles.Add (new CheckedVehicle (vehicle, aimBounds, Vector3.SqrMagnitude (centerWorldPos - owner.transform.position) + .01f, centerWorldPos));
            //        Handles.Label (centerWorldPos, string.Format ("{0}", Vector3.SqrMagnitude (vehicle.transform.position - owner.transform.position)+.1f));
            //        Gizmos.color = Color.blue;
            //        Gizmos.DrawRay (centerWorldPos, vehicle.transform.up * 20f);
            //        Gizmos.DrawSphere (centerWorldPos, .2f);
            //    }
            //    Handles.Label(vehicle.transform.position, string.Format("{0}", angle));
            //}

            float angle = HelpTools.AngleSigned (owner.Turret.transform.forward, vehicle.transform.position - owner.Turret.transform.position, owner.Turret.transform.up);
            float dist = Vector3.SqrMagnitude(vehicle.transform.position - owner.transform.position);
            Handles.Label(vehicle.transform.position, string.Format("a: {0}\nd: {1}", angle, dist));

        }


        //RaycastHit hit = new RaycastHit();

        //for (int i = 0; i < checkedVehicles.Count; i++) {
        //    CheckedVehicle checkedVeh = checkedVehicles[i];


        //    Gizmos.color = Color.black;
        //    Gizmos.DrawLine (owner.ShotPoint.position, checkedVeh.centerWorldPos);

        //    if (CheckVehZoneHitGizmo (checkedVeh.vehicle, checkedVeh.bounds, checkedVeh.centerWorldPos, out hit, true)) {
        //        break;
        //    }
        //    if (
        //        CheckVehZoneHitGizmo (checkedVeh.vehicle, checkedVeh.bounds, checkedVeh.centerWorldPos, out hit, false, BoundsVertZone.Center)
        //        || CheckVehZoneHitGizmo (checkedVeh.vehicle, checkedVeh.bounds, checkedVeh.centerWorldPos, out hit, false, BoundsVertZone.Bottom)
        //        || CheckVehZoneHitGizmo (checkedVeh.vehicle, checkedVeh.bounds, checkedVeh.centerWorldPos, out hit, false, BoundsVertZone.Top)
        //    ) {
        //        break;
        //    }
        //}
    }

#endif
}
