using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class VKAuth : MonoBehaviour {

    static VKAuth instance;
    public List<GameObject> ITButtons;
    public List<GameObject> FTButtons;

    private Dictionary<Interface, List<GameObject>> ButtonsSets = new Dictionary<Interface, List<GameObject>>();

    public void EnableButtons(bool enable)
    {
        foreach (var button in ButtonsSets[GameData.CurInterface])
        {
            button.SetActive(enable);
        }
    }
#if UNITY_ANDROID
	public static VKAuth Instance {
		get {
			return instance;
		}
	}

	void Awake()
    {
		instance = this;
	}

    void Start()
    {
        XdevsSplashScreen.SetActiveWaitingIndicator(false);
        //if (!VKMobile.isLoggedIn())
        //    EnableButtons(true);
    }

	private void LoginVK()
	{
	    //VKAndroid.Instance.Login();
	}

	private void NotNow()
	{
        //VKMobile.EnterGuestMode();
	}

	void OnApplicationFocus(bool status)
	{
		//if(status)
        //    EnableButtons(!VKMobile.isLoggedIn());
	}

    protected bool Button(string label)
    {
        return GUILayout.Button(
            label,
            GUILayout.MinHeight(24),
            GUILayout.MaxWidth(200)
        );
    }

	/*void OnGUI()
	{
	    if (!GameData.IsGame(Game.VKBuild))
	        return;
        if (Button("CheckUserInstall"))
        {
            //VKSdk.Che
        }

		if(Button("FRIENDS")){
			var request = VKApi.friends().getOnline(new VKParameters());
		    var listener = new VKRequestListener();
		    listener.OnComplete += delegate(string response)
		    {
		        Debug.LogError("ONCOMPLETE "+response);
		    };
		    request.executeWithListener(listener); //new VKRequestListener());
		}
        if(Button("CurrentUserInfo"))
        {
            var request = VKApi.users().get(new VKParameters()
            {
                {"fields","photo_50,city,verified"}
            });
            var listener = new VKRequestListener();
            listener.OnComplete += delegate(string response)
            {
                Debug.LogError("CurrentUserInfo ONCOMPLETE " + response);
            };
            request.executeWithListener(listener);
        }
        if(Button("FriendsForInvite"))
        {
            //Debug.LogError("TOKEN "+VKSdk.accessToken.accessToken);
            StartCoroutine(GetFriendForInviteList());
	    }
        if(Button("LOG OUT"))
        {
            VKSdk.logout();
        }

	}
    IEnumerator GetFriendForInviteList()
    {
        string url = string.Format(
            "https://api.vk.com/method/apps.getFriendsList?user_ids=ID&fields=photo_50&extended=1&count=5000&offset=0&type=invite&v=5.28&access_token=TOKEN")
            .Replace("ID", VKSdk.getAccessToken().UserId)
            .Replace("TOKEN", VKSdk.getAccessToken().TokenString);
        WWW loader = new WWW(url);
        yield return loader;
	    Debug.LogError(" "+loader.text);
        }
     */
#endif
}


