using System.Collections;
using UnityEngine;
using XDevs.LiteralKeys;
using XD;

public class CrashableVehicleTank : CrashableVehicleBase
{
    private const float             DESCENDING_SPEED = 0.33f;
    private const float             DESCENDING_DEPTH = 2.75f;
    private const float             DISTANCE_COMPARISON_ACCURACY = 0.01f;

    [SerializeField]
    private VehicleController       unit = null;
    [SerializeField]
    private CrashTransformVehicle   crashObject = null;
    [SerializeField]
    private Transform               initialParent = null;

    private Vector3                 initialLocalPosition = new Vector3();    
    private Quaternion              initialLocalRotation = new Quaternion();    
    
    private void Awake()
    {
        InitComponents();

        initialLocalPosition = crashObject.transform.localPosition;
        initialLocalRotation = crashObject.transform.localRotation;

        Dispatcher.Subscribe(EventId.TankKilled, OnTankKilled);
        Dispatcher.Subscribe(EventId.TankRespawned, OnTankRespawned);
        Dispatcher.Subscribe(EventId.MainTankAppeared, OnMainTankAppeared);
    }

    public void InitComponents()
    {
        unit = GetComponent<VehicleController>();
        crashObject = GetComponentInChildren<CrashTransformVehicle>(true);
        crashObject.Init(unit);
        initialParent = crashObject.transform.parent;
    }

    private void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.TankKilled, OnTankKilled);
        Dispatcher.Unsubscribe(EventId.TankRespawned, OnTankRespawned);
        Dispatcher.Unsubscribe(EventId.MainTankAppeared, OnMainTankAppeared);
    }

    private void OnTankKilled(EventId id, EventInfo ei)
    {
        int victimId = ((EventInfo_II)ei).int1;

        if (victimId == unit.data.playerId)
        {
            Crash();
        }
    }

    private void OnTankRespawned(EventId id, EventInfo ei)
    {
        int victimId = ((EventInfo_I)ei).int1;

        if (victimId == unit.data.playerId)
        {
            Restore();
        }
    }

    private void OnMainTankAppeared(EventId id, EventInfo ei)
    {
        Restore();
    }

    private void Crash()
    {
        crashObject.transform.SetParent(null);
        crashObject.gameObject.SetLayer(LayerMask.NameToLayer(Layer.Items[Layer.Key.Default]), 0);
        crashObject.Crash();
        crashObject.gameObject.SetActive(true);

        //StartCoroutine(Descending());
    }

    private void Restore()
    {
        crashObject.transform.SetParent(initialParent);
        crashObject.transform.localPosition = initialLocalPosition;
        crashObject.transform.localRotation = initialLocalRotation;

        crashObject.gameObject.SetActive(false);
    }

    private IEnumerator Descending()
    {
        Vector3 destination = crashObject.transform.position + Vector3.down * DESCENDING_DEPTH;

        while (Vector3.Distance(crashObject.transform.position, destination) > DISTANCE_COMPARISON_ACCURACY)
        {
            crashObject.transform.Translate(Vector3.down * DESCENDING_SPEED * Time.deltaTime, Space.World);
            yield return null;
        }
    }
}
