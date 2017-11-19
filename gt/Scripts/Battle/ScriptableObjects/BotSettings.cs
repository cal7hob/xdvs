using UnityEngine;

[System.Serializable]
public struct ParamsRange
{
    [SerializeField] private float min;
    [SerializeField] private float max;
    
    public float Min { get { return min; } } 
    public float Max { get { return max; } }

    public ParamsRange(float min, float max)
    {
        this.min = min;
        this.max = max;
    }

    public float RandWithinRange { get { return (float) MiscTools.NextRandomDouble(min, max); } }
}

public class BotSettings : ScriptableObject
{
    [SerializeField] private float clearWaypointSqrDistance = 1;
    [SerializeField] private float      stuckAngle;
    [SerializeField] private ParamsRange timesToStuck;
    [SerializeField] private ParamsRange timesToMoveBack; //stuckState
    [SerializeField] private ParamsRange moveBackSpeeds;
    [SerializeField] private ParamsRange pathUpdateDelays;
    [SerializeField] private ParamsRange revengeDelays;
    [SerializeField] private ParamsRange revengeTimes;
    [SerializeField] private ParamsRange gettingClosestVehicleDelay;
    [SerializeField] private ParamsRange fireDelays;
    [SerializeField] private ParamsRange findingPosDelays;
    [SerializeField] private ParamsRange revengeFindingPosDelays = new ParamsRange(4, 8);
    [SerializeField] private ParamsRange findingTargetDelays;
    [SerializeField] private ParamsRange broForgivenessTimeout;
    [SerializeField] private ParamsRange bonusSeekingRadius;
    [SerializeField] private ParamsRange gettingClosestBonusDelays = new ParamsRange(3, 8);
    [SerializeField] private ParamsRange changingBehaviourDelays = new ParamsRange(40, 60);
    [SerializeField] private ParamsRange findingOneShotEnemyDelays = new ParamsRange(5, 10);
    [SerializeField] private ParamsRange targetPreferenceDelays;
    [SerializeField] private float stuckVelocitySqrMagnitude = 2;   
    [SerializeField] private float targetBotRevengeChance = 85;
    [SerializeField] private float oneShotTargetCheckDistance = 20;
    private float                  oneShotTargetCheckSqrDistance;  

    public float ClearWaypointSqrDistance { get { return clearWaypointSqrDistance; } }
    public float StuckAngle { get { return stuckAngle; } }
    public ParamsRange TimesToStuck { get { return timesToStuck; } }
    public ParamsRange TimesToMoveBack { get { return timesToMoveBack; } }
    public ParamsRange MoveBackSpeeds { get { return moveBackSpeeds; } }
    public ParamsRange PathUpdateDelays { get { return pathUpdateDelays; } }
    public ParamsRange RevengeDelays { get { return revengeDelays; } }
    public ParamsRange RevengeTimes { get { return revengeTimes; } }
    public ParamsRange GettingClosestVehicleDelay { get { return gettingClosestVehicleDelay; } }
    public ParamsRange FindingPosDelays { get { return findingPosDelays; } }
    public ParamsRange RevengeFindingPosDelays { get { return revengeFindingPosDelays; } }
    public ParamsRange FindingTargetDelays { get { return findingTargetDelays; } }
    public ParamsRange BroForgivenessTimeout { get { return broForgivenessTimeout; } }
    public ParamsRange BonusSeekingRadius { get { return bonusSeekingRadius; } }
    public ParamsRange GettingClosestBonusDelays { get { return gettingClosestBonusDelays; } }
    public ParamsRange ChangingBehaviourDelays { get { return changingBehaviourDelays; } }
    public ParamsRange FindingOneShotEnemyDelays { get { return findingOneShotEnemyDelays; } }
    public ParamsRange FireDelay { get { return fireDelays; } }
    public ParamsRange TargetPreferenceDelays { get { return targetPreferenceDelays; } }

    public float OneShotTargetCheckSqrDistance { get { return oneShotTargetCheckSqrDistance; } }
    public float StuckVelocitySqrMagintude { get { return stuckVelocitySqrMagnitude; } }   
    public float TargetBotRevengeChance { get { return targetBotRevengeChance; } }

    void Awake()
    {
        oneShotTargetCheckSqrDistance = Mathf.Pow(oneShotTargetCheckDistance, 2);
    }
}
