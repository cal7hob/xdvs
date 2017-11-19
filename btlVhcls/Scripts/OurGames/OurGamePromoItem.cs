using UnityEngine;
using System;
using System.Collections;

public class OurGamePromoItem : MonoBehaviour, IItem
{
    [SerializeField] private tk2dSlicedSprite sizeBg;
    [SerializeField] private SpriteFromRes spriteFromRes;
    [SerializeField] private tk2dTextMesh lblGameName;

    private GamePromo promo;

#if UNITY_IOS || UNITY_STANDALONE_OSX
    [System.Runtime.InteropServices.DllImport ("__Internal")]
    private static extern bool XDevsCanOpenUrl (string url);
#endif

    public void Initialize(object[] parameters)
    {
        promo = (GamePromo)parameters[0];
        gameObject.name = string.Format("{0}", promo.Name);

        UpdateElements();
    }

    public virtual void UpdateElements()
    {
        spriteFromRes.SetTexture(promo.Texture);
        lblGameName.text = promo.Name;
    }

    public void OnClick()
    {
        OpenApp();
    }

    private void OpenApp()
    {
#pragma warning disable 162 // Ureachable code detected
        #region UnityEditor
#if UNITY_EDITOR
        Application.OpenURL(string.Format("https://play.google.com/store/apps/details?id={0}", promo.GoogleBundleId));
        return;
#endif
        #endregion
        #region Android
#if UNITY_ANDROID
        AndroidJavaClass up = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject ca = up.GetStatic<AndroidJavaObject>("currentActivity");
        AndroidJavaObject packageManager = ca.Call<AndroidJavaObject>("getPackageManager");
        AndroidJavaObject launchIntent = null;
        string bundle = GameData.IsGame(Game.AmazonBuild) ? promo.AmazonBundleId : promo.GoogleBundleId;
        
        try
        {
            launchIntent = packageManager.Call<AndroidJavaObject>("getLaunchIntentForPackage", bundle);
            ca.Call("startActivity", launchIntent);
        }
        catch (Exception ex)
        {
            Debug.Log("exception" + ex.Message);
        }
        if (launchIntent != null) return;
        if (GameData.IsGame(Game.AmazonBuild))
        {
            Application.OpenURL("amzn://apps/android?p="+bundle);
        }
        else
        {
            try
            {
                AndroidJavaObject intent = packageManager.Call<AndroidJavaObject>("getLaunchIntentForPackage",
                    "com.android.vending");
                AndroidJavaObject component = new AndroidJavaObject("android.content.ComponentName", "com.android.vending",
                    "com.google.android.finsky.activities.LaunchUrlHandlerActivity");
                intent.Call<AndroidJavaObject>("setComponent", component);
                intent.Call<AndroidJavaObject>("setData",
                    new AndroidJavaClass("android.net.Uri").CallStatic<AndroidJavaObject>("parse",
                        "market://details?id=" + bundle));
                ca.Call("startActivity", intent);
            }
            catch (Exception ex)
            {
                Debug.Log("exception" + ex.Message);
            }
        }
#endif
        #endregion
        #region IOS
#if UNITY_IOS
        var url = promo.IosScheme + "://";
        if (XDevsCanOpenUrl(url))
        {
            Application.OpenURL(url);
        }
        else
        {
            Application.OpenURL("itms-apps://itunes.apple.com/app/id"+promo.IosId);
        }
#endif
        #endregion
        #region WSA

#if UNITY_WSA && UNITY_WSA_10_0
        Application.OpenURL("ms-windows-store://pdp/?ProductId="+promo.WsaPdpId);
#elif UNITY_WSA && UNITY_WP_8_1
        Application.OpenURL("ms-windows-store://pdp/?PhoneAppId="+promo.WsaPhoneAppId);
#elif UNITY_WSA && UNITY_WSA_8_1
        Application.OpenURL("ms-windows-store://pdp/?AppId="+promo.WsaAppId);
#endif
        #endregion
        #region WEBGL
#if UNITY_WEBGL
        WebTools.OpenURL(promo.webUrls[SocialSettings.Platform]);
#endif
        #endregion
        #region MAC
#if UNITY_STANDALONE_OSX
        Application.OpenURL(string.Format("https://geo.itunes.apple.com/app/id{0}?mt=12", promo.MacId));
#endif
        #endregion
#pragma warning restore 162 // Ureachable code detected
    }

    public void DesrtoySelf()
    {
    }

    public Vector2 GetSize()
    {
        return new Vector2(sizeBg.dimensions.x * sizeBg.scale.x, sizeBg.dimensions.y * sizeBg.scale.y);
    }

    public string GetUniqId { get { return promo.Name; } }

    public tk2dUIItem MainUIItem { get { return null; } }

    public Transform MainTransform { get { return transform; } }
}
