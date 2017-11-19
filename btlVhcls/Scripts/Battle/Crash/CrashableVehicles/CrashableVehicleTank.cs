using UnityEngine;
using XDevs.LiteralKeys;

public class CrashableVehicleTank : CrashableVehicleBase
{
    private const float DESCENDING_SPEED = 0.33f;
    private const float DESCENDING_DEPTH = 3.5f;
    private const float DISTANCE_COMPARISON_ACCURACY = 0.01f;

    private TankController tankController;
    private CrashTransformVehicle crashObject;
    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;
    private Transform initialParent;

    private Vector3 destination;
    private bool m_doAnimation = false;

    override protected void Awake()
    {
        base.Awake ();
        tankController = GetComponent<TankController>();
        crashObject = GetComponentInChildren<CrashTransformVehicle>(true);

        crashObject.Init(tankController);

        initialLocalPosition = crashObject.transform.localPosition;
        initialLocalRotation = crashObject.transform.localRotation;
        initialParent = crashObject.transform.parent;

        Dispatcher.Subscribe(EventId.TankRespawned, OnTankRespawned);
        Dispatcher.Subscribe(EventId.MainTankAppeared, OnMainTankAppeared);
    }

    override protected void OnDestroy()
    {
        base.OnDestroy ();
        Dispatcher.Unsubscribe(EventId.TankRespawned, OnTankRespawned);
        Dispatcher.Unsubscribe(EventId.MainTankAppeared, OnMainTankAppeared);
    }

    override protected void OnTankKilled(EventId id, EventInfo ei)
    {
        int victimId = ((EventInfo_III)ei).int1;

        if (victimId == tankController.data.playerId)
            Crash();

        base.OnTankKilled (id, ei);
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
        crashObject.transform.parent = null;
        crashObject.turret.localRotation = tankController.Turret.localRotation;

        Transform[] children = crashObject.gameObject.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
            child.gameObject.layer = LayerMask.NameToLayer(Layer.Items[Layer.Key.Default]);

        foreach (MeshRenderer meshRenderer in crashObject.gameObject.GetComponentsInChildren<MeshRenderer>(true))
            meshRenderer.enabled = true;

        crashObject.gameObject.SetActive(true);

        destination = crashObject.transform.position + Vector3.down * DESCENDING_DEPTH;
        m_doAnimation = true;
        //StartCoroutine (Descending());
    }

    private void Update () {
        if (!m_doAnimation) return;

        crashObject.transform.position
            = Vector3.MoveTowards (
                current: crashObject.transform.position,
                target: destination,
                maxDistanceDelta: DESCENDING_SPEED * Time.deltaTime);
        m_doAnimation = Mathf.Abs (Vector3.Distance (crashObject.transform.position, destination)) > DISTANCE_COMPARISON_ACCURACY;
    }

    private void Restore()
    {
        m_doAnimation = false;
        crashObject.transform.parent = initialParent;
        crashObject.transform.localPosition = initialLocalPosition;
        crashObject.transform.localRotation = initialLocalRotation;

        if (crashObject.turret != null)
            crashObject.turret.localRotation = Quaternion.identity;
        else
            Debug.LogWarning("Не навешен CrashTransformVehicle.turret!", crashObject.gameObject);

        crashObject.gameObject.SetActive(false);
    }
}
