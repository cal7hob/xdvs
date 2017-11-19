using UnityEngine;
using System;
using System.Collections;

public class GamePromoButton : MonoBehaviour
{
    public tk2dBaseSprite spritePromo;
    public tk2dTextMesh lblGameName;
    private bool isInited = false;
    public tk2dBaseSprite sprite { get; private set; }

    public float VerticalSize
    {
        get { return verticalSize; }
    }

    public float HorizontalSize
    {
        get { return horizontalSize; }
    }

    private GamePromo promo;
    private static Texture2D whiteSquare;
    [SerializeField]
    private tk2dCameraAnchor controlledAnchor;

    [SerializeField]
    private float verticalSize;
    [SerializeField]
    private float horizontalSize;

    public void SetPromo(GamePromo promo)
    {
        this.promo = promo;
        lblGameName.text = promo.name;
    }

    public void OnEnable()
    {
        spritePromo.SetSprite(promo.id + "Promo");
    }

    public void SetTexture(string texName)
    {
        Init();
        //string path = string.Format("Common/Textures/{0}/{1}", tk2dSystem.CurrentPlatform, texName);
        //Texture2D texToReplace = (Texture2D)Resources.Load(path);

        //if (texToReplace == null)
        //{
        //    return;
        //}
        //sprFromTex.texture = texToReplace;
        //sprFromTex.ForceBuild();
        //sprFromTex.gameObject.SetActive(true);
    }

    private void Init()
    {
        if (!isInited)
        {
            if (controlledAnchor && controlledAnchor.AnchorCamera == null && HangarController.Instance.GuiCamera != null)
                controlledAnchor.AnchorCamera = HangarController.Instance.GuiCamera;

            //if (whiteSquare == null)
            //    whiteSquare = (Texture2D)Resources.Load("Common/white");
            //sprite = sprFromTex.GetComponent<tk2dBaseSprite>();
            sprite.enabled = true;
            isInited = true;
        }
    }
#if UNITY_IOS || UNITY_STANDALONE_OSX
    [System.Runtime.InteropServices.DllImport ("__Internal")]
    private static extern bool XDevsCanOpenUrl (string url);
#endif
    private void OnDisable()
    {
        //    sprFromTex.texture = whiteSquare;
        //    sprFromTex.ForceBuild();
        //    sprFromTex.gameObject.SetActive(false);
        //    Resources.UnloadUnusedAssets();
    }

    public void OnClick()
    {
        openApp();
    }

    private void openApp()
    {
#pragma warning disable 162
        #region UnityEditor
#if UNITY_EDITOR
        Application.OpenURL(string.Format("https://play.google.com/store/apps/details?id={0}", GameData.androidBundleIds[promo.id]));
        return;
#endif
        #endregion
        #region Android
#if UNITY_ANDROID
        AndroidJavaClass up = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject ca = up.GetStatic<AndroidJavaObject>("currentActivity");
        AndroidJavaObject packageManager = ca.Call<AndroidJavaObject>("getPackageManager");
        AndroidJavaObject launchIntent = null;
        string bundle = GameData.androidBundleIds[GameData.IsGame(Game.AmazonBuild) ? promo.id | Game.AmazonBuild : promo.id];

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
        var url = promo.iosScheme + "://";
        if (XDevsCanOpenUrl(url))
        {
            Application.OpenURL(url);
        }
        else
        {
            Application.OpenURL("itms-apps://itunes.apple.com/app/id"+promo.iosId);
        }
#endif
        #endregion
        #region WSA

#if UNITY_WSA && UNITY_WSA_10_0
        Application.OpenURL("ms-windows-store://pdp/?ProductId=" + promo.wsaPdpId);
#elif UNITY_WSA && UNITY_WP_8_1
        Application.OpenURL("ms-windows-store://pdp/?PhoneAppId="+promo.wsaPhoneAppId);
#elif UNITY_WSA && UNITY_WSA_8_1
        Application.OpenURL("ms-windows-store://pdp/?AppId="+promo.wsaAppId);
#endif
        #endregion
        #region WEBGL
#if UNITY_WEBGL
        WebTools.OpenURL(promo.webUrls[SocialSettings.Platform]);
#endif
        #endregion
        #region MAC
#if UNITY_STANDALONE_OSX
        Application.OpenURL(string.Format("https://geo.itunes.apple.com/app/id{0}?mt=12", promo.macId));
#endif
        #endregion
#pragma warning restore 162
    }
}
