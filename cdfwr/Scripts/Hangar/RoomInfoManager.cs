using System.Collections;
using System.Collections.Generic;
using Disconnect;
using Matchmaking;
using UnityEngine;

using GameMode = GameData.GameMode;

public class RoomInfoManager : MonoBehaviour
{
    private const float MAP_SELECT_TIMEOUT = 40f;

    public static Dictionary<GameManager.MapId, string> SelectedRooms { get; private set; }

    private Dictionary<GameMode, Dictionary<GameManager.MapId, string>> allModeSelectedRooms = new Dictionary
        <GameMode, Dictionary<GameManager.MapId, string>>
    {
        {GameMode.Unknown, new Dictionary<GameManager.MapId, string>()},
        {GameMode.Deathmatch, new Dictionary<GameManager.MapId, string>()},
        {GameMode.Team, new Dictionary<GameManager.MapId, string>()}
    };
    private Dictionary<GameMode, Dictionary<GameManager.MapId, int>> allModeRoomPlayersCount = new Dictionary<GameMode, Dictionary<GameManager.MapId, int>>
    {
        {GameMode.Unknown, new Dictionary<GameManager.MapId, int>()},
        {GameMode.Deathmatch, new Dictionary<GameManager.MapId, int>()},
        {GameMode.Team, new Dictionary<GameManager.MapId, int>()}
    };
    private Dictionary<GameManager.MapId, int> roomPlayersCount = new Dictionary<GameManager.MapId, int>();
    private GameMode gameMode;
    private GameMode GameMode
    {
        get { return gameMode; }
        set
        {
            gameMode = value;
            SelectedRooms = allModeSelectedRooms[gameMode];
            roomPlayersCount = allModeRoomPlayersCount[gameMode];
        }
    }


    void Awake()
    {
        GameMode = GameData.Mode;
        BattleConnectManager.AddPhotonMessageTarget(gameObject);
        Dispatcher.Subscribe(EventId.MapSelectionAppeared, OnMapSelectionAppeared);
        Dispatcher.Subscribe(EventId.GameModeChanged, OnGameModeChanged);
    }

    void OnDestroy()
    {
        BattleConnectManager.RemovePhotonMessageTarget(gameObject);
        Dispatcher.Unsubscribe(EventId.MapSelectionAppeared, OnMapSelectionAppeared);
        Dispatcher.Unsubscribe(EventId.GameModeChanged, OnGameModeChanged);
    }

    void OnReceivedRoomListUpdate()
    {
        //XdevsSplashScreen.SetActiveWaitingIndicator(false);
        RefreshInfo(isReceivedRoomListUpdate: true);
    }

    public static int GetPlaceReserve(RoomInfo roomInfo)
    {
        return roomInfo.CustomProperties.ContainsKey("rp") ?
            (int)roomInfo.CustomProperties["rp"]
            : 0;
    }

    private void OnMapSelectionAppeared(EventId id, EventInfo ei)
    {
        bool appeared = ((EventInfo_B)ei).bool1;
        if (appeared)
        {
            SelectedRooms.Clear();
            //XdevsSplashScreen.SetActiveWaitingIndicator(true, XdevsSplashScreen.Instance.waitingIndicator.GetParent(WaitingIndicatorBase.ParentType.MapSelection));
            GameManager.Instance.ConnectToPhoton();
            Invoke("MapSelectTimeout", MAP_SELECT_TIMEOUT);
            RefreshInfo(isReceivedRoomListUpdate: false);
        }
        else
        {
            PhotonNetwork.Disconnect();
            CancelInvoke("MapSelectTimeout");
        }
    }

    private void OnDisconnectedFromPhoton()
    {
        //XdevsSplashScreen.SetActiveWaitingIndicator(false);
    }

    private void RefreshInfo( bool isReceivedRoomListUpdate, bool isGameModeChanged = false) 
    {
        if(isReceivedRoomListUpdate || isGameModeChanged)
        {
            foreach (GameManager.MapId mapId in MapFramesCreator.AvailableMaps)
            {
                int count = 0;
                string selectedRoom = SelectedRooms.ContainsKey(mapId) ? SelectedRooms[mapId] : null;
                SelectedRooms[mapId] = MatchMaker.SelectRoom((int)mapId, false, selectedRoom, out count, Shop.CurrentVehicle.Info.vehicleGroup);
                roomPlayersCount[mapId] = count;
            }
        }

        foreach (MapSelector mapFrame in MapFramesCreator.MapSelectionFrames)
        {
            int minPlayers = GameData.isBotsEnabled ? (int)GameData.minBotCount : 0;
            int count = minPlayers;
            if (isReceivedRoomListUpdate && roomPlayersCount.ContainsKey(mapFrame.MapId))
                count = Mathf.Clamp(roomPlayersCount[mapFrame.MapId], minPlayers, roomPlayersCount[mapFrame.MapId]);
            bool showWaitingIndicator = !isReceivedRoomListUpdate;
            mapFrame.SetPlayersCount(count, showWaitingIndicator);
        }
    }

    private void MapSelectTimeout()
    {
        PhotonNetwork.Disconnect();
        SelectedRooms.Clear();
        GUIPager.SetActivePage("MainMenu");
    }

    private void OnGameModeChanged(EventId id, EventInfo ei)
    {
        GameMode = GameData.Mode;
        RefreshInfo(isReceivedRoomListUpdate: false, isGameModeChanged: true);
    }
}