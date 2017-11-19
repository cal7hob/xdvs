using System;
using System.Collections.Generic;
using Facebook.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DeepLinks : MonoBehaviour
{
    private bool linkProcessed = false;

#if !UNITY_WSA
    private Dictionary<string,Action> actions = new Dictionary<string, Action>
    {
        {"bank", () => GUIPager.SetActivePage("Bank")},
        {"garage", () => GUIPager.SetActivePage("VehicleShopWindow")},
        {"patterns", () => GUIPager.SetActivePage("PatternShop")},
        {"decals", () => GUIPager.SetActivePage("DecalShop")},
        {"sales", () =>
        {
            if(SpecialOffersPage.IsSaleGoing)
                GUIPager.SetActivePage(
                    pageName:       "SpecialOffersPage",
                    voiceEventId:   (int)VoiceEventKey.OffersEnter);
        }}
    };
#endif

    private void Awake()
    {
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }
    private void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    public void OnActiveSceneChanged(Scene previousScene, Scene newScene)
    {
        if (FB.IsInitialized && GameData.IsHangarScene && !linkProcessed)
        {
            if (!HangarController.Instance.IsInitialized)
            {
                Messenger.Subscribe(EventId.AfterHangarInit, HangarInitHandler, 4);
            }
            else
            {
                GetAppLink();
            }
        }
    }

    private void HangarInitHandler(EventId eventId, EventInfo eventInfo)
    {
        Messenger.Unsubscribe(EventId.AfterHangarInit, HangarInitHandler);
        GetAppLink();
    }

    private void GetAppLink()
    {
#if !UNITY_WSA
        FB.GetAppLink(delegate(IAppLinkResult result)
        {
            linkProcessed = true;
            if (string.IsNullOrEmpty(result.Url)) return;
            var uri = new Uri(result.Url);
            if (actions.ContainsKey(uri.Host))
            {
                actions[uri.Host]();
            }
        });
#endif
    }
}
