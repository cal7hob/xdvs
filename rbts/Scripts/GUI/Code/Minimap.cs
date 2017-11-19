using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System;

public class Minimap : MonoBehaviour
{
    public Transform itemsParent;
    public MinimapTankIcon itemPrefab;
    public tk2dBaseSprite sprMap;
    public static float scaleFactorX = 1f, scaleFactorY = 1f;
    public tk2dTextMesh ourTeamCount, enemyTeamCount;
    public GameObject battleCountGO;
    public GameObject wrapper;

    public static Transform realMapCornerBL, realMapCornerTR;

    private Dictionary<int, MinimapTankIcon> items = new Dictionary<int, MinimapTankIcon>();
    private int chatMessagePhotonPlayerId = 0;
    private int chatMessageId = 0;

    public static Minimap Instance { get; private set; }

    void Awake()
    {
        Messenger.Subscribe(EventId.TankJoinedBattle, OnVehicleConnected);
        Messenger.Subscribe(EventId.VehicleKilled, OnVehicleKilled);
        Messenger.Subscribe(EventId.VehicleRespawned, OnVehicleRespawned);
        Messenger.Subscribe(EventId.TankLeftTheGame, OnVehicleLeftGame);
        Messenger.Subscribe(EventId.PlayerKickout, OnPlayerKickout);
        Messenger.Subscribe(EventId.MainTankAppeared, OnMainVehicleAppeared);
        Messenger.Subscribe(EventId.StatTableVisibilityChange, OnStatTableChangeVisibility);
        if (ourTeamCount && enemyTeamCount)
            Messenger.Subscribe(EventId.TeamScoreChanged, OnTeamScoreChanged);
        if (itemPrefab.chatMessageWrapper)
            Messenger.Subscribe(EventId.BattleChatCommand, OnBattleChatCommand);
        Instance = this;
    }

    void Start()
    {
        RefreshTeamScore();
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

        tk2dSpriteFromTexture sprFromTex = sprMap.GetComponent<tk2dSpriteFromTexture>();

        var minimapPath = GameData.CurInterface.ToString() + "/Minimap/minimap_" + SceneManager.GetActiveScene().name;
        sprFromTex.texture = (Texture2D)Resources.Load(minimapPath);

        if (sprFromTex.texture == null)
        {
            Debug.LogError("Missing minimap texture: " + minimapPath);
            return;
        }

        scaleFactorX = Mathf.Abs(sprFromTex.texture.width / realMapCornerTR.transform.localPosition.x);
        scaleFactorY = Mathf.Abs(sprFromTex.texture.height / realMapCornerTR.transform.localPosition.z);

        sprFromTex.ForceBuild();
        sprMap.gameObject.SetActive(true);
    }

    private void RefreshTeamScore()
    {
        if (ourTeamCount)
            ourTeamCount.text = ScoreCounter.FriendTeamScore.ToString();
        if (enemyTeamCount)
            enemyTeamCount.text = ScoreCounter.EnemyTeamScore.ToString();
    }

    void OnDestroy()
    {
        Messenger.Unsubscribe(EventId.TankJoinedBattle, OnVehicleConnected);
        Messenger.Unsubscribe(EventId.VehicleKilled, OnVehicleKilled);
        Messenger.Unsubscribe(EventId.TankLeftTheGame, OnVehicleLeftGame);
        Messenger.Unsubscribe(EventId.VehicleRespawned, OnVehicleRespawned);
        Messenger.Unsubscribe(EventId.PlayerKickout, OnPlayerKickout);
        Messenger.Unsubscribe(EventId.MainTankAppeared, OnMainVehicleAppeared);
        Messenger.Unsubscribe(EventId.TeamScoreChanged, OnTeamScoreChanged);
        Messenger.Unsubscribe(EventId.StatTableVisibilityChange, OnStatTableChangeVisibility);
        Messenger.Unsubscribe(EventId.BattleChatCommand, OnBattleChatCommand);
        Instance = null;
    }

    private void OnVehicleConnected(EventId id, EventInfo info)
    {
        EventInfo_I eventInfo = (EventInfo_I)info;
        int playerId = eventInfo.int1;
        if (BattleController.MyVehicle != null)
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
        foreach (int key in BattleController.allVehicles.Keys)
            ShowIcon(key);
    }

    private void OnVehicleKilled(EventId id, EventInfo info)
    {
        EventInfo_II eventInfo = (EventInfo_II)info;

        int victimId = eventInfo.int1;
        HideIcon(victimId, false);
    }

    private void OnVehicleLeftGame(EventId id, EventInfo info)
    {
        EventInfo_I eventInfo = (EventInfo_I)info;
        int playerId = eventInfo.int1;
        HideIcon(playerId, true);
    }

    private void OnPlayerKickout(EventId id, EventInfo info)
    {
        EventInfo_I eventInfo = (EventInfo_I)info;
        int playerId = eventInfo.int1;
        HideIcon(playerId, true);
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
        if (items.ContainsKey(playerId))
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

    private void OnTeamScoreChanged(EventId id, EventInfo info)
    {
        RefreshTeamScore();
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

        ti.SetupChatMessage(new BattleChatPanelItemData(chatMessagePhotonPlayerId, (BattleChatCommands.Id)chatMessageId, Time.time));
    }
}
