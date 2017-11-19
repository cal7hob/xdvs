using UnityEngine;

public class BotSettings : MonoBehaviour
{
    [SerializeField] private float clearWaypointDistance = 1;
    [SerializeField] private float minStopDistanceToOtherVehicles = 150;
    [SerializeField] private float maxStopDistanceToOtherVehicles = 200;
    [SerializeField] private float minTimeToStuck = 1.5f;
    [SerializeField] private float maxTimeToStuck = 2.5f;
    [SerializeField] private float minPathUpdatePeriod = 1;
    [SerializeField] private float maxPathUpdatePeriod = 4;
    [SerializeField] private float stuckVelocitySqrMagnitude = 2;
    [SerializeField] private float stuckMinSqrDistanceToOtherVehicle = 5;
    [SerializeField] private float minRevengeDelay = 0;
    [SerializeField] private float maxRevengeDelay = 3;
    [SerializeField] private float bonusSeekingRadius = 50;
    [SerializeField] private float targetBotRevengeChance = 85;
    [SerializeField] private float oneShotTargetCheckDistance = 20;
    private float                  oneShotTargetCheckSqrDistance;
    [SerializeField] private float broForgivenessTimeout = 40;
    [SerializeField] private float setTargetPreferenceDelay = 15;

    public static float clearWaypointDistance_s;
    public static float minStopDistanceToOtherVehicles_s;
    public static float maxStopDistanceToOtherVehicles_s;
    public static float minTimeToStuck_s;
    public static float maxTimeToStuck_s;
    public static float minPathUpdatePeriod_s;
    public static float maxPathUpdatePeriod_s;
    public static float stuckSqrMagnitude_s;
    public static float stuckMinSqrDistance_s;
    public static float minRevengeDelay_s;
    public static float maxRevengeDelay_s;
    public static float bonusSeekingRadius_s;
    public static float targetBotRevengeChance_s;
    public static float oneShotTargetCheckDistance_s;
    public static float oneShotTargetCheckSqrDistance_s;
    public static float broForgivenessTimeout_s;
    public static float setTargetPreferenceDelay_s;


    void Start()
    {
        oneShotTargetCheckSqrDistance = Mathf.Pow(oneShotTargetCheckDistance, 2);

        clearWaypointDistance_s = clearWaypointDistance;
        minStopDistanceToOtherVehicles_s = minStopDistanceToOtherVehicles;
        maxStopDistanceToOtherVehicles_s = maxStopDistanceToOtherVehicles;
        minTimeToStuck_s = minTimeToStuck;
        maxTimeToStuck_s = maxTimeToStuck;
        minPathUpdatePeriod_s = minPathUpdatePeriod;
        maxPathUpdatePeriod_s = maxPathUpdatePeriod;
        stuckSqrMagnitude_s = stuckVelocitySqrMagnitude;
        stuckMinSqrDistance_s = stuckMinSqrDistanceToOtherVehicle;
        minRevengeDelay_s = minRevengeDelay;
        maxRevengeDelay_s = maxRevengeDelay;
        bonusSeekingRadius_s = bonusSeekingRadius;
        targetBotRevengeChance_s = targetBotRevengeChance;
        oneShotTargetCheckDistance_s = oneShotTargetCheckDistance;
        oneShotTargetCheckSqrDistance_s = oneShotTargetCheckSqrDistance;
        broForgivenessTimeout_s = broForgivenessTimeout;
        setTargetPreferenceDelay_s = setTargetPreferenceDelay;
    }
}
