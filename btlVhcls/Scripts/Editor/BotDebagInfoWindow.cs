using UnityEngine;
using System.Collections.Generic;
using System.Globalization;
using Disconnect;
using UnityEditor;

public class BotDebagInfoWindow : EditorWindow
{
    private HashSet<VehicleController> bots = new HashSet<VehicleController>();
    private static GUIStyle textStyle = new GUIStyle();
    private static string pattern;
    private static bool showBotsOnScn;

    private Color[] colors = {
        Color.black,
        Color.blue,
        Color.green,
        Color.magenta,
        Color.cyan,
        Color.white,
        Color.red,
        Color.yellow,
        Color.clear
    };

    private bool IsMaster { get { return PhotonNetwork.inRoom && BattleConnectManager.IsMasterClient; } }

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
            DrawTableColumn("behaviour", true);
            DrawTableColumn("state", true);
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
            if(bot == null)
                continue;

            GUILayout.BeginHorizontal();
                DrawTableColumn(bot.data.playerName);
                DrawTableColumn(bot.name);
                DrawTableColumn(bot.BotAI.CurrentBehaviour.GetType().Name);
                DrawTableColumn(bot.BotAI.CurrentState.GetType().Name);
                DrawTableColumn(bot.BotAI.Target ? bot.BotAI.Target.name : null);
                DrawTableColumn(bot.TargetAimed.ToString());
                DrawTableColumn(bot.WeaponReloadingProgress.ToString(CultureInfo.InvariantCulture));
                DrawTableColumn(bot.BotAI.XAxisControl.ToString(CultureInfo.InvariantCulture));
                DrawTableColumn(bot.BotAI.YAxisControl.ToString(CultureInfo.InvariantCulture));
                DrawTableColumn(bot.BotAI.FireButtonPressed.ToString());
                DrawTableColumn(bot.Armor.ToString());
                DrawTableColumn(bot.IsAvailable.ToString());
            GUILayout.EndHorizontal();
        }   
    }

    private void DrawTableColumn(string colName, bool bold = false, int width = 120)
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
        foreach(var bot in bots)
        {
            if (bot == null)
                continue;

            textStyle.normal.textColor = colors[i++];

            Handles.Label(bot.IndicatorPointPosition,
                string.Format(
                    pattern, bot.data.playerName,
                    bot.name, bot.BotAI.CurrentBehaviour.GetType().Name,
                    bot.BotAI.CurrentState.GetType().Name,
                    bot.BotAI.Target ? bot.BotAI.Target : null,
                    bot.TargetAimed,
                    bot.WeaponReloadingProgress,
                    bot.BotAI.XAxisControl,
                    bot.BotAI.YAxisControl,
                    bot.BotAI.FireButtonPressed,
                    bot.Armor,
                    bot.IsAvailable,
                    bot.BotAI.PositionToMove,
                    bot.transform.position), textStyle);
        }
    }


    private void CollectBots(EventId id = 0, EventInfo info = null)
    {
        if (!IsMaster)
            return;

        VehicleController[] vehs = FindObjectsOfType<VehicleController>();
        bots.Clear();
        foreach (var veh in vehs)
        {
            if (veh.IsBot)
                bots.Add(veh);
        }
    }
}
