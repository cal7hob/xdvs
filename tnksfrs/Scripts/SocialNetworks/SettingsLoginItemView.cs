using System;
using UnityEngine;

public sealed class SettingsLoginItemView : MonoBehaviour
{
    public event Action LoginSucceed = delegate{};

	[SerializeField]
	private GameObject loginGroup;

    private ISocialService socialService;

	private bool loginRequestSended;

    /*public float LoginItemHeight {
        get
        {
            return (GetComponent<tk2dUILayout>().GetMaxBounds()
             - GetComponent<tk2dUILayout>().GetMinBounds()).y;
        }
    }*/

    /*public float LoginItemWidth
    {
        get
        {
            return (loginGroup.GetComponent<tk2dUILayout>().GetMaxBounds()
             - loginGroup.GetComponent<tk2dUILayout>().GetMinBounds()).x;
        }
    }*/

    private void Awake()
	{
	}

	private void OnDestroy()
	{
		UnsubcribeFromSocialConnectionEvents();
	}

	public void Initialize(ISocialService socialService)
	{
        this.socialService = socialService;
		loginRequestSended = false;

		SubcribeToSocialConnectionEvents();
		Refresh();
	}

	private void Refresh()
	{
		loginGroup.SetActive(!socialService.IsLoggedIn);
		//SetRewards();
	}

	private void SubcribeToSocialConnectionEvents()
	{
		socialService.LoginSucceed += LoginSucceeded;
        socialService.LoginFail += LoginFailed;
	}

	private void UnsubcribeFromSocialConnectionEvents()
	{
        socialService.LoginSucceed -= LoginSucceeded;
        socialService.LoginFail -= LoginFailed;
	}

    //private void SetRewards()
    //{
    //    if (ProfileInfo.isSocialActivated)
    //    {
    //        loginRewardRoot.SetActive(false);
    //    }
    //    else
    //    {
    //        loginRewardRoot.SetActive(true);
    //        loginRewardLabel.text = "+" + ProfileInfo.SocialActivationReward.value;
    //    }
    //}

	private void LoginSucceeded(ISocialService service)
	{
        Refresh();
        XdevsSplashScreen.SetActiveWaitingIndicator(false);
		loginRequestSended = false;
	    LoginSucceed();
	}

	private void LoginFailed()
    {
        XdevsSplashScreen.SetActiveWaitingIndicator(false);
		loginRequestSended = false;
	}

	private void LoginButtonClick()
	{
		if (!loginRequestSended)
        {
            XdevsSplashScreen.SetActiveWaitingIndicator(true);
			loginRequestSended = true;
            socialService.Login();
		}
	}
}
