using System;
using System.Collections;
using System.Collections.Generic;
using Disconnect;
using Matchmaking;
using UnityEngine;

public class RoomInfoManager : MonoBehaviour
{
    public static Dictionary<GameData.GameMode, Dictionary<GameManager.MapId, string>> RoomsShowedInHangar { get; private set; }

    private const float MAP_SELECT_TIMEOUT = 40f;

    private Dictionary<GameManager.MapId, int> roomPlayersCount = new Dictionary<GameManager.MapId, int>();
    
    private bool isReceivedRoomListUpdate = false;

    void Awake()
    {
        BattleConnectManager.AddPhotonMessageTarget(gameObject);
        Dispatcher.Subscribe(EventId.GameModeChanged, OnGameModeChanged);
        GUIPager.OnPageChange += OnPageChange;

        //Fill dic with game modes on app start
        if(RoomsShowedInHangar == null)
        {
            RoomsShowedInHangar = new Dictionary<GameData.GameMode, Dictionary<GameManager.MapId, string>>();
            foreach (GameData.GameMode mode in Enum.GetValues(typeof(GameData.GameMode)))
                RoomsShowedInHangar[mode] = new Dictionary<GameManager.MapId, string>();
        }
    }

    void OnDestroy()
    {
        BattleConnectManager.RemovePhotonMessageTarget(gameObject);
        Dispatcher.Unsubscribe(EventId.GameModeChanged, OnGameModeChanged);
        GUIPager.OnPageChange -= OnPageChange;
    }

    void OnReceivedRoomListUpdate()
    {
        isReceivedRoomListUpdate = true;
        RefreshInfo();
    }

    private void OnPageChange(string prevPage, string curPage)
    {
        if (curPage == "MapSelection")
        {
            RefreshInfo();//Initialize dictionaries and turn on waiting indicator 
            GameManager.Instance.ConnectToPhoton();
            Invoke("MapSelectTimeout", MAP_SELECT_TIMEOUT);
        }
        else if(prevPage == "MapSelection")
        {
            PhotonNetwork.Disconnect();
            CancelInvoke("MapSelectTimeout");
        }
    }

    private void OnGameModeChanged(EventId id = 0, EventInfo ei = null)
    {
        RefreshInfo();
    }

    private void RefreshInfo()
    {
        if(GUIPager.ActivePageName != "MapSelection")
        {
            //Debug.Log("Ignore refreshing, because we are not in MapSelection");
            return;
        }

        //Initialize empty dictionaries
        roomPlayersCount.Clear();

        //Fill Dictionaries if room list update was received
        if (isReceivedRoomListUpdate)
        {
            foreach (GameManager.MapId mapId in GameData.availableMapsDic.Keys)
            {
                int count = 0;
                string selectedRoom = RoomsShowedInHangar[GameData.Mode].ContainsKey(mapId) ? RoomsShowedInHangar[GameData.Mode][mapId] : null;
                RoomsShowedInHangar[GameData.Mode][mapId] = MatchMaker.SelectRoom((int)mapId, false, selectedRoom, out count, Shop.CurrentVehicle.Info.vehicleGroup);
                roomPlayersCount[mapId] = count;
            }
        }

        //Update counter / waiting indicator
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

    private void OnDisconnectedFromPhoton()
    {
        isReceivedRoomListUpdate = false;
    }

    private void MapSelectTimeout()
    {
        PhotonNetwork.Disconnect();
        GUIPager.SetActivePage("MainMenu");
    }
}