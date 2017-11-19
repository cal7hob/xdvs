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

    private bool IsMaster { get { return PhotonNetwork.inRoom && PhotonNetwork.isMasterClient; } }

    void Awake()
    {
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
        {
            if (!IsMaster)
            {
                Debug.Log("алё, ты не мастер. хватит жать на эту кнопку");
                return;
            }

            CollectBots();
        }
    }

    void OnSceneGUI(SceneView sceneView)
    {
        if (!active || BattleController.Instance == null || !IsMaster )
            return;

        int i = 0;

        foreach (VehicleController bot in bots)
        {
            i++;

            if (bot == null)
            {
                continue;
            }

            var target = bot.BotAI.Target;

            textStyle.normal.textColor = colors[i];

            Handles.Label(bot.IndicatorPointPosition,
                string.Format(
                    pattern, bot.data.playerName, 
                    bot.name, bot.BotAI.CurrentBehaviour.GetType().Name, 
                    bot.BotAI.CurrentState.GetType().Name, 
                    target ? target : null, 
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


    private void CollectBots()
    {
        if (!IsMaster)
        {
            return;
        }

        VehicleController[] vehs = FindObjectsOfType<VehicleController>();
        bots.Clear();
        foreach (var veh in vehs)
        {
            if (veh.IsBot)
                bots.Add(veh);
        }
    }
}
