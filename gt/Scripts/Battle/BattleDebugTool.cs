#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Text;
using System.Collections.Generic;
using System.Linq;

public class BattleDebugTool : EditorWindow
{
    private StringBuilder battleLog = new StringBuilder(65536);
    private bool loggingBattle;
    
    private void OnGUI()
    {
        if (GUILayout.Button("Check bot's layers"))
        {
            DetectIncorrectBots();
        }
    }
    
    private void DetectIncorrectBots()
    {
        VehicleController[] botsOnScene = FindObjectsOfType<VehicleController>().Where(x => x.IsBot).ToArray();
        if (botsOnScene.Length == 0)
        {
            Debug.Log("There is no bots on the scene");
            return;
        }

        #region Проверка слоев ботов

        int parallelWorldMask = LayerMask.GetMask("ParallelWorld");
        foreach (VehicleController bot in botsOnScene)
        {
            int botLayer = bot.gameObject.layer;
            if (!MiscTools.CheckIfLayerInMask(BotDispatcher.BotsCommonMask, botLayer) && !MiscTools.CheckIfLayerInMask(parallelWorldMask, botLayer))
                Debug.LogFormat(bot.gameObject, "Bot {0} has incorrect layer ({1})", bot.name, LayerMask.LayerToName(botLayer));
        }
        
        #endregion
    }

    [MenuItem("HelpTools/Battle debug tool %#d")]
    private static void ShowBattleDebugTool()
    {
        EditorWindow window = GetWindow<BattleDebugTool>(true);
        window.Show();
    }
}

#endif