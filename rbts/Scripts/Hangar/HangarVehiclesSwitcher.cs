using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using Object = UnityEngine.Object;

public class HangarVehiclesSwitcher : MonoBehaviour
{
    private const string COMING_SOON_NAME = "Tank_ComingSoon";
    private const string VEHICLES_PATH = "HangarVehicles/";

    private IEnumerator loadGeometryRoutine;

    private BodykitController bodykitController;

    private BodykitController BodykitController
    {
        get
        {
            if (bodykitController == null)
                bodykitController = GetComponent<BodykitController>();
            
            return bodykitController;
        }
    }

    private HangarVehicle hangarVehicle;

    public HangarVehicle HangarVehicle
    {
        get
        {
            if (hangarVehicle == null)
                hangarVehicle = GetComponent<HangarVehicle>();

            return hangarVehicle;
        }
    }

    public static HangarVehiclesSwitcher Instance { get; private set; }

    public static HangarVehicleCamParams HangarVehicleCamParams { get; private set; }

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
        loadGeometryRoutine = LoadGeometry();
        StartCoroutine(loadGeometryRoutine);
    }

    void OnDisable()
    {
        if (loadGeometryRoutine != null)
        {
            StopCoroutine(loadGeometryRoutine);
            loadGeometryRoutine = null;
        }

        UnloadGeometry();
    }
	
	/* PRIVATE SECTION */
	private IEnumerator LoadGeometry()
	{
        string vehicleName = HangarVehicle.Info.isComingSoon ? COMING_SOON_NAME : name;

        string path = string.Format("{0}/{1}{2}", GameManager.CurrentResourcesFolder, VEHICLES_PATH, vehicleName);

        ResourceRequest request = Resources.LoadAsync<GameObject>(path);
	    yield return request;

	    GameObject obj = request.asset as GameObject;

        if (obj == null)
        {
            Debug.LogErrorFormat("There is no tank prefab at {0}", path);
            yield break;
        }
            
        GameObject go = Instantiate(obj);
	    //StartCoroutine(QualityManager.SetMaterial(go, Settings.Instance));
		Transform vehicleTransform = go.transform;
		List<Transform> children = new List<Transform>(vehicleTransform.childCount);

		foreach (Transform child in vehicleTransform)
			children.Add(child);

		foreach (Transform child in children)
			child.SetParent(transform, false);

		Destroy(vehicleTransform.gameObject);

		BodykitController.Init();

        UserVehicle userVehicle
            = VehicleShop.Selectors.ContainsKey(HangarVehicle.Info.id)
                ? VehicleShop.Selectors[HangarVehicle.Info.id].UserVehicle
                : null;

	    if (userVehicle == null)
	        yield break;

		userVehicle.TryOnCamouflage(PatternPool.Instance.GetItemById(userVehicle.Upgrades.CamouflageId));
		userVehicle.TryOnDecal(DecalPool.Instance.GetItemById(userVehicle.Upgrades.DecalId));

        HangarVehicleCamParams = HangarVehicle.GetComponent<HangarVehicleCamParams>();
        HangarCameraController.Instance.FindCamLookPoints();
        HangarCameraController.Instance.OnVehicleSelected(0, null);

        Messenger.Send(EventId.HangarVehicleGeometryLoaded, new EventInfo_I(HangarVehicle.Info.id));
	}
	
	private void UnloadGeometry()
	{
		foreach (Transform child in transform)
			Destroy(child.gameObject);

/*		Resources.UnloadUnusedAssets();
		System.GC.Collect();*/
	}
}
