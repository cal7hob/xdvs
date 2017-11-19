using System;
using System.Collections.Generic;
using Tanks.Models;
using UnityEngine;

public class PlayerInfo : MonoBehaviour
{
    public Avatar avatar; 
    public GameObject clanInfoBtn;

    private const string PLAYER_KEY = "player";
    private const string EXPERIENCE_KEY = "experience";
    private const string VEHICLES_KEY = "tanks";
    private const string FRIENDSHIP_STATUS_KEY = "isFriend";
    private const string BATTLE_STATS_KEY = "battleStats";
    private const string COUNTRY_KEY = "country";
    private const string REGION_KEY = "region";

    [SerializeField]
    private Player player;

    private int id;
    private int experience;
    private string noDataCaption;
    private Clan clan;

    void Awake()
    {
        Instance = this;
    }

    public static PlayerInfo Instance
    {
        get; private set;
    }

    public void Show(int playerId, string playerName)
    {
        id = playerId;
        noDataCaption = Localizer.GetText("NoData");

        Http.Request request = Http.Manager.Instance().CreateRequest("/player/info");

        request.Form.AddField("playerId", playerId);

        {
            Action loadingErrorCaption = () => XD.StaticContainer.UI.Reaction(XD.Message.MessageBox, XD.MessageBoxType.Notification, "UI_MB_CriticalError", "UI_MB_PlayerInfoLoadingError", "UI_Ok");

            Http.Manager.StartAsyncRequest(
                request:            request,
                failCallback:       failure => loadingErrorCaption(),
                successCallback:    successfulResult =>
                                    {
                                        if (successfulResult.Data.ContainsKey(PLAYER_KEY))
                                        {
                                            FillData(successfulResult, playerName);

                                            Player updatedPlayer = Player.Create(new JsonPrefs(successfulResult.Data[PLAYER_KEY]));

                                            updatedPlayer.Id = id;

                                            //ScoresController.Instance.UpdatePlayer(updatedPlayer);
                                        }
                                        else
                                        {
                                            loadingErrorCaption();
                                        }
                                    }
                );
        }
    }

    private void FillData(Http.Response response, string playerName)
    {
        GUIPager.SetActivePage("PlayerInfo", true, true);

        JsonPrefs playerInfo = new JsonPrefs(response.Data[PLAYER_KEY]);

        player = Player.Create(playerInfo);

        experience = playerInfo.ValueInt(EXPERIENCE_KEY, -1);

        if (player.Clan != null)
        {
            clan = player.Clan;

            clanInfoBtn.SetActive(true);
        }
        else
        {
            clanInfoBtn.SetActive(false);
        }

        bool isMe = id == ProfileInfo.playerId;
        bool isFriend = playerInfo.ValueBool(FRIENDSHIP_STATUS_KEY);
        bool isSocialFriend = false /*player.Social != null && FriendsManager.socialFriendsUids.Contains(player.Social.Uid)*/;

        avatar.Init(player);
        avatar.DownloadAvatar();

        Dictionary<string, object> battleStats = playerInfo.ValueObjectDict(BATTLE_STATS_KEY, null);

        if (battleStats == null)
        {
            return;
        }

        bool countRocketShots = GameData.IsGame(Game.BattleOfWarplanes | Game.BattleOfHelicopters | Game.ApocalypticCars);
    }

    private string BuildVehiclesList(JsonPrefs playerInfo)
    {
        List<string> vehicleIds = new List<string>();

        List<object> vehicles = playerInfo.ValueObjectList(VEHICLES_KEY);

        if (vehicles.Count == 0)
            return noDataCaption;

        try
        {
            foreach (object vehicleId in vehicles)
                foreach (VehicleInfo info in VehiclePool.Instance.Items)
                    if ((info.id.ToString() == vehicleId.ToString()))
                        vehicleIds.Add(info.vehicleName);

            return string.Join(",", vehicleIds.ToArray());
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                string.Format(
                    "Vehicle list parsing failed. Exception: {0}{1}",
                    exception.Message,
                    exception.StackTrace));

            return noDataCaption;
        }
    }

    private void ShowClanInfo()
    {
        ClansManager.Instance.OpenParticularClanWebPage(clan);
    }
}
