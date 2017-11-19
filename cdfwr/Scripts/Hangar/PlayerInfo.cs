using System;
using System.Collections.Generic;
using Tanks.Models;
using UnityEngine;

public class PlayerInfo : MonoBehaviour
{
    public tk2dTextMesh nicknameValueLabel;
    public tk2dTextMesh levelValueLabel;
    public tk2dTextMesh statusValueLabel;
    public tk2dTextMesh countryValueLabel;
    public tk2dTextMesh regionValueLabel;
    public tk2dTextMesh tanksValueLabel;
    public tk2dTextMesh battlesCountValueLabel;
    public tk2dTextMesh maxKillsInARowValueLabel;
    public tk2dTextMesh shootsValueLabel;
    public tk2dTextMesh hitsValueLabel;
    public tk2dTextMesh accuracyValueLabel;
    public tk2dTextMesh fragsValueLabel;
    public tk2dTextMesh mileageValueLabel;
    public tk2dTextMesh deathsValueLabel;
    public tk2dTextMesh clanName;
    public tk2dBaseSprite clanIco;
    public Avatar avatar; 
    public ActivatedUpDownButton btnFriend;
    public ActivatedUpDownButton btnUnfriend;
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
            Action loadingErrorCaption = () => MessageBox.Show(MessageBox.Type.Info, Localizer.GetText("PlayerInfoLoadingError"));

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

                                            ScoresController.Instance.UpdatePlayer(updatedPlayer);
                                        }
                                        else
                                        {
                                            loadingErrorCaption();
                                        }
                                    }
                );
        }
    }

    public void Close(tk2dUIItem tk2dUiItem)
    {
        GUIPager.Back();
    }

    public void Friend(tk2dUIItem tk2dUiItem)
    {
        FriendsManager.AddToFriends(
            playerId:           id,
            successCallback:    () =>
                                {
                                    btnFriend.Activated = false;
                                    btnUnfriend.Activated = true;
                                });
    }

    public void Unfriend(tk2dUIItem tk2dUiItem)
    {
        FriendsManager.RemoveFromFriends(
            playerId:           id,
            successCallback:    () =>
                                {
                                    btnUnfriend.Activated = false;
                                    btnFriend.Activated = true;
                                });
    }

    private void FillData(Http.Response response, string playerName)
    {
        GUIPager.SetActivePage("PlayerInfo", true, true);

        JsonPrefs playerInfo = new JsonPrefs(response.Data[PLAYER_KEY]);

        player = Player.Create(playerInfo);

        experience = playerInfo.ValueInt(EXPERIENCE_KEY, -1);

        levelValueLabel.text
            = experience != -1
                ? string.Format(
                    "{0} {1}",
                    Mathf.Clamp(
                        ProfileInfo.LevelForExperience(experience),
                        1,
                        50),
                    Localizer.GetText("Level"))
                : string.Empty;

        nicknameValueLabel.text = playerName;

        if (player.Clan != null)
        {
            clan = player.Clan;

            clanInfoBtn.SetActive(true);

            clanName.text = string.Format("[{0}]", clan.Name);

            clanIco.SetSprite(clan.Image);
        }
        else
        {
            clanInfoBtn.SetActive(false);
        }

        statusValueLabel.text = Localizer.GetText(player.IsOnline ? "OnlineStatus" : "OfflineStatus");
        countryValueLabel.text = playerInfo.ValueString(COUNTRY_KEY, noDataCaption);
        regionValueLabel.text = playerInfo.ValueString(REGION_KEY, noDataCaption);
        tanksValueLabel.text = BuildVehiclesList(playerInfo);

        bool isMe = id == ProfileInfo.profileId;
        bool isFriend = playerInfo.ValueBool(FRIENDSHIP_STATUS_KEY);
        bool isSocialFriend = player.Social != null && FriendsManager.socialFriendsUids.Contains(player.Social.Uid);

        btnFriend.Activated = !isFriend && !isMe && !isSocialFriend;
        btnUnfriend.Activated = isFriend && !isSocialFriend;

        avatar.Init(player);
        avatar.DownloadAvatar();

        Dictionary<string, object> battleStats = playerInfo.ValueObjectDict(BATTLE_STATS_KEY, null);

        if (battleStats == null)
        {
            foreach (tk2dTextMesh label in new[]
            {
                battlesCountValueLabel, maxKillsInARowValueLabel,
                shootsValueLabel,       hitsValueLabel,
                accuracyValueLabel,     fragsValueLabel,
                mileageValueLabel,      deathsValueLabel
            }) label.text = noDataCaption;

            return;
        }

        bool countRocketShots = false;

        battlesCountValueLabel.text = battleStats["battlesCount"].ToString();
        maxKillsInARowValueLabel.text = battleStats["maxKillsInARow"].ToString();

        shootsValueLabel.text = battleStats[countRocketShots ? "totalShootsSaclos" : "totalShoots"].ToString();
        hitsValueLabel.text = battleStats[countRocketShots ? "totalHitsSaclos" : "totalHits"].ToString();
        accuracyValueLabel.text = battleStats[countRocketShots ? "overralAccuracySaclos" : "overralAccuracy"].ToString();

        fragsValueLabel.text = battleStats["totalFrags"].ToString();
        mileageValueLabel.text = battleStats["totalMileage"].ToString();
        deathsValueLabel.text = battleStats["totalDeaths"].ToString();
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
