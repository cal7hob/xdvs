using UnityEngine;
using XDevs.LiteralKeys;

public class CrashableVehicleSpaceship : CrashableVehicleBase
{
    public new Animation animation;

    private CrashTransformVehicle crashObject;
    private bool isCrashStarted;
    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;
    private Transform initialParent;

    protected override void Awake()
    {
        base.Awake();

        crashObject = GetComponentInChildren<CrashTransformVehicle>(true);

        crashObject.Init(vehicleController);

        initialLocalPosition = crashObject.transform.localPosition;
        initialLocalRotation = crashObject.transform.localRotation;
        initialParent = crashObject.transform.parent;

        Dispatcher.Subscribe(EventId.TankRespawned, OnTankRespawned);
        Dispatcher.Subscribe(EventId.MainTankAppeared, OnMainTankAppeared);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Dispatcher.Unsubscribe(EventId.TankRespawned, OnTankRespawned);
        Dispatcher.Unsubscribe(EventId.MainTankAppeared, OnMainTankAppeared);
    }

    void Update()
    {
        if (!isCrashStarted)
            return;

        // Костыль для привязывания взорванного корабля к краш объекту, чтобы при помирании на высокой 
        // скорости камера была близко к крашу.
        if (vehicleController.PhotonView.isMine)
            vehicleController.transform.position = crashObject.transform.position;

        if (!animation.isPlaying)
        {
            Restore();

            if (vehicleController.data.playerId == BattleController.MyPlayerId)
                Dispatcher.Send(EventId.DeathAnimationDone, new EventInfo_SimpleEvent());
        }
    }

    protected override void OnTankKilled(EventId id, EventInfo ei)
    {
        int victimId = ((EventInfo_III)ei).int1;

        if (victimId == vehicleController.data.playerId)
            Crash();
    }

    private void OnTankRespawned(EventId id, EventInfo ei)
    {
        int victimId = ((EventInfo_I)ei).int1;

        if (victimId == vehicleController.data.playerId)
            Restore();
    }

    private void OnMainTankAppeared(EventId id, EventInfo ei)
    {
        Restore();
    }

    private void Crash()
    {
        crashObject.transform.parent = null;
        crashObject.gameObject.SetActive(true);
        crashObject.transform.position = vehicleController.transform.position;

        Transform[] children = crashObject.gameObject.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
            child.gameObject.layer = LayerMask.NameToLayer(Layer.Items[Layer.Key.Default]);

        foreach (MeshRenderer meshRenderer in crashObject.gameObject.GetComponentsInChildren<MeshRenderer>(true))
            meshRenderer.enabled = true;

        animation.Play();

        isCrashStarted = true;
    }

    private void Restore()
    {
        isCrashStarted = false;

        crashObject.transform.parent = initialParent;
        crashObject.transform.localPosition = initialLocalPosition;
        crashObject.transform.localRotation = initialLocalRotation;
        crashObject.gameObject.SetActive(false);

        animation.Stop();
    }
}
