using System.Collections;
using UnityEngine;
using XD;

public class TankBotController : TankController
{
    protected TankBotAI tankBotAI;

    public override BotAI BotAI
    {
        get 
        { 
            return tankBotAI; 
        }
    }

    public override float XAxisControl 
    { 
        get 
        { 
            return tankBotAI.XAxisControl; 
        } 
    }
    public override float YAxisControl 
    { 
        get 
        { 
            return tankBotAI.YAxisControl; 
        } 
    }
   
    protected override bool FireButtonPressed 
    { 
        get 
        { 
            return tankBotAI.FireButtonPressed; 
        } 
    }

    protected override void OnDestroy()
    {
        tankBotAI.OnBotDestroy();
        base.OnDestroy();
    }

    protected override void OnVehicleTakesDamage(EventId id, EventInfo ei)
    {
        tankBotAI.OnBotTakesDamage(id, ei);
    }

    //protected override void NormalUpdate()
    //{
    //    tankBotAI.MyBotUpdate();
    //    tankBotAI.OthersBotUpdate();
    //}

#if UNITY_EDITOR
    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();


        for (int i = 0; i < tankBotAI.path.corners.Length - 1; i++)
        {
            UnityEditor.Handles.Label(tankBotAI.path.corners[i], string.Format("Way Point {0}", i).FormatString(Color.red), GUIStyle.none);
            Debug.DrawLine(tankBotAI.path.corners[i], tankBotAI.path.corners[i + 1], Color.red);
        }

        if (tankBotAI == null || tankBotAI.path == null || tankBotAI.path.corners.Length <= 0 || tankBotAI.CurrentWaypoint > tankBotAI.path.corners.Length - 1)
        {
            return;
        }

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(tankBotAI.path.corners[tankBotAI.CurrentWaypoint], 1);

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(tankBotAI.CurrentBehaviour.PositionToMove, 2);
    }
#endif

    protected override void OnTargetAimed(EventId id, EventInfo ei)
    {
        tankBotAI.OnBotAimed(id, ei);
    }   

    protected override void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        base.OnPhotonInstantiate(info);

        if(!PhotonNetwork.isMasterClient)
        {
            tankBotAI = new DummyTankBotAI(this);
            tankBotAI.Init((BotDispatcher.BotBehaviours)PhotonView.instantiationData[1]);
            return;
        }

        tankBotAI.OnBotPhotonInstantiate();
    }

    public override void UpdateBotAssets(VehicleController nativeController)
    {
        var tankController = nativeController as TankController;

        if (tankController == null)
        {
            return;
        }

        data = tankController.data;
        turretRotationSound = tankController.turretRotationSound;
        Settings[Setting.MovingSpeed].Max = tankController.Settings[Setting.MovingSpeed].Max;
        CenterOfMass = tankController.CenterOfMass;
        continuousFire = tankController.continuousFire;
        shotCorrection = tankController.shotCorrection;
        turretRotationSpeedQualifier = tankController.turretRotationSpeedQualifier;
        rotationSpeedQualifier = tankController.rotationSpeedQualifier;
    }

    public override void ReanimateBot()
    {
        tankBotAI = new TankBotAI(this);
        tankBotAI.Init((BotDispatcher.BotBehaviours)PhotonView.instantiationData[1]);
        base.ReanimateBot();
    }

    public override void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        tankBotAI.OnBotPhotonSerializeView(stream, info);
    }
}
