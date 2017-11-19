using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System;

public class Minimap : MonoBehaviour 
{
    public Transform itemsParent;
    public MinimapTankIcon itemPrefab;
    public tk2dSlicedSprite sprMap;
    public static float scaleKoefX = 1f, scaleKoefY = 1f;
    public tk2dTextMesh ourTeamCount, enemyTeamCount;
    public GameObject battleCountGO;
    public GameObject wrapper;

    public static Transform realMapCornerBL, realMapCornerTR;

    private Dictionary<int, MinimapTankIcon> items = new Dictionary<int, MinimapTankIcon>();
    private int chatMessagePhotonPlayerId = 0;
    private int chatMessageId = 0;

    public static Minimap Instance { get; private set; }

    void Awake () 
    {
        Dispatcher.Subscribe(EventId.TankJoinedBattle, OnVehicleConnected);
        Dispatcher.Subscribe(EventId.TankKilled, OnVehicleKilled);
        Dispatcher.Subscribe(EventId.TankRespawned, OnVehicleRespawned);
        Dispatcher.Subscribe(EventId.TankLeftTheGame, OnVehicleLeftGame);
        Dispatcher.Subscribe(EventId.PlayerKickout, OnPlayerKickout);
        Dispatcher.Subscribe(EventId.MainTankAppeared, OnMainVehicleAppeared);
        Dispatcher.Subscribe(EventId.OnStatTableChangeVisibility, OnStatTableChangeVisibility);
        if (ourTeamCount && enemyTeamCount)
            Dispatcher.Subscribe(EventId.TeamScoreChanged, TeamScoreChanged);
        if (itemPrefab.chatMessageWrapper)
            Dispatcher.Subscribe(EventId.BattleChatCommand, OnBattleChatCommand);
        Instance = this;
    }

    void Start ()
    {
        TeamScoreChanged(EventId.Manual, null);
        if (BattleController.Instance.BattleMode == GameData.GameMode.Deathmatch)
        {
            if (battleCountGO)
                battleCountGO.SetActive(false);
        }

        if (realMapCornerBL == null)
        {
            GameObject goBL = GameObject.Find("MapCornerBL");
            GameObject goTR = GameObject.Find("MapCornerTR");
            if (goBL == null || goTR == null)
            {
                DT.LogError("Cant find corner objects for minimap. Disabling it...");
                gameObject.SetActive(false);
                return;
            }
            realMapCornerBL = goBL.transform;
            realMapCornerTR = goTR.transform;
        }

        scaleKoefX = Mathf.Abs(sprMap.dimensions.x / realMapCornerTR.transform.localPosition.x);
        scaleKoefY = Mathf.Abs(sprMap.dimensions.y / realMapCornerTR.transform.localPosition.z);

        tk2dSpriteFromTexture sprFromTex = sprMap.GetComponent<tk2dSpriteFromTexture>();
        sprFromTex.texture = (Texture2D)Resources.Load(GameData.CurInterface.ToString() + "/Textures/Minimap/minimap_" + SceneManager.GetActiveScene().name);
        sprMap.gameObject.SetActive(true);
        sprFromTex.ForceBuild();
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.TankJoinedBattle, OnVehicleConnected);
        Dispatcher.Unsubscribe(EventId.TankKilled, OnVehicleKilled);
        Dispatcher.Unsubscribe(EventId.TankLeftTheGame, OnVehicleLeftGame);
        Dispatcher.Unsubscribe(EventId.TankRespawned, OnVehicleRespawned);
        Dispatcher.Unsubscribe(EventId.PlayerKickout, OnPlayerKickout);
        Dispatcher.Unsubscribe(EventId.MainTankAppeared, OnMainVehicleAppeared);
        Dispatcher.Unsubscribe(EventId.TeamScoreChanged, TeamScoreChanged);
        Dispatcher.Unsubscribe(EventId.OnStatTableChangeVisibility, OnStatTableChangeVisibility);
        Dispatcher.Unsubscribe(EventId.BattleChatCommand, OnBattleChatCommand);
        Instance = null;
    }

    private void OnVehicleConnected(EventId id, EventInfo info)
    {
        EventInfo_I eventInfo = (EventInfo_I)info;
        int playerId = eventInfo.int1;
        if(BattleController.MyVehicle != null)
            ShowIcon(playerId);
    }

    private void OnVehicleRespawned(EventId id, EventInfo info)
    {
        EventInfo_I eventInfo = (EventInfo_I)info;
        int playerId = eventInfo.int1;
        if (BattleController.MyVehicle != null)
            ShowIcon(playerId);
    }

    private void OnMainVehicleAppeared(EventId id, EventInfo info)
    {
        foreach(int key in BattleController.allVehicles.Keys)
            ShowIcon(key);
    }

    private void OnVehicleKilled(EventId id, EventInfo info)
    {
        EventInfo_III eventInfo = (EventInfo_III)info;

        int victimId = eventInfo.int1;
        HideIcon(victimId, false);
    }

    private void OnVehicleLeftGame(EventId id, EventInfo info)
    {
        EventInfo_I eventInfo = (EventInfo_I)info;
        int playerId = eventInfo.int1;
        HideIcon(playerId,true);
    }

    private void OnPlayerKickout(EventId id, EventInfo info)
    {
        EventInfo_I eventInfo = (EventInfo_I)info;
        int playerId = eventInfo.int1;
        HideIcon(playerId,true);
    }

    private void HideIcon(int playerId, bool removeItem)
    {
        if (!items.ContainsKey(playerId))
        {
            //DT.LogWarning("Player {0} was not found in dictionaries! items: {1}", BattleController.GameStat[playerId].playerName, items.ContainsKey(playerId));
            return;
        }
        if (removeItem)
        {
            GameObject.Destroy(items[playerId].gameObject);
            items.Remove(playerId);
        }
        else
            items[playerId].gameObject.SetActive(false);
        
    }

    private void ShowIcon(int playerId)
    {
        if (!BattleController.allVehicles.ContainsKey(playerId))
        {
            DT.LogError("CreateIcon. Player {0} was not found in BattleController.allVehicles!", BattleController.GameStat[playerId].playerName);
            return;
        }
        if(items.ContainsKey(playerId))
        {
            items[playerId].gameObject.SetActive(true);
        }
        else
        {
            //Instantiate Minimap Icons
            items[playerId] = GameObject.Instantiate(itemPrefab, Vector3.one, Quaternion.identity) as MinimapTankIcon;
            items[playerId].transform.parent = itemsParent;
            items[playerId].transform.localPosition = Vector3.zero;
        }
        items[playerId].Setup(playerId);
    }

    private void TeamScoreChanged(EventId id, EventInfo info)
    {
        if(ourTeamCount)
            ourTeamCount.text = ScoreCounter.FriendTeamScore.ToString();
        if(enemyTeamCount)
            enemyTeamCount.text = ScoreCounter.EnemyTeamScore.ToString();
    }

    private void OnStatTableChangeVisibility(EventId id, EventInfo info)
    {
        wrapper.SetActive(!((EventInfo_B)info).bool1);
    }

    private MinimapTankIcon GetItem(int photonPlayerId)
    {
        if (items == null || !items.ContainsKey(photonPlayerId))
            return null;
        return items[photonPlayerId];
    }

    private void OnBattleChatCommand(EventId id, EventInfo info)
    {
        EventInfo_U eventData = (EventInfo_U)info;
        chatMessagePhotonPlayerId = Convert.ToInt32(eventData[0]);
        chatMessageId = Convert.ToInt32(eventData[1]);

        MinimapTankIcon ti = GetItem(chatMessagePhotonPlayerId);
        if (!ti || !ti.gameObject.activeInHierarchy)
            return;

        ti.SetupChatMessage(new BattleChatPanelItemData(chatMessagePhotonPlayerId, (BattleChatCommands.Id)chatMessageId, Time.realtimeSinceStartup));
    }
}
