using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

[CustomEditor(typeof(BotDispatcher))]
class BotAssetUpdater : Editor
{
    private int selectedBotType;
    private string[] botTypeValues;

    enum botTypes
    {
        tanks,
        tanksAR,
        copters,
        aircrafts,
        spaceShips
    }

    void OnEnable()
    {
        botTypeValues = new[]
        {
            botTypes.tanks.ToString(), botTypes.tanksAR.ToString(),
            botTypes.copters.ToString(), botTypes.aircrafts.ToString(),
            botTypes.spaceShips.ToString()
        };
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.LabelField("Bot assets updating:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("will delete all related bot assets and create new ones, filled with native assets data:", EditorStyles.miniLabel);

        EditorGUILayout.BeginHorizontal();

        selectedBotType = EditorGUILayout.Popup("", selectedBotType, botTypeValues, GUILayout.MaxWidth(100));
        GUI.color = Color.red;

        if (GUILayout.Button("Update bot assets", GUILayout.ExpandWidth(false)))
        {
            UpdateBotAssets(selectedBotType);
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private static void UpdateBotAssets(int selectedBotType)
    {
        Regex prefabNameRegexp = new Regex("[0-9]{2,}");

        var botVehicles = Resources.LoadAll<VehicleController>(string.Format("{0}/Bots", XD.StaticContainer.GameManager.CurrentResourcesFolder));

        foreach (var botVehicle in botVehicles)
        {
            DestroyImmediate(botVehicle.gameObject, true);
        }

        VehicleController[] vehicles = Resources.LoadAll<VehicleController>(string.Format("{0}", XD.StaticContainer.GameManager.CurrentResourcesFolder));

        VehicleController[] instantiated = new VehicleController[vehicles.Length];
        for (int i = 0; i < vehicles.Length; i++)
        {
            VehicleController vehicle = vehicles[i];

            if (vehicle == null)
            {
                continue;
            }
            EditorUtility.DisplayProgressBar("Instantiate bot " + i + " of " + vehicles.Length, vehicle.name, i / (float)vehicles.Length);

            var vehicleController = Instantiate(vehicle);

            if (vehicleController == null)
            {
                continue;
            }

            var vehicleObject = vehicleController.gameObject;
            vehicleObject.name = vehicle.gameObject.name;
            VehicleController botVehicleController = null;
            botVehicleController = vehicleObject.AddComponent<TankBotControllerAR>();

            //switch (selectedBotType)
            //{
            //    //case (int) botTypes.tanks:
            //    //    botVehicleController = vehicleObject.AddComponent<TankBotController>();
            //    //    break;
            //    case (int) botTypes.tanksAR:
            //        botVehicleController = vehicleObject.AddComponent<TankBotControllerAR>();
            //        break;
            //    case (int)botTypes.spaceShips:
            //        botVehicleController = vehicleObject.AddComponent<SpaceshipBotController>();
            //        break;
            //}

            if (botVehicleController != null)
            {
                botVehicleController.UpdateBotAssets(vehicleController);
            }
            instantiated[i] = botVehicleController;

            DestroyImmediate(vehicleController);

            //PrefabUtility.CreatePrefab(string.Format("Assets/Resources/{0}/Bots/{1}.prefab", XD.StaticContainer.GameManager.CurrentResourcesFolder, prefabName), vehicleObject);
            //if (vehicle != null)
            //{
            //    Debug.Log(string.Format("Asset {0} updated successefully", vehicle.name));
            //}
            //
            //DestroyImmediate(vehicleObject, true);
        }
        EditorUtility.ClearProgressBar();

        for (int i = 0; i < instantiated.Length; i++)
        {
            GameObject vehicleObject = instantiated[i].gameObject;
            EditorUtility.DisplayProgressBar("Create bot prefab " + i + " of " + instantiated.Length, vehicleObject.name, i / (float)instantiated.Length);

            Match prefabName = prefabNameRegexp.Match(vehicleObject.name);

            PrefabUtility.CreatePrefab(string.Format("Assets/Resources/{0}/Bots/{1}.prefab", XD.StaticContainer.GameManager.CurrentResourcesFolder, prefabName), vehicleObject);
            Debug.Log(string.Format("Asset {0} updated successefully", vehicleObject.name));

            DestroyImmediate(vehicleObject, true);
        }

        EditorUtility.ClearProgressBar();
    }
}