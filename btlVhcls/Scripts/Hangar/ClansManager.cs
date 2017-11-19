using System;
using System.Collections.Generic;
using Tanks.Models;
using UnityEngine;
using JSONObject = System.Collections.Generic.Dictionary<string, object>;

enum ClanPage
{
    Info = 0,
    Search = 1,
    Statistics = 2,
    Contribution = 4
}

public class ClansManager : MonoBehaviour
{
    private static Dictionary<ClanPage, string> clansWebPagesRoutes = new Dictionary<ClanPage, string>
    {
        { ClanPage.Info, "/public/profile/myClan" },
        { ClanPage.Search, "/public/profile/searchClans" },
        { ClanPage.Statistics, Http.Manager.ROUTE_PROFILE },
        { ClanPage.Contribution, "/public/profile/contribution" }
    };

    public static ClansManager Instance { get; private set; }

    private bool isSiteOpened = false;

    private void Awake()
    {
        Instance = this;
        Dispatcher.Subscribe(EventId.AfterHangarInit, OnProfileInfoLoadedFromServer);
        Dispatcher.Subscribe(EventId.ProfileInfoLoadedFromServer, OnProfileInfoLoadedFromServer);
    }

    private void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.AfterHangarInit, OnProfileInfoLoadedFromServer);
        Dispatcher.Unsubscribe(EventId.ProfileInfoLoadedFromServer, OnProfileInfoLoadedFromServer);
        Instance = null;
    }

    private void OnProfileInfoLoadedFromServer(EventId id, EventInfo info)
    {
        ProfileInfo.Clan = ClanInfoFromDict(ProfileInfo.clanInfoDict);
    }

    public Clan ClanInfoFromDict(JSONObject clanInfoDict)
    {
        return clanInfoDict.Count > 0 ? Clan.Create(new JsonPrefs(clanInfoDict)) : null;
    }

    private static string GetURL(ClanPage page, Clan clan = null)
    {
        var clanPart = clan == null ? "" : "/" + Uri.EscapeUriString(clan.Name);

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

    public void OpenAccountManagementWebPage(tk2dUIItem btn)
    {
        OpenAccountManagementWebPageStatic();
    }

    public static void OpenAccountManagementWebPageStatic()
    {
        if (!Instance)
            return;
        Instance.isSiteOpened = true;
        Http.Manager.OpenURL(GetURL(ClanPage.Statistics));
    }

    void OnApplicationFocus(bool focused)
    {
        if (focused && isSiteOpened) {
            isSiteOpened = false;
            ProfileInfo.SaveToServer ();
        }
    }
}
