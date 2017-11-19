using System;
using UnityEngine;
#if UNITY_ANDROID
public class VKShareDialogListener : AndroidJavaProxy
{
    public VKShareDialogListener()
        : base("com.vk.sdk.dialogs.VKShareDialog$VKShareDialogListener") { }

    public Action<int> OnComplete;
    public Action OnCancel;

    public void onVkShareComplete(int id)
    {
        if (null != OnComplete)
        {
            OnComplete(id);
        }
    }
    public void onVkShareCancel()
    {
        if (null != OnCancel)
        {
            OnCancel();
        }
    } 
}
#endif