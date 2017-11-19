using UnityEngine;

public class TurretBotARController : TurretTankARController
{
    protected BotAI botAI;

    public TurretBotARController(VehicleController vehicle, Animation shootAnimation)
        : base(vehicle, shootAnimation)
    {
        botAI = vehicle.BotAI;
    }

    public override float TurretRotationSpeedQualifier
    {
        get
        {
            return vehicle.turretRotationSpeedQualifier;
        }
    }

    protected override Quaternion Rotation
    {
        get
        {
            return TargetAimed ?
            Quaternion.LookRotation((vehicle.ViewPoint - vehicle.ShotPoint.position).normalized, vehicle.ShotPoint.up) :
            vehicle.ShotPoint.rotation;
        }
    }

    public override void TurretRotation()
    {
        if (botAI.Target == null || botAI.CurrentBehaviour.StopRotateTurret)
        {
            return;
        }

        float deltaForRotation = lastTouchTurretRotation;
        lastTurretLocalRotationY = turret.localEulerAngles.y;

        var targetDir = (botAI.Target.transform.position - turret.position).normalized;
        botAI.TurretAxisControl = Mathf.Clamp(Vector3.Dot(targetDir, turret.right), -1, 1);

        if (!HelpTools.Approximately(TurretAxisControl, 0))
        {
            deltaForRotation = TurretAxisControl;
            TurretCentering = false;
        }
        else if (TurretCentering)
        {
            if (HelpTools.Approximately(turret.localEulerAngles.y, 0))
            {
                TurretCentering = false;
                return;
            }

            deltaForRotation = Mathf.Clamp(Mathf.DeltaAngle(turret.localEulerAngles.y, 0), -1, 1);
        }

        if (HelpTools.Approximately(deltaForRotation, 0))
        {
            return;
        }

        float maxTurretRotationAngle = vehicle.Speed * TurretRotationSpeedQualifier * Time.deltaTime;
        float realRotation = 
            realRotation = Mathf.Clamp(
            value: (BattleSettings.Instance != null)?
                HelpTools.ApplySensitivity(deltaForRotation, BattleSettings.Instance.TurretRotationSensitivity) * maxTurretRotationAngle:
                deltaForRotation * maxTurretRotationAngle,
            min: -maxTurretRotationAngle,
            max: maxTurretRotationAngle);//0f;


        if (TurretCentering && Mathf.Abs(realRotation) > Mathf.Abs(Mathf.DeltaAngle(turret.localEulerAngles.y, 0)))
        {
            turret.localEulerAngles = Vector3.zero;
        }
        else
        {
           // Debug.Log(vehicle.data.playerId + " angle = " + realRotation);
            turret.Rotate(0, realRotation, 0, Space.Self);
        }
    }
}