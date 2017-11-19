using UnityEngine;

public class GraveYard : MonoBehaviour
{
    [SerializeField] private Transform crashedVehicle;
    [SerializeField] private Transform turret;
    [SerializeField] private GameObject grave;

    private int playerId;

    public Transform CrashedVehicle { get { return crashedVehicle; } }
    public Transform Turret { get { return turret; } }

    void Awake()
    {
        Dispatcher.Subscribe(EventId.TankLeftTheGame, OnTankLeftTheGame);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.TankLeftTheGame, OnTankLeftTheGame);
    }

    public void Init(VehicleController vehicleController)
    {
        MakeInvisibleInGrave();
        playerId = vehicleController.data.playerId;
    }

    public void SetVisible(bool activate)
    {
        crashedVehicle.gameObject.SetActive(activate);
        grave.SetActive(activate);
    }

    private void OnTankLeftTheGame(EventId id, EventInfo ei)
    {
        EventInfo_I info = (EventInfo_I)ei;

        int playerId = info.int1;

        if (playerId == this.playerId)
            Destroy(gameObject);
    }

    private void MakeInvisibleInGrave()
    {
        var renderers = CrashedVehicle.GetComponentsInChildren<MeshRenderer>();

        foreach (var meshRenderer in renderers)
        {
            meshRenderer.material.renderQueue = 3002; // it`s because the grave shader has a rendererQueue value == 2001 ("Geometry"+1)
        }
    }
}
