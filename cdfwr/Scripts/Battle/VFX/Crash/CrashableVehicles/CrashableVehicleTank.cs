using UnityEngine;
using XDevs.LiteralKeys;

public class CrashableVehicleTank : CrashableVehicleBase
{
    private const float DESCENDING_SPEED = 0.33f;
    private const float DESCENDING_DEPTH = 3.5f;
    private const float DISTANCE_COMPARISON_ACCURACY = 0.01f;

    private TankController tankController;
    private GraveYard graveYard;
    private Vector3 initLocalPosition;
    private Quaternion initLocalRotation;
    private Vector3 crashModelInitLocalPosition;
    private Quaternion crashModelInitLocalRotation;
    private Transform initialParent;

    private Vector3 destination;
    private bool m_doAnimation = false;

    protected override void Awake()
    {
        base.Awake();
        tankController = GetComponent<TankController>();
        graveYard = GetComponentInChildren<GraveYard>(true);

        graveYard.Init(tankController);

        initLocalPosition = graveYard.transform.localPosition;
        initLocalRotation = graveYard.transform.localRotation;
        crashModelInitLocalPosition = graveYard.CrashedVehicle.localPosition;
        crashModelInitLocalRotation = graveYard.CrashedVehicle.localRotation;
        initialParent = graveYard.transform.parent;

        Dispatcher.Subscribe(EventId.TankRespawned, OnTankRespawned);
        Dispatcher.Subscribe(EventId.MainTankAppeared, OnMainTankAppeared);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Dispatcher.Unsubscribe(EventId.TankRespawned, OnTankRespawned);
        Dispatcher.Unsubscribe(EventId.MainTankAppeared, OnMainTankAppeared);
    }

    protected override void OnTankKilled(EventId id, EventInfo ei)
    {
        int victimId = ((EventInfo_II)ei).int1;

        if (victimId == tankController.data.playerId)
            Crash();

        base.OnTankKilled(id, ei);
    }

    private void OnTankRespawned(EventId id, EventInfo ei)
    {
        int victimId = ((EventInfo_I)ei).int1;

        if (victimId == tankController.data.playerId)
            Restore();
    }

    private void OnMainTankAppeared(EventId id, EventInfo ei)
    {
        Restore();
    }

    private void Crash()
    {
        graveYard.transform.parent = null;
        graveYard.Turret.localRotation = tankController.Turret.localRotation;

        Transform[] children = graveYard.gameObject.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
            child.gameObject.layer = LayerMask.NameToLayer(Layer.Items[Layer.Key.Default]);

        foreach (MeshRenderer meshRenderer in graveYard.gameObject.GetComponentsInChildren<MeshRenderer>(true))
            meshRenderer.enabled = true;

        graveYard.SetVisible(true);

        destination = graveYard.CrashedVehicle.transform.position + Vector3.down * DESCENDING_DEPTH;
        m_doAnimation = true;
        //StartCoroutine (Descending());
    }

    private void Update()
    {
        if (!m_doAnimation) return;

        graveYard.CrashedVehicle.position
            = Vector3.MoveTowards(
                current: graveYard.CrashedVehicle.position,
                target: destination,
                maxDistanceDelta: DESCENDING_SPEED * Time.deltaTime);
        m_doAnimation = Mathf.Abs(Vector3.Distance(graveYard.transform.position, destination)) > DISTANCE_COMPARISON_ACCURACY;
    }

    private void Restore()
    {
        m_doAnimation = false;
        graveYard.transform.parent = initialParent;
        graveYard.transform.localPosition = initLocalPosition;
        graveYard.transform.localRotation = initLocalRotation;
        graveYard.CrashedVehicle.localPosition = crashModelInitLocalPosition;
        graveYard.CrashedVehicle.localRotation = crashModelInitLocalRotation;

        if (graveYard.Turret != null)
            graveYard.Turret.localRotation = Quaternion.identity;
        else
            Debug.LogWarning("Не навешен GraveYard.turret!", graveYard.gameObject);

        graveYard.SetVisible(false);
    }
}