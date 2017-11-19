using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class LinkAccountPage : MonoBehaviour
{
    [SerializeField] private Transform wrapper;
    [SerializeField] private LinkAccountPageLoginItem loginItemViewPrefab;
    [SerializeField] private LinkAccountPageLogoutItem logoutItemViewPrefab;
    [SerializeField] private LinkAccountPageGroupItem groupItemViewPrefab;
    [SerializeField] private UniAlignerBase socialButtonsAligner;
    [SerializeField] private UniAlignerBase groupsAligner;
    [SerializeField] private Renderer btnOpenGameAccount;
    [SerializeField] private GameObject goInstruction;
    [SerializeField] private tk2dTextMesh rewardMoneyValue;
    [SerializeField] private tk2dSlicedSprite rewardMoneyIcon;
    [SerializeField] private float spaceBetweenItems = 0f;

    [SerializeField] private ActivatedUpDownButton loginStateSpecificObjects;//Вкл/выкл объекты для незалогиненного/залогиненного состояния

    private List<LinkAccountPageLoginItem> loginItemViews = new List<LinkAccountPageLoginItem>();
    private List<LinkAccountPageGroupItem> groupItemViews = new List<LinkAccountPageGroupItem>();
    private LinkAccountPageLogoutItem logoutItemView;

    public void Start()
    {
        Initialize();
        Dispatcher.Subscribe(EventId.OnLanguageChange,LanguageChangeHandler);
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

    private void PopulateReward()
    {
        if (rewardMoneyValue)
        {
            rewardMoneyValue.text = ProfileInfo.SocialActivationReward.LocalizedValue;
            rewardMoneyValue.SetMoneySpecificColorIfCan(ProfileInfo.SocialActivationReward);
        }

        if (rewardMoneyIcon)
        {
            rewardMoneyIcon.SetSprite(ProfileInfo.SocialActivationReward.currency.ToString().ToLower());
        }
    }

    private void DestroyOldItems()
	{
        socialButtonsAligner.Clear();
        groupsAligner.Clear();

        foreach (LinkAccountPageLoginItem current in loginItemViews)
		{
			Destroy(current.gameObject);
		}
		loginItemViews.Clear();
        foreach (LinkAccountPageGroupItem current in groupItemViews)
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
	    PopulateReward();
	    var alreadyLoggedInService = SocialSettings.AvailableSocialServices().FirstOrDefault(service => service.IsLoggedIn);
        btnOpenGameAccount.gameObject.SetActive(false);
        if (loginStateSpecificObjects)
            loginStateSpecificObjects.Activated = alreadyLoggedInService != null;

        if (alreadyLoggedInService != null)
	    {
            logoutItemView = CreateLogoutItemView(alreadyLoggedInService);
            foreach (SocialNetworkGroup socialGroup in alreadyLoggedInService.AllSocialGroups)
            {
                var socialGroupItemView = CreateSocialGroupItemView(socialGroup);
                groupItemViews.Add(socialGroupItemView);
            }
            alreadyLoggedInService.GroupJoined += delegate(string id)
            {
                if (id == (string)GameData.socialGroups["fb"] || id == (string)GameData.socialGroups["vk"])
                    DisableInstruction();
            };
	    }
        else
        {
            btnOpenGameAccount.gameObject.SetActive(true);
            socialButtonsAligner.AddItem(btnOpenGameAccount, 0, 0, 0);
            foreach (var current in SocialSettings.AvailableSocialServices())
            {
                bool languageRussian = Localizer.Language == Localizer.LocalizationLanguage.Russian;
                bool socialRussian = current.Platform == SocialPlatform.Vkontakte
                                     || current.Platform == SocialPlatform.Odnoklassniki;
                if(socialRussian && !languageRussian)
                    continue;
			    var item = CreateLoginItemView(current);
			    loginItemViews.Add(item);
		    }
            if(SocialSettings.AvailableSocialServices().Count() == 0)
                DisableInstruction();
        }

        if (ProfileInfo.socialActivity.Contains(SocialAction.activated) || ProfileInfo.isSocialActivated)
            DisableInstruction();

        SubcribeToLoginItemsEvents();
        socialButtonsAligner.Align();
	    if (groupsAligner)
            groupsAligner.Align();
    }

    private void DisableInstruction()
    {
        if (goInstruction)
            goInstruction.SetActive(false);
    }

    private void SubcribeToLoginItemsEvents()
    {
        foreach (var settingsLoginItemView in loginItemViews)
            settingsLoginItemView.LoginSucceed += Initialize;

        if (logoutItemView)
            logoutItemView.LogoutSucceed += Initialize;
    }

    private void UnsubcribeToLoginItemsEvents()
    {
        foreach (var settingsLoginItemView in loginItemViews)
            settingsLoginItemView.LoginSucceed -= Initialize;

        if (logoutItemView)
            logoutItemView.LogoutSucceed -= Initialize;
    }

    private LinkAccountPageLoginItem CreateLoginItemView(ISocialService socialService)
	{
		var settingsLoginItemView = Instantiate(loginItemViewPrefab);
		settingsLoginItemView.transform.parent = socialButtonsAligner.transform;
		settingsLoginItemView.transform.localPosition = Vector3.zero;
		settingsLoginItemView.Initialize(socialService);
        socialButtonsAligner.AddItem(settingsLoginItemView.transform.GetComponent<Renderer>(), spaceBetweenItems, 0);//email button already exists at 0 position

        return settingsLoginItemView;
	}

    private LinkAccountPageLogoutItem CreateLogoutItemView(ISocialService socialService)
    {
        var settingsLogoutItemView = Instantiate(logoutItemViewPrefab);
        if (groupsAligner)
        {
            settingsLogoutItemView.transform.parent = groupsAligner.transform;
            settingsLogoutItemView.transform.localPosition = Vector3.zero;
            settingsLogoutItemView.Initialize(socialService);
            groupsAligner.AddItem(settingsLogoutItemView.transform.GetComponent<Renderer>(), groupsAligner.GetItemsList().Count == 0 ? 0 : spaceBetweenItems, 0);
        }
        return settingsLogoutItemView;
    }

    private LinkAccountPageGroupItem CreateSocialGroupItemView(SocialNetworkGroup socialGroup)
    {
        var socialGroupItemView = Instantiate(groupItemViewPrefab);
        if (groupsAligner != null)
        {
            socialGroupItemView.transform.parent = groupsAligner.transform;
            socialGroupItemView.transform.localPosition = Vector3.zero;
            socialGroupItemView.Initialize(socialGroup);
            groupsAligner.AddItem(socialGroupItemView.transform.GetComponent<Renderer>(), groupsAligner.GetItemsList().Count == 0 ? 0 : spaceBetweenItems, 0);
        }
        return socialGroupItemView;
    }

    private void OnBtnOpenGameAccountPageClick(tk2dUIItem btn)
    {
        GUIPager.SetActivePage("GameAccount",true);
    }

    public void OpenPage(tk2dUIItem btn)
    {
        OpenPageStatic();
    }

    public static void OpenPageStatic()
    {
        GUIPager.SetActivePage("LinkAccountPage", false);
    }
}
