using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using AssetBundles;

public class HangarVehiclesSwitcher : MonoBehaviour
{
    private const string COMING_SOON_NAME = "Tank_ComingSoon";

    private BodykitController bodykitController;
    private IEnumerator changingVehicleMaterialRoutine;

    public static HangarVehiclesSwitcher Instance { get; private set; }

    public static HangarVehicleCamParams HangarVehicleCamParams { get; private set; }

    public HangarVehicle HangarVehicle { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        Instance = null;
    }

    void OnEnable()
    {
        StartCoroutine(LoadGeometry());
        Dispatcher.Subscribe(EventId.QualityLevelChanged, OnQualityLevelChanged);
    }

    void OnDisable()
    {
        UnloadGeometry();
        Dispatcher.Unsubscribe(EventId.QualityLevelChanged, OnQualityLevelChanged);
    }
    
    /* PUBLIC SECTION */

    public void Init()
    {
        HangarVehicle = GetComponent<HangarVehicle>();
        bodykitController = GetComponent<BodykitController>();
    }

    /* PRIVATE SECTION */

    private void OnQualityLevelChanged(EventId id, EventInfo ei)
    {
        StartCoroutine(ChangeMaterials());
    }

    private IEnumerator ChangeMaterials()
    {
        if (changingVehicleMaterialRoutine != null)
            QualityManager.Instance.StopCoroutine(changingVehicleMaterialRoutine);

        changingVehicleMaterialRoutine
            = QualityManager.Instance.ObjectMaterialsChanging(
                obj:            gameObject,
                graphicsLevel:  GraphicsLevel.ultraQuality,
                immediate:      true);

        yield return StartCoroutine(changingVehicleMaterialRoutine);
    }

    private IEnumerator LoadGeometry()
    {
        string vehicleName = HangarVehicle.Info.isComingSoon ? COMING_SOON_NAME : name;

        string bundle = string.Format("{0}/vehicleshangar", GameManager.CurrentResourcesFolder).ToLower();
        AssetBundleLoadAssetOperation request = AssetBundleManager.LoadAssetAsync(bundle, vehicleName, typeof(GameObject));

        if (request == null)
            yield break;

        yield return StartCoroutine(request);

        // Get the asset.
        Object obj = request.GetAsset<GameObject>();

        if (obj == null)
        {
            Debug.LogErrorFormat("There is no vehicle prefab in bundle {0} with name {1}.", bundle, vehicleName);
            yield break;
        }
            
        GameObject go = (GameObject)Instantiate(obj);

        Transform vehicleTransform = go.transform;

        List<Transform> children = new List<Transform>(vehicleTransform.childCount);

        foreach (Transform child in vehicleTransform)
            children.Add(child);

        foreach (Transform child in children)
            child.SetParent(transform, false);

        Destroy(vehicleTransform.gameObject);

        yield return StartCoroutine(ChangeMaterials());

        bodykitController.Init();

        UserVehicle userVehicle
            = VehicleShop.Selectors.ContainsKey(HangarVehicle.Info.id)
                ? VehicleShop.Selectors[HangarVehicle.Info.id].UserVehicle
                : null;

        if (userVehicle == null)
            yield break;

        userVehicle.TryOnCamouflage(PatternPool.Instance.GetItemById(userVehicle.Upgrades.CamouflageId));
        userVehicle.TryOnDecal(DecalPool.Instance.GetItemById(userVehicle.Upgrades.DecalId));

        CenterVehicle();

        HangarVehicleCamParams = HangarVehicle.GetComponent<HangarVehicleCamParams>();

        HangarCameraController.Instance.FindCamLookPoints();
        HangarCameraController.Instance.OnVehicleSelected(0, null);

        Dispatcher.Send(EventId.HangarVehicleGeometryLoaded, new EventInfo_I(HangarVehicle.Info.id));
    }

    private void CenterVehicle()
    {
        if (!GameData.IsGame(Game.Armada | Game.MetalForce))
            return; // TODO: доделать для остальных проектов. Пока нормально центруется только там, где есть вложенный Body.

        float offsetZ = HangarCameraController.Instance.transform.position.z - HangarVehicle.BodyRenderer.bounds.center.z;
        float offsetX = HangarCameraController.Instance.transform.position.x - HangarVehicle.BodyRenderer.bounds.center.x;

        Vector3 pos = HangarVehicle.transform.position;

        pos.z += offsetZ;
        pos.x += offsetX;

        HangarVehicle.transform.position = pos;
    }
    
    private void UnloadGeometry()
    {
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        Resources.UnloadUnusedAssets();

        System.GC.Collect();
    }
}
