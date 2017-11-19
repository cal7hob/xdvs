using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

class SettingsLogoutItemView : MonoBehaviour
{
    public event Action LogoutSucceed = delegate { };
    
    [SerializeField]
    private GameObject logoutGroup;

    [SerializeField]
    private SocialUserInfoView socialUserInfoView;

    private ISocialService socialService;
    
    /*public float LogoutItemHeight
    {
        get
        {
            return (GetComponent<tk2dUILayout>().GetMaxBounds()
               - GetComponent<tk2dUILayout>().GetMinBounds()).y;
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
        SubcribeToSocialConnectionEvents();
        Refresh();
    }

    private void Refresh()
    {
        logoutGroup.SetActive(socialService.IsLoggedIn);
        socialUserInfoView.Initialize(socialService);
    }

    private void SubcribeToSocialConnectionEvents()
    {
        socialService.LogoutSucceed += LogoutSucceeded;
    }

    private void UnsubcribeFromSocialConnectionEvents()
    {
        socialService.LogoutSucceed -= LogoutSucceeded;
    }

    private void LogoutSucceeded()
    {
        Refresh();
        XdevsSplashScreen.SetActiveWaitingIndicator(false);
        LogoutSucceed();
    }

    private void LogoutButtonClick()
    {
        ShowLogoutPopup(delegate(MessageBox.Answer result)
        {
            if (result == MessageBox.Answer.Yes)
            {
                socialService.Logout();
            }
        });
    }
    private void ShowLogoutPopup(Action<MessageBox.Answer> callback)
    {
        string logoutMessage = Localizer.GetText("lblLogoutConfirmText");
        MessageBox.Show(MessageBox.Type.Question, logoutMessage, callback);
    }
}
