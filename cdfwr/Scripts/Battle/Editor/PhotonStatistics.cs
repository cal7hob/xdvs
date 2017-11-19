using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

using Hashtable = ExitGames.Client.Photon.Hashtable;

public class PhotonStatistics : EditorWindow
{
    private class RoomComparer : IComparer<RoomInfo>
    {
        public int Compare(RoomInfo first, RoomInfo second)
        {
            if (first.PlayerCount < second.PlayerCount)
                return -1;

            return first.PlayerCount == second.PlayerCount ? 0 : 1;
        }
    }

    private string gameVersion;
    private CloudRegionCode region = CloudRegionCode.eu;
    private ConnectionState lastConnectionState;
    private Dictionary<int, List<RoomInfo>> roomsForLevels = new Dictionary<int, List<RoomInfo>>();
    private Dictionary<int, bool> levelVisibilities = new Dictionary<int, bool>();
    private RoomComparer roomComparer = new RoomComparer();

    private Dictionary<CloudRegionCode, string> regionPostfix = new Dictionary<CloudRegionCode, string>
    {
        {CloudRegionCode.eu, "e"},
        {CloudRegionCode.asia , "a"}
    };

    private bool Disconnected
    {
        get { return PhotonNetwork.connectionState == ConnectionState.Disconnected; }
    }
    
    void OnEnable()
    {
        gameVersion = GameManager.PHOTON_ROOM_VERSION + regionPostfix[region];
        lastConnectionState = PhotonNetwork.connectionState;
    }

    void OnDisable()
    {
        if (!Disconnected)
        {
            PhotonNetwork.Disconnect();
        }
    }

    void OnGUI()
    {
        switch (PhotonNetwork.connectionState)
        {
            case ConnectionState.Disconnected:
                ShowGUIForConnect();
                break;
            case ConnectionState.Connecting:
                ShowGUIForConnecting();
                break;
            case ConnectionState.Connected:
                ShowStatistics();
                break;
        }
    }

    void Update()
    {
        // Перерисовать окно, если изменился статус фотоновского подключения
        if (lastConnectionState != PhotonNetwork.connectionState)
        {
            lastConnectionState = PhotonNetwork.connectionState;
            Repaint();
            return;
        }

        if (PhotonNetwork.connectionState == ConnectionState.Connected)
        {
            CollectRoomsInfo();
            Repaint();
        }
    }


    [MenuItem("HelpTools/Photon statistics")]
    private static void ShowWindow()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogError("Photon Statistics может быть запущен только в режиме игры");
            return;
        }

        PhotonStatistics window = GetWindow<PhotonStatistics>();
        window.Show();
    }

    private void ShowGUIForConnect()
    {
        CloudRegionCode lastRegion = region;
        region = (CloudRegionCode)EditorGUILayout.EnumPopup("Регион", region);
        if (region != lastRegion)
        {
            gameVersion = GameManager.PHOTON_ROOM_VERSION + (regionPostfix.ContainsKey(region) ? regionPostfix[region] : "");
        }

        gameVersion = EditorGUILayout.TextField("Версия для Photon", gameVersion);
        if (GUILayout.Button("Подключиться"))
        {
            Connect();
        }
    }

    private void ShowStatistics()
    {
        GUILayout.Label(string.Format("Всего игроков: {0}", PhotonNetwork.countOfPlayers));
        GUILayout.Label("Из них:");
        GUILayout.Label(string.Format("В комнатах: {0}", PhotonNetwork.countOfPlayersInRooms));
        GUILayout.Label(string.Format("В lobby: {0}", PhotonNetwork.countOfPlayersOnMaster));
        EditorGUILayout.Separator();

        //CollectRoomsInfo();
        GUILayout.Label(string.Format("Всего комнат: {0}", PhotonNetwork.countOfRooms));
        
        foreach (var keyValuePair in roomsForLevels)
        {
            if (keyValuePair.Value.Count == 0)
                continue;

            int currentLevel = keyValuePair.Key;
            bool levelVisibility;
            if (!levelVisibilities.TryGetValue(currentLevel, out levelVisibility))
            {
                levelVisibility = false;
                levelVisibilities.Add(currentLevel, false);
            }

            List<RoomInfo> rooms = keyValuePair.Value;
            // ReSharper disable once AssignmentInConditionalExpression
            if (levelVisibilities[currentLevel] = EditorGUILayout.Foldout(levelVisibility,
                string.Format("{0} уровень ({1}) - всего реальных игроков: {2}", currentLevel, rooms.Count, TotalPlayersInRooms(rooms))))
            {
                for (int i = 0; i < rooms.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label(string.Format("{0})", i + 1));
                    ShowRoomInfo(rooms[i]);
                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        if (GUILayout.Button("Отключиться"))
        {
            Disconnect();
        }
    }

    private void CollectRoomsInfo()
    {
        foreach (var roomsForLevel in roomsForLevels.Values)
        {
            roomsForLevel.Clear();
        }

        RoomInfo[] rooms = PhotonNetwork.GetRoomList();

        foreach (RoomInfo room in rooms)
        {
            int roomLevel = (int) room.CustomProperties["lv"];
            if (!roomsForLevels.ContainsKey(roomLevel))
            {
                roomsForLevels[roomLevel] = new List<RoomInfo>();
            }

            roomsForLevels[roomLevel].Add(room);
        }

        foreach (var roomsForLevel in roomsForLevels.Values)
        {
            roomsForLevel.Sort(roomComparer);
            roomsForLevel.Reverse();
        }
    }

    private void ShowRoomInfo(RoomInfo room)
    {
        Hashtable roomProperties = room.CustomProperties;
        int playerCount = room.PlayerCount;
        int botCount = (int) roomProperties["bcn"];
        int reservedPlaces = (int) roomProperties["rp"];
        int mapId = (int) roomProperties["mp"];
        GUILayout.Label(string.Format("Карта: {0}", (GameManager.MapId)mapId));
        GUILayout.Label(string.Format("Игроков: {0}", playerCount));
        GUILayout.Label(string.Format("Ботов: {0}", botCount));
        GUILayout.Label(string.Format("Всего: {0}", playerCount + botCount));
        GUILayout.Label(string.Format("Забронировано: {0}", reservedPlaces));
    }

    private void ShowGUIForConnecting()
    {
        GUILayout.Label("Connecting...");
    }

    private void Connect()
    {
        PhotonNetwork.ConnectToRegion(region, gameVersion);
    }

    private void Disconnect()
    {
        PhotonNetwork.Disconnect();
    }

    private int TotalPlayersInRooms(List<RoomInfo> rooms)
    {
        int totalPlayers = 0;
        for (int i = 0; i < rooms.Count; i++)
        {
            totalPlayers += rooms[i].PlayerCount;
        }

        return totalPlayers;
    }
}
