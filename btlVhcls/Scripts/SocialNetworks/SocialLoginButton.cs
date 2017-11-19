using UnityEngine;
using System.Collections;

public class SocialLoginButton : MonoBehaviour 
{
    public GameObject activateGO;
    public GameObject loginGO;
    public tk2dTextMesh lActivationRewardMoney;
    public tk2dSlicedSprite moneyIcon;
	// Use this for initialization
	void Start ()
    {
#if UNITY_STANDALONE_OSX
        gameObject.SetActive(false);
#else
        ApplyMode();
	    if (SocialSettings.Instance != null)
	    {
	        SocialSettings.Instance.LoginSucceed += Hide;
	        SocialSettings.Instance.LogoutSucceed += Show;
	    }
	    if(SocialSettings.IsLoggedIn)
            Hide();
#endif
	}

    private void OnDestroy()
    {
        if (SocialSettings.Instance != null)
        {
            SocialSettings.Instance.LoginSucceed -= Hide;
            SocialSettings.Instance.LogoutSucceed -= Show;
        }
    }
    void Show()
    {
        gameObject.SetActive(true);
    }

    void Hide()
    {
        gameObject.SetActive(false);
    }

	void OpenSettingsSocialTab () 
    {
        if (RightPanel.Instance.rightPanel.scrollableArea.IsSwipeScrollingInProgress)
            return;
        LinkAccountPage.OpenPageStatic();
	}
    private void ApplyMode()
    {
        if (loginGO != null) loginGO.SetActive(ProfileInfo.isSocialActivated);
        if (activateGO != null) activateGO.SetActive(!ProfileInfo.isSocialActivated);
        if (lActivationRewardMoney)
        {
            lActivationRewardMoney.text = ProfileInfo.SocialActivationReward.LocalizedValue;
            lActivationRewardMoney.SetMoneySpecificColorIfCan(ProfileInfo.SocialActivationReward);

            if (moneyIcon)
            {
                moneyIcon.SetSprite(ProfileInfo.SocialActivationReward.currency.ToString().ToLower());
                moneyIcon.dimensions = new Vector2(moneyIcon.CurrentSprite.GetBounds().size.x,
                                                   moneyIcon.CurrentSprite.GetBounds().size.y);
            }
        }
    }
}
