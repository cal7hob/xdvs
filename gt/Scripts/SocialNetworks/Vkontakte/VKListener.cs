using UnityEngine;
#if UNITY_ANDROID
public class VKListener 
    : AndroidJavaProxy 
{
    public VKListener() : base("com.xdevs.vk.sdk.IListener") { }

    public delegate void OnResultDelegate();
    public delegate void OnAccessDeniedDelegate( );
    public OnResultDelegate OnResult;
    public OnAccessDeniedDelegate OnAccessDenied;

    public void onAccessDenied(AndroidJavaObject authorizationError)
    {
        if (null != OnAccessDenied)
            OnAccessDenied();
    }

    public void onReceiveNewToken(AndroidJavaObject newToken)
    {
        if (null != OnResult)
            OnResult();
    }
    public void onCaptchaError(AndroidJavaObject captchaError)
    {
    }

    public void onTokenExpired(AndroidJavaObject expiredToken)
    {
    }

    public void onAcceptUserToken(AndroidJavaObject token)
    {
    }

    public void onRenewAccessToken(AndroidJavaObject token)
    {
    }
}
#endif