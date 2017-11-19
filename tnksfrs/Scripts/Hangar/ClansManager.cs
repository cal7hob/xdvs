using System.Collections.Generic;
using Tanks.Models;
using UnityEngine;
using JSONObject = System.Collections.Generic.Dictionary<string, object>;

enum ClanPage
{
    Info = 0,
    Search = 1,
    Statistics = 2,
    Contribution = 4,
    Support = 5,
}

public class ClansManager : MonoBehaviour
{
    Dictionary<ClanPage, string> clansWebPagesRoutes = new Dictionary<ClanPage, string> {
        { ClanPage.Info, "/public/profile/myClan" },
        { ClanPage.Search, "/public/profile/searchClans" },
        { ClanPage.Statistics, Http.Manager.ROUTE_PROFILE },
        { ClanPage.Support, "/public/profile/feedback"},
        { ClanPage.Contribution, "/public/profile/contribution" } };

    public static ClansManager Instance { get; private set; }

    private bool isSiteOpened = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        Dispatcher.Subscribe(EventId.AfterHangarInit, OnProfileInfoLoadedFromServer);
        Dispatcher.Subscribe(EventId.ProfileInfoLoadedFromServer, OnProfileInfoLoadedFromServer);
    }

    private void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.AfterHangarInit, OnProfileInfoLoadedFromServer);
        Dispatcher.Unsubscribe(EventId.ProfileInfoLoadedFromServer, OnProfileInfoLoadedFromServer);

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnProfileInfoLoadedFromServer(EventId id, EventInfo info)
    {
        ProfileInfo.Clan = ClanInfoFromDict(ProfileInfo.clanInfoDict);
    }

    public Clan ClanInfoFromDict(JSONObject clanInfoDict)
    {
        return clanInfoDict.Count > 0 ? Clan.Create(new JsonPrefs(clanInfoDict)) : null;
    }

    private string GetURL(ClanPage page, Clan clan = null)
    {
        var clanPart = clan == null ? "" : "/" + WWW.EscapeURL(clan.Name);

        return clansWebPagesRoutes[page] + clanPart;
    }

    public void OpenParticularClanWebPage(Clan clan)
    {
        isSiteOpened = true;
        Http.Manager.OpenURL(GetURL(ClanPage.Search, clan));
    }

    public void OpenClansWebPage()
    {
        if (ProfileInfo.Clan == null) {
            isSiteOpened = true;
            Http.Manager.OpenURL (GetURL (ClanPage.Search));
        }
        else {
            isSiteOpened = true;
            Http.Manager.OpenURL (GetURL (ClanPage.Info));
        }
    }

    public void OpenAccountManagementWebPage()
    {
        isSiteOpened = true;
        Http.Manager.OpenURL(GetURL(ClanPage.Statistics));
    }

    public void OpenAccountSupportPage()
    {
        isSiteOpened = true;
        Http.Manager.OpenURL(GetURL(ClanPage.Support));
    }

	void OnApplicationFocus(bool focused)
    {
        if (focused && isSiteOpened) {
            isSiteOpened = false;
            ProfileInfo.SaveToServer ();
        }
    }
}
