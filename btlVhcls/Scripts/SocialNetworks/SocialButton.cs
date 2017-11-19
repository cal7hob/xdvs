using UnityEngine;

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
        if(socialNetworkLogo == null)
        {
            Debug.LogErrorFormat("Social sprite not assigned on object {0}", MiscTools.GetFullTransformName(transform));
            return;
        }

        socialNetworkLogo.gameObject.SetActive(true);
        if (socialNotLoggedInLogo)
            socialNotLoggedInLogo.SetActive(false);

        socialNetworkLogo.SetSprite(SocialSettings.GetCurPlatformSocialNetworkLogo(SocialNetworkIconType.socialButton));

        if(SocialSettings.Platform == SocialPlatform.Undefined)
        {
            #if UNITY_STANDALONE_OSX
                Activated = false;
            #endif
            if (!GameData.IsHangarScene)
            {
                Activated = false;
            }
            if (socialNotLoggedInLogo)
            {
                socialNetworkLogo.gameObject.SetActive(false);
                socialNotLoggedInLogo.SetActive(true);
            }

        }
    }
}
