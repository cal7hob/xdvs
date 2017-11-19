using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AppBuild;

[CustomEditor(typeof(BotDispatcher))]
class BotPrefabsUpdater : Editor
{
    private bool inResources;
    private int selectedBotType;
    private string[] botTypeValues;

    enum botTypes
    {
        tanks,
        tanksAR,
        copters,
        aircrafts,
        spaceShips,
        tanksMF
    }

    void OnEnable()
    {
        botTypeValues = new[]
        {
            botTypes.tanks.ToString(), botTypes.tanksAR.ToString(),
            botTypes.copters.ToString(), botTypes.aircrafts.ToString(),
            botTypes.spaceShips.ToString(), botTypes.tanksMF.ToString(),
        };
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.LabelField("Bot prefab updating:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("will delete all related bot prefabs and create new ones, filled with native assets data:", EditorStyles.miniLabel);

        inResources = EditorGUILayout.Toggle("In Resources", inResources);

        EditorGUILayout.BeginHorizontal();

        selectedBotType = EditorGUILayout.Popup("", selectedBotType, botTypeValues, GUILayout.MaxWidth(100));
        GUI.color = Color.red;

        if (GUILayout.Button("Update bot prefabs", GUILayout.ExpandWidth(false)))
        {
            UpdateBotPrefabs(selectedBotType, inResources);
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical();

        EditorGUILayout.LabelField("Для добавления всяких объектов, точек и тп в префабы", EditorStyles.miniLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("helper", GUILayout.ExpandWidth(false)))
        {
            Helper();
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private static VehicleController[] LoadPrefabs(string path, bool inResources)
    {
        if (inResources)
            return Resources.LoadAll<VehicleController>(string.Format("{0}{1}", GameManager.CurrentResourcesFolder, path));

        var assetsPath = string.Format("Assets/{0}/Vehicles{1}", GameData.assetsProjectFolder[GameData.CurInterface], path);
        var result = new List<VehicleController>();
        var allAssetPaths = AssetDatabase.GetAllAssetPaths();

        foreach (string assetPath in allAssetPaths)
        {
            if (!assetPath.StartsWith(assetsPath))
                continue;

            var asset = AssetDatabase.LoadAssetAtPath<VehicleController>(assetPath);

            if (asset != null)
                result.Add(asset);
        }

        result = result.OrderBy(veh => veh.name).ToList();

        return result.ToArray();
    }

    private static void UpdateBotPrefabs(int selectedBotType, bool inResources)
    {
        Regex prefabNameRegexp = new Regex("[0-9]{2,}");

        VehicleController[] vehicleBotPrefabs = LoadPrefabs("/Bots", inResources);

        foreach (var botVehicle in vehicleBotPrefabs)
            DestroyImmediate(botVehicle.gameObject, true);

        VehicleController[] vehicleBattlePrefabs = LoadPrefabs(string.Empty, inResources);

        foreach (VehicleController vehicleBattlePrefab in vehicleBattlePrefabs)
            Instantiate(vehicleBattlePrefab);

        VehicleController[] vehicleInstances = FindObjectsOfType<VehicleController>();

        foreach (VehicleController vehicleInstance in vehicleInstances)
        {
            if (vehicleInstance == null)
                continue;

            GameObject vehicleGO = vehicleInstance.gameObject;
            vehicleGO.name = vehicleInstance.gameObject.name; // Клон-хуён.

            VehicleController botVehicleController = null;

            switch (selectedBotType)
            {
                case (int)botTypes.tanks:
                    botVehicleController = vehicleGO.AddComponent<TankBotController>();
                    break;
                case (int)botTypes.tanksAR:
                    botVehicleController = vehicleGO.AddComponent<TankBotControllerAR>();
                    break;
                case (int)botTypes.spaceShips:
                    botVehicleController = vehicleGO.AddComponent<SpaceshipBotController>();
                    break;
                case (int)botTypes.aircrafts:
                    botVehicleController = vehicleGO.AddComponent<AircraftBotController>();
                    break;
                case (int)botTypes.copters:
                    botVehicleController = vehicleGO.AddComponent<HelicopterBotController>();
                    break;
                case (int)botTypes.tanksMF:
                    botVehicleController = vehicleGO.AddComponent<TankBotControllerMF>();
                    break;
            }

            if (botVehicleController != null)
            {
                botVehicleController.UpdateBotPrefabs(vehicleInstance); // TODO: переделать бы это всё, а то так какое-нибудь новое поле забыть можно.

                PhotonView photonView = botVehicleController.GetComponent<PhotonView>();

                photonView.ObservedComponents = new List<Component> { botVehicleController };

                PhotonTransformView photonTransformView = botVehicleController.GetComponent<PhotonTransformView>();
                PhotonRigidbodyView photonRigidbodyView = botVehicleController.GetComponent<PhotonRigidbodyView>();

                if (photonTransformView != null)
                    photonView.ObservedComponents.Add(photonTransformView);

                if (photonRigidbodyView != null)
                    photonView.ObservedComponents.Add(photonRigidbodyView);

                EditorUtility.SetDirty(vehicleGO);
            }

            Match prefabName = prefabNameRegexp.Match(vehicleGO.name);

            DestroyImmediate(vehicleInstance);

            string botPrefabPath = string.Format("Assets/{0}/Vehicles/Bots/{1}.prefab", GameData.assetsProjectFolder[GameData.CurInterface], prefabName);

            if (inResources)
                botPrefabPath = string.Format("Assets/Resources/{0}/Bots/{1}.prefab", GameManager.CurrentResourcesFolder, prefabName);

            PrefabUtility.CreatePrefab(botPrefabPath, vehicleGO);

            DestroyImmediate(vehicleGO);
        }
    }

    private static void Helper()
    {
        var vehs = Resources.LoadAll<VehicleController>(string.Format("{0}", GameManager.CurrentResourcesFolder));
        var vehsList = new List<VehicleController>();

        foreach (var vehiclePrefab in vehs)
        {
            var vehicle = Instantiate(vehiclePrefab);
            vehsList.Add(vehicle);

            var viewPoint = vehicle.transform.Find("ViewPoint");
            if(viewPoint)
            {
                viewPoint.name = "LookPoint";
                vehicle.lookPoint = viewPoint;
            }
            //vehicle.forCam = vehicle.Turret.Find("ForCamera");

            EditorUtility.SetDirty(vehicle);
            PrefabUtility.ReplacePrefab(vehicle.gameObject, vehiclePrefab);
            Debug.Log(string.Format("{0} updated", vehicle.name));
        }

        for (int i = 0; i < vehsList.Count; i++)
        {
            DestroyImmediate(vehsList[i].gameObject);
        }
    }
}
