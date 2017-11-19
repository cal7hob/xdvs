using System.Collections;
using UnityEngine;

public class VoiceSwitcher : MonoBehaviour
{
    private IEnumerator checkingVoiceManagerRoutine;

    void Awake()
    {
        CheckVoiceManager();
        Messenger.Subscribe(EventId.SettingsSubmited, OnSettingsSubmited);
        Messenger.Subscribe(EventId.BattleSettingsSubmited, OnSettingsSubmited);
        Messenger.Subscribe(EventId.OnLanguageChange, OnSettingsSubmited);
    }

    void OnDestroy()
    {
        Messenger.Unsubscribe(EventId.SettingsSubmited, OnSettingsSubmited);
        Messenger.Unsubscribe(EventId.BattleSettingsSubmited, OnSettingsSubmited);
        Messenger.Unsubscribe(EventId.OnLanguageChange, OnSettingsSubmited);
    }

    private void OnSettingsSubmited(EventId id, EventInfo ei)
    {
        CheckVoiceManager();
    }

    private string GetSuitablePrefabPath()
    {
        var path = GameManager.CurrentResourcesFolder + "/VoiceManagers/VoiceManager";

        path += GameData.IsHangarScene ? "Hangar" : "Battle";
        path += ProfileInfo.IsBattleTutorial && !GameData.IsHangarScene ? "Tutorial" : string.Empty;
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
        if (GameManager.CurrentMap == GameManager.MapId.LoadingScene)
            yield break;

        while (!Localizer.Loaded)
            yield return null;

        foreach (VoiceManager voiceManager in FindObjectsOfType<VoiceManager>())
            Destroy(voiceManager.gameObject);

        Resources.UnloadUnusedAssets();

        yield return new WaitForEndOfFrame();

        if (!ProfileInfo.isVoiceDisabled && ProfileInfo.IsVoiceDisablingAvailable)
        {
            string path = GetSuitablePrefabPath();

            VoiceManager voiceManagerPrefab = Resources.Load<VoiceManager>(path);

            if (voiceManagerPrefab)
                Instantiate(voiceManagerPrefab);
            else
                Debug.LogErrorFormat("Can't instantiate VoiceManager prefab at path '{0}'", path);
        }
    }
}
