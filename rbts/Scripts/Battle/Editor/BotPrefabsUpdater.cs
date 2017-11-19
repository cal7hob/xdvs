using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

class BotPrefabsUpdater : EditorWindow
{
    [MenuItem("HelpTools/Bots updater")]
    private static void Init()
    {
        BotPrefabsUpdater window = GetWindow<BotPrefabsUpdater>();
        window.Show();
    }

    void OnGUI()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.LabelField("Bot prefab updating:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("will delete all related bot prefabs and create new ones, filled with native assets data:", EditorStyles.miniLabel);

        EditorGUILayout.BeginHorizontal();

        GUI.color = Color.red;

        if (GUILayout.Button("Update bot prefabs", GUILayout.ExpandWidth(false)))
        {
            UpdateBotPrefabs();
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

    private static void UpdateBotPrefabs()
    {
        Regex prefabNameRegexp = new Regex("[0-9]{2,}");

        var botVehicles = Resources.LoadAll<VehicleController>(string.Format("{0}/Bots",
            GameManager.CurrentResourcesFolder));

        foreach (var botVehicle in botVehicles)
        {
            DestroyImmediate(botVehicle.gameObject, true);
        }

        var vehicles = Resources.LoadAll<VehicleController>(string.Format("{0}/BattleVehicles",
            GameManager.CurrentResourcesFolder));

        foreach (VehicleController resVeh in vehicles)
        {
            VehicleController vehicleController = Instantiate(resVeh);
            GameObject vehicleObject = vehicleController.gameObject;
            VehicleController botVehicleController = null;

            if (vehicleController is RobotController)
                botVehicleController = vehicleObject.AddComponent<RobotBotController>();
            else if (vehicleController is TankController)
                botVehicleController = vehicleObject.AddComponent<TankBotController>();

            if (botVehicleController != null)
            {
                botVehicleController.UpdateBotPrefabs(vehicleController);
                botVehicleController.GetComponent<PhotonView>().ObservedComponents = new List<Component> { botVehicleController };
            }

            DestroyImmediate(vehicleController);
            var prefabName = prefabNameRegexp.Match(vehicleObject.name);
            PrefabUtility.CreatePrefab(
                string.Format("Assets/Resources/{0}/Bots/{1}.prefab", GameManager.CurrentResourcesFolder, prefabName), vehicleObject);
            Debug.Log(string.Format("prefab {0} updated successefully", prefabName));
            DestroyImmediate(vehicleObject);
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
