using System.Collections;
using UnityEngine;
using AssetBundles;

public class VoiceSwitcher : MonoBehaviour
{
    private IEnumerator checkingVoiceManagerRoutine;

    void Awake()
    {
        CheckVoiceManager();
        Dispatcher.Subscribe(EventId.SettingsSubmited, OnSettingsSubmited);
        Dispatcher.Subscribe(EventId.BattleSettingsSubmited, OnSettingsSubmited);
        Dispatcher.Subscribe(EventId.OnLanguageChange, OnSettingsSubmited);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.SettingsSubmited, OnSettingsSubmited);
        Dispatcher.Unsubscribe(EventId.BattleSettingsSubmited, OnSettingsSubmited);
        Dispatcher.Unsubscribe(EventId.OnLanguageChange, OnSettingsSubmited);
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
            string bundle = string.Format("{0}/voices", GameManager.CurrentResourcesFolder).ToLower ();
            string vManager = string.Format ("VoiceManager{0}{1}{2}",
                GameData.IsHangarScene ? "Hangar" : "Battle",
                ProfileInfo.IsBattleTutorial && !GameData.IsHangarScene ? "Tutorial" : string.Empty,
                GameData.InterfaceShortName
            );

            AssetBundleLoadAssetOperation request = AssetBundleManager.LoadAssetAsync(bundle, vManager, typeof(GameObject));
            if (request == null)
                yield break;
            yield return StartCoroutine(request);

            // Get the asset.
            GameObject voiceManagerPrefab = request.GetAsset<GameObject>();

            //string path = GetSuitablePrefabPath();
            //VoiceManager voiceManagerPrefab = Resources.Load<VoiceManager>(path);

            if (voiceManagerPrefab)
            {
                Instantiate(voiceManagerPrefab);
            }
            else
            {
                //Debug.LogErrorFormat("Can't instantiate VoiceManager prefab at path '{0}'", path);
                Debug.LogErrorFormat("Can't instantiate VoiceManager prefab from bundle '{0}' with name '{1}'", bundle, vManager);
            }
        }
    }
}
