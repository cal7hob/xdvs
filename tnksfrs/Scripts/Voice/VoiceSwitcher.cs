using System.Collections;
using UnityEngine;

public class VoiceSwitcher : MonoBehaviour
{
    private IEnumerator checkingVoiceManagerRoutine;

    void Awake()
    {
        CheckVoiceManager();
        Dispatcher.Subscribe(EventId.SettingsSubmited, OnSettingsSubmited);
        Dispatcher.Subscribe(EventId.BattleSettingsSubmited, OnSettingsSubmited);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.SettingsSubmited, OnSettingsSubmited);
        Dispatcher.Unsubscribe(EventId.BattleSettingsSubmited, OnSettingsSubmited);
    }

    private void OnSettingsSubmited(EventId id, EventInfo ei)
    {
        CheckVoiceManager();
    }

    private string GetSuitablePrefabPath()
    {
        var path = XD.StaticContainer.GameManager.CurrentResourcesFolder + "/VoiceManagers/VoiceManager";

        path += !XD.StaticContainer.SceneManager.InBattle ? "Hangar" : "Battle";
        path += !XD.StaticContainer.Profile.BattleTutorialCompleted && XD.StaticContainer.SceneManager.InBattle ? "Tutorial" : string.Empty;
        path += GameData.InterfaceShortName;

        return path;
    }

    private void CheckVoiceManager()
    {
        if (checkingVoiceManagerRoutine != null)
            StopCoroutine(checkingVoiceManagerRoutine);

        checkingVoiceManagerRoutine = CheckingVoiceManager();

        StartCoroutine(checkingVoiceManagerRoutine);
    }

    private IEnumerator CheckingVoiceManager()
    {
        if (XD.StaticContainer.GameManager.CurrentMap == XD.MapId.LoadingScene)
            yield break;
        
        //foreach (VoiceManager voiceManager in FindObjectsOfType<VoiceManager>())
        //    Destroy(voiceManager.gameObject);

        Resources.UnloadUnusedAssets();

        yield return new WaitForEndOfFrame();

        if (!ProfileInfo.isVoiceDisabled && ProfileInfo.IsVoiceDisablingAvailable)
        {
            string path = GetSuitablePrefabPath();

            //VoiceManager voiceManagerPrefab = Resources.Load<VoiceManager>(path);

            //if (voiceManagerPrefab)
            //    Instantiate(voiceManagerPrefab);
            //else
            //    Debug.LogErrorFormat("Can't instantiate VoiceManager prefab at path '{0}'", path);
        }
    }
}
