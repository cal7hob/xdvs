using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class LinkSuggestion : MonoBehaviour
{
    [SerializeField]
    private Transform wrapper;
    [SerializeField]
    private SettingsLoginItemView loginItemViewPrefab;
    [SerializeField]
    private SettingsLogoutItemView logoutItemViewPrefab;
    [SerializeField]
    private SettingsSocialGroupItemView groupItemViewPrefab;
    [SerializeField]
    private UniAlignerBase socialButtonsAligner;
    [SerializeField]
    private UniAlignerBase groupsAligner;
    [SerializeField]
    private Renderer btnOpenGameAccount;

    private List<SettingsLoginItemView> loginItemViews = new List<SettingsLoginItemView>();
    private List<SettingsSocialGroupItemView> groupItemViews = new List<SettingsSocialGroupItemView>();
    private SettingsLogoutItemView logoutItemView;

    public float spaceBetweenItems = 0f;
   
    public void Start()
    {
        Initialize();
        Dispatcher.Subscribe(EventId.OnLanguageChange, LanguageChangeHandler);
    }

    private void LanguageChangeHandler(EventId eventId, EventInfo eventInfo)
    {
        Initialize();
    }

    private void OnDestroy()
    {
        UnsubcribeToLoginItemsEvents();
        Dispatcher.Unsubscribe(EventId.OnLanguageChange, LanguageChangeHandler);
    }

    private void DestroyOldItems()
    {
        socialButtonsAligner.Clear();
        groupsAligner.Clear();

        foreach (SettingsLoginItemView current in loginItemViews)
        {
            Destroy(current.gameObject);
        }
        loginItemViews.Clear();
        foreach (SettingsSocialGroupItemView current in groupItemViews)
        {
            Destroy(current.gameObject);
        }
        groupItemViews.Clear();
        if (logoutItemView != null)
        {
            Destroy(logoutItemView.gameObject);
            logoutItemView = null;
        }
    }

    private void Initialize()
    {
        DestroyOldItems();
        var alreadyLoggedInService = SocialSettings.AvailableSocialServices().FirstOrDefault(service => service.IsLoggedIn);
        btnOpenGameAccount.gameObject.SetActive(false);
        if (null != alreadyLoggedInService)
        {
            logoutItemView = CreateLogoutItemView(alreadyLoggedInService);
            foreach (SocialNetworkGroup socialGroup in alreadyLoggedInService.AllSocialGroups)
            {
                var socialGroupItemView = CreateSocialGroupItemView(socialGroup);
                groupItemViews.Add(socialGroupItemView);
            }
        }
        else
        {
            btnOpenGameAccount.gameObject.SetActive(true);
            socialButtonsAligner.AddItem(btnOpenGameAccount, spaceBetweenItems, spaceBetweenItems, 0);
            foreach (var current in SocialSettings.AvailableSocialServices())
            {
                bool languageRussian = Localizer.Language == Localizer.LocalizationLanguage.Russian;
                bool socialRussian = current.SocialNetworkInfo.Platform == SocialPlatform.Vkontakte
                                     || current.SocialNetworkInfo.Platform == SocialPlatform.Odnoklassniki;
                if (socialRussian && !languageRussian)
                    continue;
                var item = CreateLoginItemView(current);
                loginItemViews.Add(item);
            }
        }
        SubcribeToLoginItemsEvents();
        socialButtonsAligner.Align();
        if (groupsAligner != null) groupsAligner.Align();
    }
    
    private void SubcribeToLoginItemsEvents()
    {
        foreach (var settingsLoginItemView in loginItemViews)
        {
            settingsLoginItemView.LoginSucceed += Initialize;
        }
        if (logoutItemView != null) logoutItemView.LogoutSucceed += Initialize;
    }

    private void UnsubcribeToLoginItemsEvents()
    {
        foreach (var settingsLoginItemView in loginItemViews)
        {
            settingsLoginItemView.LoginSucceed -= Initialize;
        }
        if (logoutItemView != null) logoutItemView.LogoutSucceed -= Initialize;
    }

    private SettingsLoginItemView CreateLoginItemView(ISocialService socialService)
    {
        var settingsLoginItemView = Instantiate(loginItemViewPrefab);
        settingsLoginItemView.transform.parent = socialButtonsAligner.transform;
        settingsLoginItemView.transform.localPosition = Vector3.zero;
        settingsLoginItemView.Initialize(socialService);
        socialButtonsAligner.AddItem(settingsLoginItemView.transform.GetComponent<Renderer>(), 0, spaceBetweenItems);

        return settingsLoginItemView;
    }
    private SettingsLogoutItemView CreateLogoutItemView(ISocialService socialService)
    {
        var settingsLogoutItemView = Instantiate(logoutItemViewPrefab);
        if (groupsAligner != null)
        {
            settingsLogoutItemView.transform.parent = groupsAligner.transform;
            settingsLogoutItemView.transform.localPosition = Vector3.zero;
            settingsLogoutItemView.Initialize(socialService);
            groupsAligner.AddItem(settingsLogoutItemView.transform.GetComponent<Renderer>(), socialButtonsAligner.GetItemsList().Count == 0 ? spaceBetweenItems : 0, spaceBetweenItems);
        }
        return settingsLogoutItemView;
    }
    private SettingsSocialGroupItemView CreateSocialGroupItemView(SocialNetworkGroup socialGroup)
    {
        var socialGroupItemView = Instantiate(groupItemViewPrefab);
        if (groupsAligner != null)
        {
            socialGroupItemView.transform.parent = groupsAligner.transform;
            socialGroupItemView.transform.localPosition = Vector3.zero;
            socialGroupItemView.Initialize(socialGroup);
            groupsAligner.AddItem(socialGroupItemView.transform.GetComponent<Renderer>(), socialButtonsAligner.GetItemsList().Count == 0 ? spaceBetweenItems : 0, spaceBetweenItems);
        }
        return socialGroupItemView;
    }

    private void OnBtnOpenGameAccountPageClick(tk2dUIItem btn)
    {
        GUIPager.SetActivePage("GameAccount");
    }

    private void Skip()
    {
        GUIPager.SetActivePage("MainMenu");
    }
}
