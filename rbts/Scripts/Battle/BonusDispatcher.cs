using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq; 
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Random = System.Random;
using BonusOptions = Http.BonusDispatcher;

public class BonusDispatcher : MonoBehaviour
{
    public List<GameObject> bonusObjects;

    private const int MAX_BONUSES_IN_ROOM = 15;
    private const int MIN_SILVER_AMOUNT = 50;
    private const float MAP_BONUS_APPEARANCE_TIME = 10;
    private const float MIN_BONUS_REFRESH_INTERVAL = 3;

    private static Random random;

    private readonly List<BonusItem> bonusItems = new List<BonusItem>(10);

    private int[] mapBonusChances;
    private string[] mapBonusNames;

    public static BonusDispatcher Instance
    {
        get; private set;
    }

    private static Random Random
    {
        get { return random ?? (random = new Random()); }
    }

    private static BonusOptions Options
    {
        get { return Http.Manager.BattleServer.options.bonusDispatcher; }
    }

    public List<BonusItem> BonusItems
    {
        get { return bonusItems; }
    }

    void Awake()
	{
		if (Instance)
			Debug.LogError("There are more than one BonusDispatcher on the scene", gameObject);

		Instance = this;

        Messenger.Subscribe(EventId.VehicleKilled, OnTankKilled);
        Messenger.Subscribe(EventId.HelicopterKilled, OnHelicopterKilled);
        Messenger.Subscribe(EventId.TryingTakeItem, OnItemTakenTry);
        Messenger.Subscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Messenger.Subscribe(EventId.NowImMaster, OnImMaster);
        Messenger.Subscribe(EventId.BeforeReconnecting, OnDisconnect);
        Messenger.Subscribe(EventId.TroubleDisconnect, OnDisconnect);
    }

	void OnDestroy()
	{
        Messenger.Unsubscribe(EventId.VehicleKilled, OnTankKilled);
        Messenger.Unsubscribe(EventId.HelicopterKilled, OnHelicopterKilled);
        Messenger.Unsubscribe(EventId.TryingTakeItem, OnItemTakenTry);
        Messenger.Unsubscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Messenger.Unsubscribe(EventId.NowImMaster, OnImMaster);
        Messenger.Unsubscribe(EventId.BeforeReconnecting, OnDisconnect);
        Messenger.Unsubscribe(EventId.TroubleDisconnect, OnDisconnect);

        Instance = null;
	}

	/* PUBLIC SECTION */
	public static void RegisterBonusItem(BonusItem item)
	{
		if (Instance != null)
			Instance.bonusItems.Add(item);
	}

	public static void UnRegisterBonusItem(BonusItem item)
	{
		if (Instance != null)
			Instance.bonusItems.Remove(item);
	}

    public static void GenerateKillBonus(VehicleController victim, VehicleController attacker)
    {
        GenerateKillBonus(victim, attacker, Vector3.zero);
    }

    public static void GenerateKillBonus(VehicleController victim, VehicleController attacker, Vector3 position)
	{
		if (!PhotonNetwork.isMasterClient || !BattleController.CheckPlayersCount())
			return;

		if (!victim)
		{
			Debug.LogError("GenerateKillBonus(): no victim specified!");
			return;
		}

		int itemIndex;
        int vehicleGroupDelta = -5;

	    if (GoldRush.Leader == victim.data.playerId)
	    {
            itemIndex = 10; // Бонус золотой лихорадки.
	    }
		else
		{
			if (!attacker)
			{
				//Debug.LogError("BonusDispatcher.GenerateKillBonus(): attacker can be null only for gold rush leader");
				return;
			}

            int goldBonusChance
                = Mathf.Clamp(
                    value:  GameData.BonusChancesData.goldChance - (attacker.data.playerLevel * attacker.data.playerLevel),
                    min:    GameData.BonusChancesData.goldChanceMin,
                    max:    GameData.BonusChancesData.goldChance
            );

			int silverBonusChance = GameData.BonusChancesData.silverChance;
			int experienceBonusChance = GameData.BonusChancesData.experienceChance;

			itemIndex = MiscTools.GetRandomIndex(goldBonusChance, silverBonusChance, experienceBonusChance); // Обычный бонус.

            vehicleGroupDelta = victim.VehicleGroup - attacker.VehicleGroup;
        }

		int amount = 0;
		string bonusPrefabName = null;

		switch (itemIndex)
		{
			case 0: // GOLD
				bonusPrefabName = "Bonus_Gold";
				amount = GoldBonusAmount(victim, vehicleGroupDelta);
				break;
			case 1: // SILVER
				bonusPrefabName = "Bonus_Silver";
				amount = SilverBonusAmount(victim, vehicleGroupDelta);
				break;
			case 2: // EXPERIENCE
				bonusPrefabName = "Bonus_Experience";
				amount = ExperienceBonusAmount(victim, attacker, false);
		        if (amount <= 0)
		            return;
                break;
			case 50:// Gold-rush
				bonusPrefabName = "Bonus_GoldRush";
				amount = GoldRush.TotalStake;
				GoldRush.Leader = 0;
				Hashtable properties = new Hashtable{{ "goldLeader", 0}};
				PhotonNetwork.room.SetCustomProperties(properties);
				break;
        }

		Vector3 bonusPosition
            = GameData.IsGame(Game.IronTanks)
                ? victim.transform.position
				: victim.transform.position + Vector3.up * 0.5f;

        if (position != Vector3.zero)
            bonusPosition = position;

        PhotonNetwork.InstantiateSceneObject(
            prefabName:
                string.Format("{0}/Bonuses/{1}", GameManager.CurrentResourcesFolder,
                    GameManager.PrefabNamePrefix + bonusPrefabName),
            position:   bonusPosition,
            rotation:   Quaternion.identity,
            group:      0,
            data:       new object[]
                        {
                            new BonusItem.BonusInfo(
                                _amount:            amount,
                                _pointIndex:        0,
                                _appearanceTime:    PhotonNetwork.time,
                                _ownerId:           victim.data.playerId)
                        });
	}

	public static int ExperienceBonusAmount(VehicleController victim, VehicleController attacker, bool instant)
	{
	    if (victim == attacker)
	        return 0;

        int maxArmor = victim.MaxArmor;
        int tankGroupDelta = victim.VehicleGroup - attacker.VehicleGroup;

        float divider
	        = GameData.BonusAmountsData.GetDividerValue(
                tankGroupDelta: tankGroupDelta,
                bonusType:      BonusItem.BonusType.Experience,
                forBot:         victim.IsBot,
                instant:        instant);

        int bonusValue = MiscTools.Round(Mathf.RoundToInt(maxArmor / divider), 5);

	    if (instant && VehicleController.AreClanmates(victim, attacker))  // Скидка за соклановца в моментальном бонусе.
	        bonusValue = (int)(bonusValue * GameData.ClanmateScoreBonusRatio);

	    return bonusValue;
	}
	
	/* PRIVATE SECTION */

	private static int GoldBonusAmount(VehicleController victim, int tankGroupDelta)
	{
	    return 1;
	}

	private static int SilverBonusAmount(VehicleController victim, int tankGroupDelta)
	{
        // Максимальная броня жертвы:
	    int maxArmor = victim.MaxArmor;

        // Делитель с сервера:
	    float divider
	        = GameData.BonusAmountsData.GetDividerValue(
                tankGroupDelta: tankGroupDelta,
                bonusType:      BonusItem.BonusType.Silver,
                forBot:         victim.IsBot,
                instant:        false);
        
        // Рандомный коэффициент, в границах, взятых с сервера:
	    float randomCoefficient
            = Convert.ToSingle(
                (Random.NextDouble()
                        * (GameData.BonusAmountsData.maxRandomCoefficient - GameData.BonusAmountsData.minRandomCoefficient))
                    + GameData.BonusAmountsData.minRandomCoefficient);

        // Умножаем броню жертвы на рандомный коэффициент, и делим это всё на делитель с сервера:
        float deservedSilverRaw = (maxArmor * randomCoefficient) / divider;

        // Округляем до целого числа, кратного 20:
        int deservedSilverRounded = MiscTools.Round(Mathf.RoundToInt(deservedSilverRaw), 20);

        // Отдаём получившееся значение; если оно не меньше 50. Если меньше – отдаём 50:
	    return deservedSilverRounded < MIN_SILVER_AMOUNT ? MIN_SILVER_AMOUNT : deservedSilverRounded;
	}

    private void SetChances()
    {
        mapBonusNames = new[]
        {
            "Bonus_Health", "Bonus_Fuel", "Bonus_Attack",
            "Bonus_Reload", "Bonus_Speedup", "Bonus_Landmine",
            "Bonus_Missile"
        };

        mapBonusChances = new[]
        {
            Options.healthBonusChance, Options.fuelBonusChance, Options.attackBonusChance,
            Options.reloadBonusChance, Options.speedupBonusChance, Options.landmineBonusChance,
            Options.missileBonusChance
        };
    }

	private void GenerateMapBonuses(int count)
	{
        if (count <= 0 || !PhotonNetwork.isMasterClient || !BattleController.CheckPlayersCount())
			return;

        bonusObjects = new List<GameObject>();

        string[] bonusNames = MiscTools.GetRandomFromSeveral(mapBonusChances, mapBonusNames, count);

		foreach (string bonusName in bonusNames)
		{
			int pointIndex;

			Vector3 point = BonusPoints.GetRandomPoint(out pointIndex);

            BonusPoints.LockPoint(pointIndex);

		    int amount;

		    switch (bonusName)
		    {
		        case "Bonus_Missile":
		            amount = 1;
		            break;
                default:
		            amount = 0;
		            break;
		    }
            
            var bonus = PhotonNetwork.InstantiateSceneObject(
                prefabName:
                    string.Format("{0}/Bonuses/{1}", GameManager.CurrentResourcesFolder,
                        GameManager.PrefabNamePrefix + bonusName),
                position:   point,
                rotation:   Quaternion.identity,
                group:      0,
                data:       new object[]
                            {
                                new BonusItem.BonusInfo(
                                    _amount:            amount,
                                    _pointIndex:        pointIndex,
                                    _appearanceTime:    PhotonNetwork.time + MAP_BONUS_APPEARANCE_TIME,
                                    _ownerId:           0)
                                    });
  
            bonusObjects.Add(bonus);
		}

		PhotonNetwork.room.SetCustomProperties(
            new Hashtable
            {
                { "bc", (int)PhotonNetwork.room.CustomProperties["bc"] + count }
            });
	}

    private void OnTankKilled(EventId id, EventInfo info)
	{
        if (GameData.IsGame(Game.BattleOfHelicopters))
            return;

		EventInfo_II eventInfo = (EventInfo_II)info;

		int victimId = eventInfo.int1;
		int attackerId = eventInfo.int2;

		VehicleController victim;
		VehicleController attacker;

	    if (PhotonNetwork.isMasterClient &&
	        BattleController.allVehicles.TryGetValue(victimId, out victim) &&
	        BattleController.allVehicles.TryGetValue(attackerId, out attacker))
	    {
            GenerateKillBonus(victim, attacker);
	    }
	}

    private void OnHelicopterKilled(EventId id, EventInfo info)
    {
        EventInfo_IIV eventInfo = (EventInfo_IIV)info;

        int victimId = eventInfo.int1;
        int attackerId = eventInfo.int2;
        Vector3 position = eventInfo.vector;

        VehicleController victim;
        VehicleController attacker;

        if (PhotonNetwork.isMasterClient &&
            BattleController.allVehicles.TryGetValue(victimId, out victim) &&
            BattleController.allVehicles.TryGetValue(attackerId, out attacker))
        {
            GenerateKillBonus(victim, attacker, position);
        }
    }

    private void CleanRoom(bool withDestroying)
	{
		foreach (BonusItem item in bonusItems)
		{
		    if (item.IsTaken)
		    {
                PhotonNetwork.Destroy(item.gameObject);
		        continue;
		    }

            if (item.info.appearanceTime >= BattleController.MyCreationTime)
				continue;

			if (withDestroying && !item.IsMapBonus)
				PhotonNetwork.Destroy(item.gameObject);
		}
	}

	private void OnItemTakenTry(EventId id, EventInfo info)
	{
        EventInfo_II eventInfo = (EventInfo_II)info;

		int itemId = eventInfo.int1;
		int playerId = eventInfo.int2;
        PhotonView photonView = PhotonView.Find(itemId);
		if (!photonView)
			return;

		BonusItem item = photonView.GetComponent<BonusItem>();
        if (item.IsTaken)
			return;
		
		item.IsTaken = true;
        if (PhotonNetwork.isMasterClient)
        {
            bool botTakes = BotDispatcher.IsPlayerBot(playerId);
            if (playerId == BattleController.MyPlayerId || botTakes || item.syncByMaster)
            {
                RecalcBonusValueForItem(item, playerId);
                Messenger.Send(
		            id: EventId.ItemTaken,
		            info: new EventInfo_III((int) item.bonusType, item.info.amount, playerId),
		            target: botTakes ? Messenger.EventTargetType.ToMaster : Messenger.EventTargetType.ToSpecific,
		            specificId: playerId);
		    }
            PhotonNetwork.Destroy(photonView.gameObject);
        }
		else if (!item.syncByMaster)
		{
            RecalcBonusValueForItem(item, playerId);
            Messenger.Send(EventId.ItemTaken, new EventInfo_III((int)item.bonusType, item.info.amount, playerId)); 
		}
	}

    private void RecalcBonusValueForItem(BonusItem item, int getterId)
    {
        VehicleController owner;
        VehicleController getter;
        if (!BattleController.allVehicles.TryGetValue(item.info.ownerId, out owner)
                || !BattleController.allVehicles.TryGetValue(getterId, out getter)
                || !VehicleController.AreClanmates(getter, owner))      //Эффект только для соклановцев
            return;

        switch (item.bonusType)
        {
            case BonusItem.BonusType.Experience:
                item.info.amount = MiscTools.Round((int)(item.info.amount * GameData.ClanmateScoreBonusRatio), 5, true); //Изменение бонуса, если соклановцы
                break;
            case BonusItem.BonusType.Silver:
                item.info.amount = MiscTools.Round((int)(item.info.amount * GameData.ClanmateSilverBonusRatio), 20, true); //Изменение бонуса, если соклановцы
                break;
        }
    }

    private void OnMainTankAppeared(EventId eid, EventInfo ei)
    {
        if (PhotonNetwork.isMasterClient)
            return;

        for (int i = 0; i < bonusItems.Count; i++)
        {
            bonusItems[i].IsTaken = true;
        }
    }

    private void OnImMaster(EventId id, EventInfo ei)
	{
		Debug.Log("I'm MASTER!");
		EventInfo_B info = (EventInfo_B)ei;
		CleanRoom(true);
		StartCoroutine(CheckRoomBonuses(info.bool1));
	}

    private void OnDisconnect(EventId id, EventInfo ei)
    {
        StopAllCoroutines();
        CancelInvoke();
    }

    private bool CheckPlayersAmount()
    {
        if (BattleController.allVehicles.Count < GameData.BonusChancesData.minPlayersForSpawn)
            return false;

        string someClanName = null;
        foreach (var veh in BattleController.allVehicles.Values)
        {
            if (string.IsNullOrEmpty(veh.data.clanName))
                return true;

            if (someClanName == null)
            {
                someClanName = veh.data.clanName;
                continue;
            }

            if (someClanName != veh.data.clanName)
                return true;
        }

        return false;
    }

	private IEnumerator CheckRoomBonuses(bool iAmCreator)
	{
        while (!GameManager.BattleOptionsReceived && PhotonNetwork.isMasterClient && !ProfileInfo.IsBattleTutorial)/* && BattleController.MyVehicle == null) )*/
        {
            yield return null;
        }

        if (mapBonusChances == null)
            SetChances();

        double nextBonusRefresh;
        YieldInstruction wait = new WaitForSeconds(1.0f);

        if (iAmCreator)
	    {
	        nextBonusRefresh = PhotonNetwork.time;
	        PhotonNetwork.room.SetCustomProperties(new Hashtable { { "nbr", nextBonusRefresh } });
	    }
	    else
	    {
            nextBonusRefresh = (double)PhotonNetwork.room.CustomProperties["nbr"];
	    }

        while (PhotonNetwork.isMasterClient)
		{
            while (PhotonNetwork.time < nextBonusRefresh)
                yield return wait;

            if (PhotonNetwork.room == null)
                yield break;

            if (CheckPlayersAmount() || ProfileInfo.IsBattleTutorial)
                GenerateMapBonuses(Mathf.Clamp(Options.bonusesInRoom, 0, MAX_BONUSES_IN_ROOM) - (int)PhotonNetwork.room.CustomProperties["bc"]);

			nextBonusRefresh += Mathf.Clamp(Options.bonusRefreshInterval, MIN_BONUS_REFRESH_INTERVAL, int.MaxValue);
            PhotonNetwork.room.SetCustomProperties(new Hashtable { { "nbr", nextBonusRefresh } });

            yield return wait;
        }
	}
}