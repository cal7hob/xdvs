using System.Collections;
using System.Collections.Generic;
using Disconnect;
using Matchmaking;
using UnityEngine;

using GameMode = GameData.GameMode;
using XD;
using System;

public class RoomInfoManager : MonoBehaviour
{
    private const float MAP_SELECT_TIMEOUT = 40f;

    public static Dictionary<XD.MapId, string> SelectedRooms { get; private set; }

    private Dictionary<GameMode, Dictionary<XD.MapId, string>> allModeSelectedRooms = new Dictionary
        <GameMode, Dictionary<XD.MapId, string>>
    {
        {GameMode.Unknown, new Dictionary<XD.MapId, string>()},
        {GameMode.Deathmatch, new Dictionary<XD.MapId, string>()},
        {GameMode.Team, new Dictionary<XD.MapId, string>()}
    };
    private Dictionary<GameMode, Dictionary<XD.MapId, int>> allModeRoomPlayersCount = new Dictionary<GameMode, Dictionary<XD.MapId, int>>
    {
        {GameMode.Unknown, new Dictionary<XD.MapId, int>()},
        {GameMode.Deathmatch, new Dictionary<XD.MapId, int>()},
        {GameMode.Team, new Dictionary<XD.MapId, int>()}
    };
    private Dictionary<XD.MapId, int> roomPlayersCount = new Dictionary<XD.MapId, int>();
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
        XD.StaticContainer.Connector.AddPhotonMessageTarget(gameObject);
        Dispatcher.Subscribe(EventId.MapSelectionAppeared, OnMapSelectionAppeared);
        Dispatcher.Subscribe(EventId.GameModeChanged, OnGameModeChanged);
    }

    void OnDestroy()
    {
        XD.StaticContainer.Connector.RemovePhotonMessageTarget(gameObject);
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
            XD.StaticContainer.GameManager.ConnectToPhoton();
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

    private static List<ARMapInfo> maps = null;
    public static List<ARMapInfo> GetMaps(bool recreate = false)
    {
        if (maps != null && !recreate)
        {
            return maps;
        }

        maps = new List<ARMapInfo>();
        List<MapId> available = new List<XD.MapId>();

        foreach (Dictionary<string, object> map in GameData.mapsList)
        {
            if ((MapId)Convert.ToInt32(map["mapId"]) != MapId.random_map &&
                (bool)map["isEnabled"] &&
                Convert.ToInt32(map["mapLevel"]) <= StaticType.Profile.Instance<IProfile>().LevelCalculator.Level)
            {
                available.Add((MapId)Convert.ToInt32(map["mapId"]));
            }
        }

        foreach (Dictionary<string, object> map in GameData.mapsList)
        {
            MapId mapId = (MapId)Convert.ToInt32(map["mapId"]);
            //Инстанируем плашку рандомной карты только если больше одной карты доступно
            if ((mapId == MapId.random_map && available.Count > 1) || mapId != MapId.random_map)
            {
                ARMapInfo info = new ARMapInfo(map);
                maps.Add(info);
            }
        }

        return maps;
    }

    private void RefreshInfo( bool isReceivedRoomListUpdate, bool isGameModeChanged = false) 
    {

        if (isReceivedRoomListUpdate || isGameModeChanged)
        {
            List<MapId> list = new List<MapId>();
            foreach (ARMapInfo map in GetMaps())
            {
                list.Add(map.id);
            }
            
            IUnitHangar unit = StaticType.MainData.Instance<IMainData>().SelectedHangarUnit;

            if (unit == null)
            {
                Debug.LogErrorFormat(this, "{0} RefreshInfo False -- unit is NULL!", name);
                return;
            }

            foreach (MapId mapId in list)
            {
                int count = 0;
                string selectedRoom = SelectedRooms.ContainsKey(mapId) ? SelectedRooms[mapId] : null;                

                SelectedRooms[mapId] = MatchMaker.SelectRoom((int)mapId, false, selectedRoom, out count, unit.VehicleGroup);
                roomPlayersCount[mapId] = count;
            }
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