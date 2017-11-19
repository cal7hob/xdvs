using UnityEngine;
using System.Collections;
using System;

public class VkMessageHandler : MonoBehaviour 
{
    public event Action<string> AuthorizationFinished;
    public event Action AuthorizationFailed;
    public event Action TokenUpdated;
    public event Action TokenExpired;
	public event Action InitializationFinished;

    public static VkMessageHandler Instance
    {
        get
        {
            if(instance == null)
            {
                Initialize();
            }
            return instance;
        }

    }
    private static VkMessageHandler instance;
    public static void Initialize()
    {
        if(instance == null)
        {
            GameObject container = new GameObject("VkMessageHandler");
            DontDestroyOnLoad(container.gameObject);
            instance = container.AddComponent<VkMessageHandler>();
        }
    }
    
    public void OnAuthorizationFinished(string authorizationResult)
    {
        Debug.LogError ("authorizationResult: "+authorizationResult);
        if (AuthorizationFinished != null) 
        {
            AuthorizationFinished(authorizationResult);
        }
    }
    public void OnAuthorizationFailed(string message)
    {
        Debug.LogError ("AuthorizationFailed");
        if (AuthorizationFailed != null) 
        {
            AuthorizationFailed();
        }
    }
    public void OnTokenUpdated(string message)
    {
        Debug.LogError ("TokenUpdated message: " + message);
        if (TokenUpdated != null) 
        {
            TokenUpdated();
        }
    }
    public void OnTokenExpired(string message)
    {
        Debug.LogError ("TokenExpired message: " + message);
        if (TokenExpired != null) 
        {
            TokenExpired();
        }
    }
	public void OnInitializationFinished(string message)
	{
		Debug.LogError ("InitializationFinished message: " + message);
		if (InitializationFinished != null) 
		{
			InitializationFinished();
		}
	}
}
