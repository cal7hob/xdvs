using System;
using UnityEngine;
#if UNITY_ANDROID

public class VkCheckUserInstallListener : AndroidJavaProxy
{
    public VkCheckUserInstallListener(Action<bool> onUserState)
        : base("com.xdevs.vk.sdk.IVkCheckUserInstallListener")
    {
        OnUserState = onUserState;
    }

    private Action<bool> OnUserState;
    public void onUserState(bool isVkUser)
    {
        OnUserState(isVkUser);
    }
}
#endif