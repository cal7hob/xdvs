using System;
using UnityEngine;
using System.Collections.Generic;
using System.Globalization;
using Bots;
using UnityEditor;

public class BotDebagInfoWindow : EditorWindow
{
    private HashSet<BotAI> bots = new HashSet<BotAI>();
    private static GUIStyle textStyle = new GUIStyle();
    private static string pattern;
    private static bool showBotsOnScn;

    private Color[] colors = {
        Color.yellow,
        Color.blue,
        Color.green,
        Color.magenta,
        Color.cyan,
        Color.white,
        Color.red,
        Color.black,
        Color.clear
    };

    private bool IsMaster { get { return PhotonNetwork.inRoom && PhotonNetwork.isMasterClient; } }

    [MenuItem("HelpTools/Bot Debag Infos")]
    private static void Init()
    {
        var window = GetWindow<BotDebagInfoWindow>(true, "Bot debag infos");
        window.Show();

        textStyle.normal.textColor = Color.yellow;
        textStyle.fontSize = 10;
        textStyle.fontStyle = FontStyle.Bold;

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
positionToGoTo: {12} 
selfPosition: {13}
closestVehicle: {14}
closestEnemyVehicle: {15}
";
    }

    void OnEnable()
    {
        SceneView.onSceneGUIDelegate += OnSceneGUI;
        CollectBots();

        Dispatcher.Subscribe(EventId.TankJoinedBattle, CollectBots);
    }

    void OnDisable()
    {
        SceneView.onSceneGUIDelegate -= OnSceneGUI;
        Dispatcher.Unsubscribe(EventId.TankJoinedBattle, CollectBots);
    }

    void OnGUI()
    {
        if (!IsMaster)
        {
            GUILayout.BeginHorizontal();
            DrawTableColumn("You are not master client...", true, 300);
            GUILayout.EndHorizontal();
            return;
        }

        if (bots.Count == 0)
        {
            GUILayout.BeginHorizontal();
            DrawTableColumn("There are no bots on the scene", true, 300);
            GUILayout.EndHorizontal();
            return;
        }

        GUILayout.BeginHorizontal(GUI.skin.box);
        showBotsOnScn = EditorGUILayout.Toggle("Show bot infos on scene", showBotsOnScn);

        if (GUILayout.Button("Refresh", GUILayout.Width(100)))
            CollectBots();

        GUILayout.EndHorizontal();

        FillTable();
    }

    void OnInspectorUpdate()
    {
        Repaint();
    }

    private void FillTable()
    {
        GUILayout.BeginHorizontal(GUI.skin.box);
        DrawTableColumn("name", true);
        DrawTableColumn("scn name", true);
        DrawTableColumn("target", true);
        DrawTableColumn("aimed", true);
        DrawTableColumn("reloading", true);
        DrawTableColumn("X Axis", true);
        DrawTableColumn("Y Axis", true);
        DrawTableColumn("Fire pressed", true);
        DrawTableColumn("Armor", true);
        DrawTableColumn("IsAvailable", true);

        GUILayout.EndHorizontal();

        foreach (var bot in bots)
        {
            if (bot == null)
                continue;

            GUILayout.BeginHorizontal();
            DrawTableColumn(bot.SlaveController.data.playerName);
            DrawTableColumn(bot.SlaveController.name);
            DrawTableColumn(bot.Target ? bot.Target.data.playerName : null); // bot.BotAI.Target ? bot.BotAI.Target.data.playerName : null
            DrawTableColumn(bot.SlaveController.TargetAimed.ToString()); // bot.TargetAimed.ToString()
            DrawTableColumn(string.Empty); // bot.WeaponReloadingProgress.ToString(CultureInfo.InvariantCulture)
            DrawTableColumn(bot.BotXAxisControl.ToString(CultureInfo.InvariantCulture));
            DrawTableColumn(bot.BotYAxisControl.ToString(CultureInfo.InvariantCulture));
            DrawTableColumn(string.Empty); //bot.BotAI.FireButtonPressed.ToString()
            DrawTableColumn(bot.SlaveController.Armor.ToString()); // bot.Armor.ToString()
            DrawTableColumn(bot.SlaveController.IsAvailable.ToString()); // bot.IsAvailable.ToString()
            GUILayout.EndHorizontal();
        }
    }

    private void DrawTableColumn(string colName, bool bold = false, int width = 80)
    {
        GUILayout.BeginVertical();
        GUILayout.Label(colName, bold ? EditorStyles.boldLabel : EditorStyles.label, GUILayout.Width(width));
        GUILayout.EndVertical();
    }

    void OnSceneGUI(SceneView sceneView)
    {
        if (!showBotsOnScn || bots.Count == 0 || BattleController.Instance == null || !IsMaster)
            return;

        int i = 0;
        foreach (var bot in bots)
        {
            if (bot == null)
                continue;

            textStyle.normal.textColor = colors[i++];

            Handles.Label(bot.SlaveController.IndicatorPointPosition,
                string.Format(
                    pattern, bot.SlaveController.data.playerName,
                    bot.name, string.Empty, //bot.BotAI.CurrentBehaviour.GetType().Name,
                    string.Empty, //bot.BotAI.CurrentState.GetType().Name,
                    string.Empty, //bot.BotAI.Target ? bot.BotAI.Target : null,
                    bot.SlaveController.TargetAimed, //bot.TargetAimed,
                    string.Empty, //bot.WeaponReloadingProgress,
                    bot.BotXAxisControl,
                    bot.BotYAxisControl,
                    string.Empty, //bot.BotAI.FireButtonPressed,
                    string.Empty, //bot.Armor,
                    string.Empty, //bot.IsAvailable,
                    string.Empty, //bot.BotAI.PositionToMove,
                    string.Empty, //bot.transform.position,
                    string.Empty, //bot.BotAI.ClosestVehicle == null ? null : bot.BotAI.ClosestVehicle.data.playerName,
                    string.Empty, textStyle));//bot.BotAI.ClosestEnemyVehicle == null ? null : bot.BotAI.ClosestEnemyVehicle.data.playerName), textStyle);
        }
    }


    private void CollectBots(EventId id = 0, EventInfo info = null)
    {
        if (!IsMaster)
            return;

        BotAI[] vehs = FindObjectsOfType<BotAI>();
        bots.Clear();
        foreach (var veh in vehs)
        {
            bots.Add(veh);
        }
    }
}
