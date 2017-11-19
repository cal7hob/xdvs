using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

public class BotControllerHandles : EditorWindow
{
    private HashSet<VehicleController> bots = new HashSet<VehicleController>();
    private bool active;
    private GUIStyle textStyle = new GUIStyle();
    private string pattern;

    void Awake()
    {
        Dispatcher.Subscribe(EventId.TankJoinedBattle, OnNewVehConnected, 4);
        Dispatcher.Subscribe(EventId.TankLeftTheGame, OnVehLeftTheGame);

        textStyle.normal.textColor = Color.yellow;
        textStyle.fontSize = 10;

        pattern = @"
botName: {0}
vehicleName: {1}
behaviour: {2}
state: {3}
target: {4}
aimed: {5}
reloadingProgress: {6}
xAxisControl: {7}
yAxisControl: {8}
fireButtonPressed: {9}
health: {10} 
isAvailable: {11}
humanTargetPreference: {12}
positionToGoTo: {13} 
selfPosition: {14}"; 
    }

    private void OnVehLeftTheGame(EventId id, EventInfo ei)
    {
        CollectBots();
    }

    private void OnNewVehConnected(EventId id, EventInfo ei)
    {
        CollectBots();
    }

    [MenuItem("HelpTools/Bot infos")]
    private static void Init()
    {
        BotControllerHandles window = GetWindow<BotControllerHandles>(true, "Bot spy");
        window.Show();
    }

    void OnEnable()
    {
        SceneView.onSceneGUIDelegate += OnSceneGUI;
        CollectBots();
    }

    void OnDisable()
    {
        SceneView.onSceneGUIDelegate -= OnSceneGUI;
    }

    void OnGUI()
    {
        active = EditorGUILayout.Toggle("Show bot types", active) && bots.Count > 0;
        if (GUILayout.Button("Refresh"))
            CollectBots();
    }

    void OnSceneGUI(SceneView sceneView)
    {
        if (!active || BattleController.Instance == null)
            return;

        foreach (VehicleController bot in bots)
        {
            if (bot == null)
            {
                CollectBots();
                return;
            }

            Handles.Label(bot.IndicatorPointPosition,
                string.Format(
                    pattern, bot.data.playerName, 
                    bot.name, bot.BotAI.CurrentBehaviour.GetType().Name, 
                    bot.BotAI.CurrentState.GetType().Name, 
                    bot.BotAI.CurrentBehaviour.Target == null ? null : bot.BotAI.CurrentBehaviour.Target, 
                    bot.BotAI.TargetAimed, 
                    bot.WeaponReloadingProgress, 
                    bot.BotAI.XAxisControl, 
                    bot.BotAI.YAxisControl,
                    bot.BotAI.FireButtonPressed, 
                    bot.HPSystem.Armor, 
                    bot.IsAvailable,
                    bot.BotAI.CurrentBehaviour.HumanTargetPreference,
                    bot.BotAI.CurrentBehaviour.PositionToMove,
                    bot.transform.position), textStyle);
        }
    }


    private void CollectBots()
    {
        VehicleController[] vehs = FindObjectsOfType<VehicleController>();
        bots.Clear();
        foreach (var veh in vehs)
        {
            if (veh.IsBot)
                bots.Add(veh);
        }
    }
}
