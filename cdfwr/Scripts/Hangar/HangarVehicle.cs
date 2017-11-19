using UnityEngine;

public class HangarVehicle : MonoBehaviour
{
    [SerializeField] private VehicleInfo info;
    private Transform weaponWrapper;

    public IKHangarController IKHangarController { get; private set; }
    public VehicleInfo Info { get { return info; } }
    public Transform WeaponWrapper { get { return weaponWrapper; } }

    public int IdFromObjectName
    {
        get
        {
            string[] nameParts = name.Split('_');
            string idString = nameParts[0];

            return int.Parse(idString);
        }
    }

    public string TechnicalName
    {
        get
        {
            if (GameData.IsGame(Game.CodeOfWar))
            {
                string[] parts = name.Split('_');

                if (parts.Length != 3)
                {
                    DT.LogError("Wrong template name <{0}>! The name must have 3 groups, separated by '_' ", name);
                    return string.Empty;
                }

                return parts[2];
            }

            return string.Empty;
        }
    }

    public GameObject Body
    {
        get
        {
            return transform.Find("Body").gameObject ?? transform.Find("Mesh").gameObject;
        }
    }

    public Renderer BodyRenderer
    {
        get { return Body.GetComponent<Renderer>(); }
    }

    public Mesh BodyMesh
    {
        get { return Body.GetComponent<MeshFilter>().sharedMesh; }
    }

    void Awake()
    {
        Dispatcher.Subscribe(EventId.HangarVehicleGeometryLoaded, ShowCurrentWeapon);
        Dispatcher.Subscribe(EventId.AfterHangarInit, ShowCurrentWeapon);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.HangarVehicleGeometryLoaded, ShowCurrentWeapon);
        Dispatcher.Unsubscribe(EventId.AfterHangarInit, ShowCurrentWeapon);
    }

    private void ShowCurrentWeapon(EventId id, EventInfo info)
    {
        weaponWrapper = transform.FindInHierarhy("WeaponWrapper");
        IKHangarController = transform.GetComponentInChildren<IKHangarController>();
        HangarWeaponsHolder.Instance.GiveCurrentWeaponToSoldier();
    }
}
