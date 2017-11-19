using UnityEngine;
using XDevs.LiteralKeys;

public class IRCM : Shell
{
    public float accelerationRatio = 0.25f;

    private const float GROUND_CHECK_RAY_LENGTH = 0.5f;

    private float currentSpeed;

    protected override void Update()
    {
        RaycastHit hit;

        if (Physics.Raycast(
            /* ray:         */  new Ray(transform.position, Vector3.down),
            /* hitInfo:     */  out hit,
            /* maxDistance: */  GROUND_CHECK_RAY_LENGTH,
            /* layerMask:   */  MiscTools.GetLayerMask(Layer.Key.Default, Layer.Key.Terrain, Layer.Key.Water)) ||
            flightDistance > maxDistance)
        {
            Disactivate();
            return;
        }

        currentSpeed
            = Mathf.Lerp(
                a:  currentSpeed,
                b:  speed,
                t:  accelerationRatio);

        deltaPos = currentSpeed * Time.deltaTime;

        transform.Translate(-transform.up * deltaPos, Space.World);

        flightDistance += deltaPos;
    }

    #if UNITY_EDITOR
    protected override void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(center: transform.position, radius: 1.0f);
    }
    #endif

    public override void Activate(
        VehicleController       owner,
        int                     damage,
        int                     hitMask,
        int                     victimId  = BattleController.DEFAULT_TARGET_ID,
        GunShellInfo.ShellType  shellType = GunShellInfo.ShellType.Usual)
    {
        ownerId = owner.data.playerId;
        Damage = damage;
        this.hitMask = hitMask;
        this.victimId = victimId;
        this.shellType = shellType;

        gameObject.SetActive(true);

        transform.localRotation = Quaternion.identity;

        if (IsLocal && !BotOwns)
            Dispatcher.Send(EventId.MyTankShots, new EventInfo_I((int)shellType));

        if (!GameData.IsGame(Game.BattleOfHelicopters))
            return;

        Dispatcher.Send(
            id:     EventId.ShellStateChanged,
            info:   new EventInfo_BIIII(
                        _bool1: true,
                        _int1:  victimId,
                        _int2:  ownerId,
                        _int3:  (int)shellType,
                        _int4:  id));
    }

    public override void Disactivate()
    {
        base.Disactivate();

        if (!GameData.IsGame(Game.BattleOfHelicopters))
            return;

        Dispatcher.Send(
            id:     EventId.ShellStateChanged,
            info:   new EventInfo_BIIII(
                        _bool1: false,
                        _int1:  victimId,
                        _int2:  ownerId,
                        _int3:  (int)shellType,
                        _int4:  id));
    }
}
