using UnityEngine;
using System.Collections.Generic;

public class HangarVehiclesSwitcher : MonoBehaviour
{
    private const string COMING_SOON_NAME = "Tank_ComingSoon";
    private const string VEHICLES_PATH = "HangarVehicles/";

    private BodykitController bodykitController;

    public HangarVehicle HangarVehicle { get; private set; }

    public static HangarVehiclesSwitcher Instance { get; private set; }

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
        HangarVehicle = GetComponent<HangarVehicle>();
        bodykitController = GetComponent<BodykitController>();

        LoadGeometry();
    }

    void OnDisable()
    {
        UnloadGeometry();
    }

	/* PRIVATE SECTION */
	private void LoadGeometry()
	{
	    string vehicleName = HangarVehicle.Info.isComingSoon ? COMING_SOON_NAME : name;

        string path = string.Format("{0}/{1}{2}", XD.StaticContainer.GameManager.CurrentResourcesFolder, VEHICLES_PATH, vehicleName);

        Object obj = Resources.Load(path, typeof(GameObject));

        if (obj == null)
        {
            Debug.LogErrorFormat("There is no tank prefab at {0}", path);
            return;
        }
            
        GameObject go = Instantiate(obj) as GameObject;
	    //StartCoroutine(QualityManager.SetMaterial(go, Settings.Instance));
		Transform vehicleTransform = go.transform;
		List<Transform> children = new List<Transform>(vehicleTransform.childCount);

		foreach (Transform child in vehicleTransform)
			children.Add(child);

		foreach (Transform child in children)
			child.SetParent(transform, false);

		Destroy(vehicleTransform.gameObject);

		bodykitController.Init();

        /*UserVehicle userVehicle
            = VehicleShop.Selectors.ContainsKey(HangarVehicle.Info.id)
                ? VehicleShop.Selectors[HangarVehicle.Info.id].UserVehicle
                : null;

        if (userVehicle == null)
            return;

		userVehicle.TryOnCamouflage(PatternPool.Instance.GetItemById(userVehicle.Upgrades.CamouflageId));
		userVehicle.TryOnDecal(DecalPool.Instance.GetItemById(userVehicle.Upgrades.DecalId));

	    CenterVehicle();
        HangarVehicleCamParams = HangarVehicle.GetComponent<HangarVehicleCamParams>();
        HangarCameraController.Instance.FindCamLookPoints();
        HangarCameraController.Instance.OnVehicleSelected(0, null);*/
    }

    private void CenterVehicle()
    {
        if (!GameData.IsGame(Game.Armada2))
        {
            return; //todo: доделать для остальных проектов. Пока нормально центруется только там, где есть вложенный Body
        }

        /*var offsetZ = HangarCameraController.Instance.transform.position.z - HangarVehicle.BodyRenderer.bounds.center.z;
        var offsetX = HangarCameraController.Instance.transform.position.x - HangarVehicle.BodyRenderer.bounds.center.x;
        var pos = HangarVehicle.transform.position;
        pos.z += offsetZ;
        pos.x += offsetX;
        HangarVehicle.transform.position = pos;*/
    }

    private void UnloadGeometry()
	{
		foreach (Transform child in transform)
			Destroy(child.gameObject);

		Resources.UnloadUnusedAssets();
		System.GC.Collect();
	}
}
