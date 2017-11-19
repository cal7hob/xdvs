using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class RobotBotController : RobotController
{
    protected TankBotAI tankBotAI;

    public override BotAI BotAI
    {
        get { return tankBotAI; }
    }

    public override float XAxisControl { get { return tankBotAI.XAxisControl; } }
    public override float YAxisControl { get { return tankBotAI.YAxisControl; } }
    private float turretAxisControl;

    protected override float TurretAxisControl
    {
        get { return turretAxisControl; }
    }
    protected override bool FireButtonPressed { get { return tankBotAI.FireButtonPressed; } }
    protected override float ZoomRotationSpeed { get { return RotationSpeed; } }

    protected override void OnDestroy()
    {
        tankBotAI.OnBotDestroy();
        base.OnDestroy();
    }

    public override float TurretRotationSpeedQualifier
    {
        get
        {
            return turretRotationSpeedQualifier;
        }
    }

    protected override void Update()
    {
        base.Update();
        if (!PhotonView || !IsAvailable)
            return;

        if (PhotonView.isMine)
        {
            CheckLifeTime();
        }
    }

#if UNITY_EDITOR

    protected override void OnSceneView(SceneView sceneView)
    {
        Transform cameraTransform = sceneView.camera.transform;
        Vector3 objDir = (transform.position - cameraTransform.position).normalized;
        if (Vector3.Dot(cameraTransform.forward, objDir) < 0.5f) // Не входит в FOV 60 градусов
            return;

        Handles.color = Color.red;
        if (BotDispatcher.ShowInnerStates)
            Handles.Label(indicatorPoint.position, BotAI.ToString());
    }

    void OnDrawGizmos()
    {
        if (!BotDispatcher.DrawBotPaths || BotDispatcher.PathsForSelected || tankBotAI == null || tankBotAI.PathCorners.Count == 0 || tankBotAI.CurrentWaypointInd > tankBotAI.PathCorners.Count - 1)
        {
            return;
        }

        DrawGizmos();
    }

    void OnDrawGizmosSelected()
    {
        if (!BotDispatcher.DrawBotPaths || !BotDispatcher.PathsForSelected || tankBotAI == null || tankBotAI.PathCorners.Count == 0 || tankBotAI.CurrentWaypointInd > tankBotAI.PathCorners.Count - 1)
        {
            return;
        }

        DrawGizmos();
    }

    private void DrawGizmos()
    {
        for (int i = 0; i < tankBotAI.PathCorners.Count - 1; i++)
        {
            Debug.DrawLine(tankBotAI.PathCorners[i], tankBotAI.PathCorners[i + 1], Color.red);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(tankBotAI.PathCorners[Mathf.Clamp(tankBotAI.CurrentWaypointInd, 0, 1000)], 1);

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(tankBotAI.PositionToMove, 2);
    }
#endif

    public override void TurretRotation()
    {
        if (Stunned)
            return;

        if (tankBotAI.Target == null)
            return;

        float deltaForRotation = 0;

        var targetDir = (tankBotAI.Target.transform.position - Turret.position).normalized;
        turretAxisControl = Mathf.Clamp(Vector3.Dot(targetDir, Turret.right), -1, 1);

        if (!HelpTools.Approximately(TurretAxisControl, 0))
        {
            deltaForRotation = TurretAxisControl;
            TurretCentering = false;
        }
        else if (TurretCentering)
        {
            if (HelpTools.Approximately(Turret.localEulerAngles.y, 0))
            {
                TurretCentering = false;
                return;
            }

            deltaForRotation = Mathf.Clamp(Mathf.DeltaAngle(Turret.localEulerAngles.y, 0), -1, 1);
        }

        if (HelpTools.Approximately(deltaForRotation, 0))
            return;

        float maxTurretRotationAngle = Speed * TurretRotationSpeedQualifier * Time.deltaTime;
        float realRotation = 0f;
        if (BattleSettings.Instance != null)
        {
            realRotation = Mathf.Clamp(
                   value: HelpTools.ApplySensitivity(deltaForRotation, BattleSettings.Instance.TurretRotationSensitivity) * maxTurretRotationAngle,
                   min: -maxTurretRotationAngle,
                   max: maxTurretRotationAngle);
        }
        else
        {
            realRotation = Mathf.Clamp(
                    value: deltaForRotation * maxTurretRotationAngle,
                    min: -maxTurretRotationAngle,
                    max: maxTurretRotationAngle);
        }

        if (TurretCentering && Mathf.Abs(realRotation) > Mathf.Abs(Mathf.DeltaAngle(Turret.localEulerAngles.y, 0)))
            Turret.localEulerAngles = Vector3.zero;
        else
        {
            Turret.Rotate(0, realRotation, 0, Space.Self);
        }
    }

    protected override void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        base.OnPhotonInstantiate(info);

        if(!PhotonNetwork.isMasterClient)
        {
            tankBotAI = new DummyTankBotAI(this, (BotDispatcher.BotBehaviours)PhotonView.instantiationData[1]);
        }
    }

    public override void UpdateBotPrefabs(VehicleController nativeController)
    {
        RobotController robotController = nativeController as RobotController;

        if (robotController == null)
        {
            return;
        }

        id = robotController.id;
        tankGroup = robotController.tankGroup;
        data = robotController.data;
        forCam = robotController.forCam;
        lookPoint = robotController.lookPoint;
        cameraEndPoint = robotController.cameraEndPoint;
        explosionFxInfo = robotController.explosionFxInfo;
        shotFXInfo = robotController.shotFXInfo;
        shootEffectPoints = robotController.shootEffectPoints;
        engineSound = robotController.engineSound;
        turretRotationSound = robotController.turretRotationSound;
        shotSound = robotController.shotSound;
        explosionSound = robotController.explosionSound;
        respawnSound = robotController.respawnSound;
        maxSpeed = robotController.maxSpeed;
        centerOfMass = robotController.centerOfMass;
        continuousFire = robotController.continuousFire;
        turretRotationSpeedQualifier = robotController.turretRotationSpeedQualifier;
        rotationSpeedQualifier = robotController.rotationSpeedQualifier;
        walkSpeedRatio = robotController.walkSpeedRatio;
        rotationSpeedRatio = robotController.rotationSpeedRatio;
        animateWalking = robotController.animateWalking;
        shellId = robotController.shellId;
        stepClips = robotController.stepClips;
        crashModelResource = robotController.crashModelResource;
    }

    public override void ReanimateBot()
    {
        tankBotAI = new TankBotAI(this, (BotDispatcher.BotBehaviours)PhotonView.instantiationData[1]);
        base.ReanimateBot();
    }

    private void CheckLifeTime()
    {
        if (PhotonNetwork.time >= KickBotAt)
        {
            if (PhotonNetwork.isMasterClient)
                BotDispatcher.Instance.RemoveBot(this);
        }
    }
}