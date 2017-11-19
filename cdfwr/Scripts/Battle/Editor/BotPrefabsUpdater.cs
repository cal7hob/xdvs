using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Bots;

[CustomEditor(typeof(BotDispatcher))]
class BotPrefabsUpdater : Editor
{
    private int selectedBotType;
    private string[] botTypeValues;

    enum botTypes
    {
        soldiers,
    }

    void OnEnable()
    {
        botTypeValues = new[]
        {
            botTypes.soldiers.ToString(),
        };
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.LabelField("Bot prefab updating:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("will delete all related bot prefabs and create new ones, filled with native assets data:", EditorStyles.miniLabel);

        EditorGUILayout.BeginHorizontal();

        selectedBotType = EditorGUILayout.Popup("", selectedBotType, botTypeValues, GUILayout.MaxWidth(100));
        GUI.color = Color.red;

        if (GUILayout.Button("Update bot prefabs", GUILayout.ExpandWidth(false)))
        {
            UpdateBotPrefabs(selectedBotType);
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical();

        //EditorGUILayout.LabelField("Для добавления всяких объектов, точек и тп в префабы", EditorStyles.miniLabel);

        //EditorGUILayout.BeginHorizontal();

        //if (GUILayout.Button("helper", GUILayout.ExpandWidth(false)))
        //{
        //    Helper();
        //}

        //EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private static void UpdateBotPrefabs(int selectedBotType)
    {
        Regex prefabNameRegexp = new Regex("[0-9]{2,}");

        var botVehicles = Resources.LoadAll<VehicleController>(string.Format("{0}/BattleVehicles/BotVehicles", GameManager.CurrentResourcesFolder));

        foreach (var botVehicle in botVehicles)
        {
            DestroyImmediate(botVehicle.gameObject, true);
        }

        var vehicles = Resources.LoadAll<VehicleController>(string.Format("{0}/BattleVehicles/PlayerVehicles", GameManager.CurrentResourcesFolder));

        foreach (var vehicle in vehicles)
        {
            var vehicleController = Instantiate(vehicle);

            if (vehicleController == null)
            {
                continue;
            }

            var vehicleObject = vehicleController.gameObject;
            vehicleObject.name = vehicle.gameObject.name;
            VehicleController botVehicleController = null;

            switch (selectedBotType)
            {
                case (int)botTypes.soldiers:
                    botVehicleController = vehicleObject.AddComponent<SoldierBotController>();
                    break;
            }

            if (botVehicleController != null)
            {
                botVehicleController.UpdateBotPrefabs(vehicleController);
                botVehicleController.GetComponent<PhotonView>().ObservedComponents = new List<Component> { botVehicleController };

                EditorUtility.SetDirty(vehicleObject);
            }

            DestroyImmediate(vehicleController);

            var vehName = vehicle.name;
            var prefabName = prefabNameRegexp.Match(vehicleObject.name);
            
            PrefabUtility.CreatePrefab(string.Format("Assets/Resources/{0}/BattleVehicles/BotVehicles/{1}.prefab", GameManager.CurrentResourcesFolder, prefabName), vehicleObject);

            Debug.Log(string.Format("prefab {0} updated successefully", vehName));
            DestroyImmediate(vehicleObject, true);
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
            //vehicle.cameraPoint = vehicle.Turret.Find("ForCamera");

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
