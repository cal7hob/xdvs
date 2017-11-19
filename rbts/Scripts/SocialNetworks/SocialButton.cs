using UnityEngine;
using System.Collections;

public class SocialButton : ActivatedUpDownButton
{
    public tk2dBaseSprite socialNetworkLogo;
    public GameObject socialNotLoggedInLogo;
    void Start()
    {
        ChangeLogo();
        if (SocialSettings.Instance != null)
        {
            SocialSettings.Instance.LoginSucceed += ChangeLogo;
            SocialSettings.Instance.LogoutSucceed += ChangeLogo;
        }
    }

    private void OnDestroy()
    {
        if (SocialSettings.Instance != null)
        {
            SocialSettings.Instance.LoginSucceed -= ChangeLogo;
            SocialSettings.Instance.LogoutSucceed -= ChangeLogo;
        }
    }

    private void ChangeLogo()
    {
        if (socialNetworkLogo != null)
        {
            socialNetworkLogo.gameObject.SetActive(true);
            if (socialNotLoggedInLogo != null) socialNotLoggedInLogo.SetActive(false);
            //var testPlatform = SocialPlatform.Facebook;

            //switch (testPlatform)
            switch (SocialSettings.Platform)
            {
                case SocialPlatform.Facebook:
                    socialNetworkLogo.SetSprite("Facebook");
#if UNITY_WSA_10_0
                    Activated = false;
#endif
                    break;
                case SocialPlatform.Odnoklassniki:
                    socialNetworkLogo.SetSprite("odnoklassniki");
                    break;
                case SocialPlatform.Mail:
                    socialNetworkLogo.SetSprite("moimir");
                    break;
                case SocialPlatform.Vkontakte:
                    socialNetworkLogo.SetSprite("vk");
                    break;
                case SocialPlatform.Undefined:
                    socialNetworkLogo.SetSprite("social");
#if UNITY_STANDALONE_OSX
                    Activated = false;
#endif
                    if (!GameData.IsHangarScene)
                    {
                        Activated = false;
                    }
                    if (socialNotLoggedInLogo != null)
                    {
                        socialNetworkLogo.gameObject.SetActive(false);
                        socialNotLoggedInLogo.SetActive(true);
                    }
                    break;
            }
        }
    }
}
