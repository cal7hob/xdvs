using System.Collections;
using UnityEngine;
using XD;

public class TankBotControllerAR : TankControllerAR
{
    protected TankBotAI     tankBotAI = null;
    private Clamper         randomTimer = new Clamper(4, 5, 8);
    private Vector3         randomPosition = new Vector3();
    private float           randomTime = 0;

    public override BotAI BotAI
    {
        get
        {
            return tankBotAI;
        }
    }

    public override float YAxisAcceleration
    {
        get
        {
            return yAxisAcceleration
                = Accelerate(
                    oldSpeed: yAxisAcceleration,
                    newSpeed: tankBotAI.YAxisControl,
                    step: yAxisAccelerationStep,
                    inertionRatio: yAxisInertion,
                    xAxis: false);
        }
    }   

    public override float XAxisAcceleration
    {
        get
        {
            return xAxisAcceleration
                = Accelerate(
                    oldSpeed: xAxisAcceleration,
                    newSpeed: tankBotAI.XAxisControl,
                    step: xAxisAccelerationStep,
                    inertionRatio: xAxisInertion,
                    xAxis: true);
        }
    }  

    protected override bool FireButtonPressed
    {
        get
        {
            return tankBotAI.FireButtonPressed;
        }
    }

    protected override AudioClip CollisionSound
    {
        get
        {
            return collisionSound;
        }
    }

    [PunRPC]
    public override void Respawn(Vector3 position, Quaternion rotation, bool restoreLife, bool firstTime)
    {
        base.Respawn(position, rotation, restoreLife, firstTime);

        yAxisAcceleration = 0;
        xAxisAcceleration = 0;
    }

    public override bool PrimaryFire(Quaternion rotation)
    {
        if (StaticType.GameController.Instance<IGameController>().BattleEnded)
        {
            return false;
        }

        MarkActivity();

        if (!mainWeapon.IsReady)
        {
            return false;
        }

        //if (!weapons[DefaultShellType].IsReady)
        //    return false;

        //if (PhotonView.isMine)
        //    BattleGUI.FireButtons[DefaultShellType].SimulateReloading();

        //weapons[DefaultShellType].RegisterShot();
        mainWeapon.RegisterShot();
        Vector3 euler = shotPoint.eulerAngles + Random.insideUnitSphere * Random.Range(0, 1.75f);


        Shell shell
            = ShellPoolManager.GetShell(
                shellName: primaryShellInfo.shellPrefabName,
                position: shotPoint.position,
                rotation: Quaternion.Euler(euler));

        shell.OwnerSpeed = Mathf.Abs(currentAcceleration);
        continuousFire = false;
        shell.Activate(this, data.attack, hitMask);
        return true;
    }

    protected override void NormalUpdate()
    {
        if (tankBotAI == null)
        {
            Debug.LogErrorFormat(this, "tankBotAI == NULL, {0}, {1}", name, id);
            return;
        }

        base.NormalUpdate();
        tankBotAI.CheckIfFireNeed();        
    }

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        for (int i = 0; i < tankBotAI.path.corners.Length - 1; i++)
        {
            Debug.DrawLine(tankBotAI.path.corners[i], tankBotAI.path.corners[i + 1], Color.red);
        }
    }

    protected override void OnCollisionEnter(Collision collision)
    {
        base.OnCollisionEnter(collision);
        suspensionController.CheckGroundContacts(collision);
    }

    public override void UpdateBotAssets(VehicleController nativeController)
    {
        var tankControllerAR = nativeController as TankControllerAR;

        if (tankControllerAR == null)
        {
            return;
        }

        id = tankControllerAR.id;
        tankGroup = tankControllerAR.tankGroup;
        data = tankControllerAR.data;
        forCam = tankControllerAR.forCam;
        lookPoint = tankControllerAR.lookPoint;
        turretRotationSound = tankControllerAR.turretRotationSound;
        continuousFire = tankControllerAR.continuousFire;
        shotCorrection = tankControllerAR.shotCorrection;
        turretRotationSpeedQualifier = tankControllerAR.turretRotationSpeedQualifier;
        rotationSpeedQualifier = tankControllerAR.rotationSpeedQualifier;

        COM = tankControllerAR.COM;
        Gravity = tankControllerAR.Gravity;
        GetComponent<SuspensionController>().VehicleContoller = this;

        verticalAngles = tankControllerAR.VerticalAngles;
        lowQualityCollider = tankControllerAR.LowQualityCollider;
        collisionSound = tankControllerAR.collisionSound;
        //reloadingSound = tankControllerAR.reloadingSound;
        xAxisAccelerationStep = tankControllerAR.xAxisAccelerationStep;
        yAxisAccelerationStep = tankControllerAR.yAxisAccelerationStep;
        xAxisInertion = tankControllerAR.xAxisInertion;
        yAxisInertion = tankControllerAR.yAxisInertion;

        var soundControllerAR = GetComponent<SoundControllerTankAR>();
        DestroyImmediate(soundControllerAR, true);

        PhotonView pv = GetComponent<PhotonView>();
        pv.ObservedComponents.Add(this);
        //Settings = StaticContainer.MainData.GetUnitHangar(id).Settings.Clone();
    }

    //public override void UpdateBotAssets(VehicleController nativeController)
    //{
    //    base.UpdateBotAssets(nativeController);

    //    var tankControllerAR = nativeController as TankControllerAR;

    //    if (tankControllerAR == null)
    //    {
    //        return;
    //    }

    //    idleSound = tankControllerAR.idleSound;
    //    trackSound = tankControllerAR.trackSound;
    //    collisionSound = tankControllerAR.collisionSound;
    //    rotationSound = tankControllerAR.rotationSound;
    //    accelerationSound = tankControllerAR.accelerationSound;
    //    reloadingSound = tankControllerAR.reloadingSound;
    //    reverseSound = tankControllerAR.reverseSound;
    //    xAxisAccelerationStep = tankControllerAR.xAxisAccelerationStep;
    //    yAxisAccelerationStep = tankControllerAR.yAxisAccelerationStep;
    //    xAxisInertion = tankControllerAR.xAxisInertion;
    //    yAxisInertion = tankControllerAR.yAxisInertion;

    //    var soundControllerAR = GetComponent<SoundControllerTankAR>();
    //    DestroyImmediate(soundControllerAR, true);
    //}

    protected override void OnTargetAimed(EventId id, EventInfo ei)
    {
        tankBotAI.OnBotAimed(id, ei);
    }

    public override void ReanimateBot()
    {
        tankBotAI = new TankBotAI(this);
        tankBotAI.Init((BotDispatcher.BotBehaviours)PhotonView.instantiationData[1]);
        base.ReanimateBot();
    }

    protected override void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        base.OnPhotonInstantiate(info);

        if (transmissionController != null)
        {
            transmissionController.InitComponents(false);
        }

        if (suspensionController != null)
        {
            suspensionController.InitComponents(false);
        }

        if (!PhotonNetwork.isMasterClient)
        {
            tankBotAI = new DummyTankBotAI(this);
            tankBotAI.Init((BotDispatcher.BotBehaviours)PhotonView.instantiationData[1]);
            return;
        }

        tankBotAI.OnBotPhotonInstantiate();
    }
    
    protected override void OnVehicleTakesDamage(EventId id, EventInfo ei)
    {
        tankBotAI.OnBotTakesDamage(id, ei);
    }

    public override Vector3 TargetPosition
    {
        get
        {
            return (tankBotAI.CurrentBehaviour.Target == null ? randomPosition : tankBotAI.CurrentBehaviour.Target.transform.position) + Vector3.up;
        }
    }
  
    private void DrawSkidmarks()
    {
        if (Mathf.Abs(currentAcceleration) < MOVEMENT_SPEED_THRESHOLD && Mathf.Abs(curMaxRotationSpeed) < MOVEMENT_SPEED_THRESHOLD)
        {
            return;
        }

        for (int i = 0; i < SkidmarksPoints.Length; i++)
        {
            if (onGround)
            {
                SkidmarksPoints[i].Draw();
            }
            else
            {
                SkidmarksPoints[i].Chop();
            }
        }
    }
}