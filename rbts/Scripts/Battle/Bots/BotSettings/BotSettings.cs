using UnityEngine;

[System.Serializable]
public struct DelayRange
{
    [SerializeField] private float min;
    [SerializeField] private float max;
    
    public float Min { get { return min; } } 
    public float Max { get { return max; } }

    public float RandWithinRange { get { return MiscTools.random.Next((int)min * 10, (int)max * 10) * 0.1f; } }
}

public class BotSettings : MonoBehaviour
{
    [SerializeField] private float clearWaypointSqrDistance = 1;
    [SerializeField] private DelayRange stopSqrDistanceToOtherVehicles;
    [SerializeField] private float      stuckAngle;
    [SerializeField] private DelayRange timesToStuck;
    [SerializeField] private DelayRange timesToMoveBack; //stuckState
    [SerializeField] private DelayRange moveBackSpeeds;
    [SerializeField] private DelayRange pathUpdateDelays;
    [SerializeField] private DelayRange revengeDelays;
    [SerializeField] private DelayRange revengeTimes;

    [SerializeField] private DelayRange findingPosDelaysTarget;
    [SerializeField] private DelayRange findingPosDelaysFighter;
    [SerializeField] private DelayRange findingPosDelaysAgressor;
    [SerializeField] private DelayRange findingPosDelaysTutorial;

    [SerializeField] private DelayRange findingTargetDelaysTarget;
    [SerializeField] private DelayRange findingTargetDelaysFighter;
    [SerializeField] private DelayRange findingTargetDelaysAgressor;
    [SerializeField] private float stuckVelocitySqrMagnitude = 2;
    [SerializeField] private float stuckMinSqrDistanceToOtherVehicle = 5;
    [SerializeField] private float bonusSeekingRadius = 20;
    [SerializeField] private float targetBotRevengeChance = 85;
    [SerializeField] private float oneShotTargetCheckDistance = 20;
    private float                  oneShotTargetCheckSqrDistance;
    [SerializeField] private float broForgivenessTimeout = 40;
    [SerializeField] private float setTargetPreferenceDelay = 15;
    [SerializeField] private int[] botCamoulageIds;
    [SerializeField] private int[] botDecalIds;


    public static float clearWaypointSqrDistance_s;
    public static DelayRange stopSqrDistancesToOtherVehicles_s;
    public static float      stuckAngle_s;
    public static DelayRange timesToStuck_s;
    public static DelayRange timesToMoveBack_s;
    public static DelayRange moveBackSpeeds_s;
    public static DelayRange pathUpdateDelays_s;
    public static DelayRange revengeDelays_s;
    public static DelayRange revengeTimes_s;

    public static DelayRange findingPosDelaysTarget_s;
    public static DelayRange findingPosDelaysFighter_s;
    public static DelayRange findingPosDelaysAgressor_s;
    public static DelayRange findingPosDelaysTutorial_s;

    public static DelayRange findingTargetDelaysTarget_s;
    public static DelayRange findingTargetDelaysFighter_s;
    public static DelayRange findingTargetDelaysAgressor_s;

    public static float maxPathUpdatePeriod_s;
    public static float stuckSqrMagnitude_s;
    public static float stuckMinSqrDistance_s;
    public static float bonusSeekingRadius_s;
    public static float targetBotRevengeChance_s;
    public static float oneShotTargetCheckDistance_s;
    public static float oneShotTargetCheckSqrDistance_s;
    public static float broForgivenessTimeout_s;
    public static float setTargetPreferenceDelay_s;
    public static int[] botCamoulageIds_s;
    public static int[] botDecalIds_s;

    void Start()
    {
        oneShotTargetCheckSqrDistance = Mathf.Pow(oneShotTargetCheckDistance, 2);

        clearWaypointSqrDistance_s = clearWaypointSqrDistance;
        stuckAngle_s = stuckAngle;
        stopSqrDistancesToOtherVehicles_s = stopSqrDistanceToOtherVehicles;
        timesToStuck_s = timesToStuck;
        timesToMoveBack_s = timesToMoveBack;
        moveBackSpeeds_s = moveBackSpeeds;
        pathUpdateDelays_s = pathUpdateDelays;
        stuckSqrMagnitude_s = stuckVelocitySqrMagnitude;
        stuckMinSqrDistance_s = stuckMinSqrDistanceToOtherVehicle;
        revengeDelays_s = revengeDelays;
        revengeTimes_s = revengeTimes;
        bonusSeekingRadius_s = bonusSeekingRadius;
        targetBotRevengeChance_s = targetBotRevengeChance;
        oneShotTargetCheckDistance_s = oneShotTargetCheckDistance;
        oneShotTargetCheckSqrDistance_s = oneShotTargetCheckSqrDistance;
        broForgivenessTimeout_s = broForgivenessTimeout;
        setTargetPreferenceDelay_s = setTargetPreferenceDelay;

        findingPosDelaysTarget_s = findingPosDelaysTarget;
        findingPosDelaysFighter_s = findingPosDelaysFighter;
        findingPosDelaysAgressor_s = findingPosDelaysAgressor;
        findingPosDelaysTutorial_s = findingPosDelaysTutorial;

        findingTargetDelaysTarget_s = findingTargetDelaysTarget;
        findingTargetDelaysFighter_s = findingTargetDelaysFighter;
        findingTargetDelaysAgressor_s = findingTargetDelaysAgressor;
        botCamoulageIds_s = botCamoulageIds;
        botDecalIds_s = botDecalIds;
    }
}
