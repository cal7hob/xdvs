using UnityEngine;
using System.Collections.Generic;

public class HangarVehiclesSwitcher : MonoBehaviour
{
    private const string COMING_SOON_NAME = "Tank_ComingSoon";
    private const string VEHICLES_PATH = "HangarVehicles/";

    private BodykitController bodykitController;
    private bool isCamDistanceSet;
    private float minCamDist;

    public HangarVehicle HangarVehicle { get; private set; }

    public static HangarVehicleCamParams HangarVehicleCamParams { get; private set; }

    void OnEnable()
    {
        LoadGeometry();
    }

    void OnDisable()
    {
        UnloadGeometry();
    }
	
	/* PUBLIC SECTION */
	public void Init()
	{
        HangarVehicle = GetComponent<HangarVehicle>();
		bodykitController = GetComponent<BodykitController>();
	}

    /* PRIVATE SECTION */
    private void LoadGeometry()
	{
	    string vehicleName = HangarVehicle.Info.isComingSoon ? COMING_SOON_NAME : name;

        string path = string.Format("{0}/{1}{2}", GameManager.CurrentResourcesFolder, VEHICLES_PATH, vehicleName);

        Object obj = Resources.Load(path, typeof(GameObject));

        if (obj == null)
        {
            Debug.LogErrorFormat("There is no tank prefab at {0}", path);
            return;
        }
            
        GameObject prefab = Instantiate(obj) as GameObject;
	    //StartCoroutine(QualityManager.SetMaterial(go, Settings.Instance));
		Transform vehicleTransform = prefab.transform;
		List<Transform> children = new List<Transform>(vehicleTransform.childCount);

		foreach (Transform child in vehicleTransform)
			children.Add(child);

		foreach (Transform child in children)
			child.SetParent(transform, false);

        CenterVehicle();
        HangarVehicleCamParams = gameObject.GetComponent<HangarVehicleCamParams>();

        if (!isCamDistanceSet)
        {
            var hangarVehiclePrefab = prefab.GetComponent<HangarVehiclePrefab>();

            if (hangarVehiclePrefab.LongestMesh != null)
            {
                var vehLongestMesh = hangarVehiclePrefab.LongestMesh;
                var longestPartTranform = vehLongestMesh.transform;
                var bounds = vehLongestMesh.sharedMesh.bounds;
                var camExtremumPoint = longestPartTranform.TransformPoint(bounds.max + longestPartTranform.forward * HangarCameraController.CamOffset);
               
                HangarVehicleCamParams.MinDistanceToVehicle = Vector3.Distance(HangarCameraController.Instance.transform.position, camExtremumPoint);
                minCamDist = HangarVehicleCamParams.MinDistanceToVehicle;
            }
            isCamDistanceSet = true;
        }
        else
        {
            HangarVehicleCamParams.MinDistanceToVehicle = minCamDist;
        }

        Destroy(vehicleTransform.gameObject);
        bodykitController.Init();

        UserVehicle userVehicle
            = VehicleShop.Selectors.ContainsKey(HangarVehicle.Info.id)
                ? VehicleShop.Selectors[HangarVehicle.Info.id].UserVehicle
                : null;

        if (userVehicle == null)
            return;

		userVehicle.TryOnCamouflage(PatternPool.Instance.GetItemById(userVehicle.Upgrades.CamouflageId));
		userVehicle.TryOnDecal(DecalPool.Instance.GetItemById(userVehicle.Upgrades.DecalId));

        HangarCameraController.Instance.FindCamLookPoints();
        Dispatcher.Send(EventId.HangarVehicleGeometryLoaded, new EventInfo_I(HangarVehicle.Info.id));
    }

    private void CenterVehicle()
    {
        var offsetZ = HangarCameraController.Instance.transform.position.z - HangarVehicle.BodyRenderer.bounds.center.z;
        var offsetX = HangarCameraController.Instance.transform.position.x - HangarVehicle.BodyRenderer.bounds.center.x;
        var pos = HangarVehicle.transform.position;
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
