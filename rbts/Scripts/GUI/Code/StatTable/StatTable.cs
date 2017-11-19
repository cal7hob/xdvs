using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System.Linq;
using UnityEngineInternal;
using CodeStage.AntiCheat.ObscuredTypes;
using Disconnect;
using Http;

public class StatTable : MonoBehaviour
{
    private enum RowType
    {
        Own,
        Friend,
        Enemy
    }

    private class BonusBuyingInfo
    {
        private tk2dTextMesh priceLabel;
        private ActivatedUpDownButton button;
        private ObscuredInt price;
        private bool blocked = false;
        private bool loading = false;

        public BonusBuyingInfo(tk2dTextMesh _priceLabel, ActivatedUpDownButton _button, int _price)
        {
            priceLabel = _priceLabel;
            button = _button;
            price = _price;
        }

        public int Price
        {
            get {return price;}
            set
            {
                if (blocked)
                    return;

                price = value;
                RefreshButton();
            }
        }

        public bool Blocked
        {
            get {return blocked;}
            set
            {
                blocked = value;
                RefreshButton();
            }
        }

        public bool Loading
        {
            get { return loading; }
            set
            {
                loading = value;
                RefreshButton ();
            }
        }

        private void RefreshButton ()
        {
            priceLabel.text = price.ToString();
            button.Activated = ProfileInfo.CanBuy(new ProfileInfo.Price(price, ProfileInfo.PriceCurrency.Gold)) && !blocked && !loading;
        }
    }

    public enum TableState
    {
        AfterDeath,
        Quit,
        BattleEnd,
        ExitWaiting,

        None
    }

    public StatTableRow ownRowPrefab;
    public StatTableRow otherRowPrefab;
    public StatTableRow friendRowPrefab;
    public GameObject friendEmptyRow;
    public GameObject enemyEmptyRow;
    public float rowSpacing;
    public GameObject afterDeathPart;
    public GameObject quitPart;
    public GameObject waitingExitPart;
    public GameObject battleEndPart;
    public Counter counter;
    public Counter exitCounter;
    public GameObject[] objectsActivatedIfVip;
    public GameObject[] objectsDisactivatedIfVip;
    public GameObject[] objectsActivatedIfDeathmatch;
    public GameObject[] objectsActivatedIfTeamMode;
    public ActivatedUpDownButton btnHasten;
    public ActivatedUpDownButton btnBuyBonusAttack;
    public ActivatedUpDownButton btnBuyBonusReload;
    public tk2dTextMesh lblAttackBonusPrice;
    public tk2dTextMesh lblReloadBonusPrice;
    public static StatTable instance;
    public static event Action OnStopTimer;

    private static int respawnTimeCorrection;
    private static Action scheduledShow;

    private GameObject[] friendEmptyRows;
    public GameObject[] enemyEmptyRows;
    [SerializeField]private tk2dTextMesh lblHeader;
    [SerializeField]private tk2dTextMesh lblMyTeamScore;
    [SerializeField]private tk2dTextMesh lblEnemyTeamScore;
    [SerializeField]private Transform tableBody;
    [SerializeField]private Transform tableBody2;
    private StatTableRow mainRow;
    private List<StatTableRow> enemyRows;
    private List<StatTableRow> friendRows;
    private bool initiated;
    private float rowHeight;
    private bool onScreen;
    private TableState state;
    private BonusBuyingInfo buyAttack;
    private BonusBuyingInfo buyReload;
    private bool bonusBought;
    private bool waitingForExit;
    private int currentBonusPrice = GameManager.BONUS_START_PRICE;
    private bool isLoading = false;

    public static int MyVehicleRank { get; set; }
    public static TableState State { get { return instance ? instance.state : TableState.None; } }
    public static bool IsEnoughPlayers { get; set; }

    void Awake()
    {
        instance = this;

        if (tableBody == null)
            tableBody = transform.Find("Body");
        if (tableBody2 == null)
            tableBody2 = transform.Find("Body2");

        counter.OnStop += StopTimer;
        gameObject.SetActive(false);
        MiscTools.SetObjectsActivity(objectsActivatedIfTeamMode, false);
        MiscTools.SetObjectsActivity(objectsActivatedIfDeathmatch, false);
        onScreen = false;

        Messenger.Subscribe(EventId.PhotonJoinedRoom, Init);
        Messenger.Subscribe(EventId.MyTankRespawned, OnMyTankRespawned);
        Messenger.Subscribe(EventId.BeforeReconnecting, BeforeReconnect);
        Messenger.Subscribe(EventId.TroubleDisconnect, OnTroubleDiconnect);
        Messenger.Subscribe(EventId.TeamScoreChanged, OnTeamScoreChanged);
    }

    void OnDestroy()
    {
        instance = null;
        OnStopTimer = null;
        scheduledShow = null;

        Messenger.Unsubscribe(EventId.PhotonJoinedRoom, Init);
        Messenger.Unsubscribe(EventId.MyTankRespawned, OnMyTankRespawned);
        Messenger.Unsubscribe(EventId.BeforeReconnecting, OnTroubleDiconnect);
        Messenger.Unsubscribe(EventId.TeamScoreChanged, OnTeamScoreChanged);
    }

    private void Init(EventId id, EventInfo info)
    {
        if (initiated) return;

        MyVehicleRank = 99;
        mainRow = CreateRow(RowType.Own, true);
        buyAttack = new BonusBuyingInfo(lblAttackBonusPrice, btnBuyBonusAttack, GameManager.BONUS_START_PRICE);
        buyReload = new BonusBuyingInfo(lblReloadBonusPrice, btnBuyBonusReload, GameManager.BONUS_START_PRICE);

        //Включаем / выключаем все специфичные для разных режимов игры объекты
        MiscTools.SetObjectsActivity(objectsActivatedIfTeamMode, BattleController.Instance.IsTeamMode);
        MiscTools.SetObjectsActivity(objectsActivatedIfDeathmatch, !BattleController.Instance.IsTeamMode);
        if (lblHeader != null)
            lblHeader.text = Localizer.GetText(GameData.GameModeLocalizationKey);

        MiscTools.CheckBtnVipState(objectsActivatedIfVip, objectsDisactivatedIfVip);
        rowHeight = mainRow.GetComponent<tk2dSlicedSprite>().dimensions.y * mainRow.GetComponent<tk2dSlicedSprite>().scale.y + rowSpacing;

        if (BattleController.Instance.IsTeamMode)
        {
            enemyRows = new List<StatTableRow>(GameData.maxPlayers / 2 + 1);
            friendRows = new List<StatTableRow>(GameData.maxPlayers / 2 + 1);
            CreateEmptyRows();
            FillRowBuffer(friendRows, RowType.Friend, true);
            FillRowBuffer(enemyRows, RowType.Enemy, false);
        }
        else
        {
            enemyRows = new List<StatTableRow>(GameData.maxPlayers);
            FillRowBuffer(enemyRows, RowType.Enemy, true);
        }

        initiated = true;
    }

    void Update()
    {
        if (OnScreen && State == TableState.AfterDeath && ProfileInfo.IsPlayerVip && btnHasten.Activated && XDevs.Input.GetButtonDown("Hasten Respawn"))
        {
            OnHasten(btnHasten.uiItem);
        }
        if (!XDevs.Input.GetButtonDown("Back"))
        {
            return;
        }

        if (OnScreen && State == TableState.BattleEnd)
            return;

        switch (state)
        {
            case TableState.Quit:
                Hide();
                return;
            case TableState.BattleEnd:
                gameObject.SetActive(false);
                return;
            case TableState.AfterDeath:
                return;
        }
    }


    /* PUBLIC SECTION */
    public static void Hide()
    {
        //!!!!!Firstly - do SetActive, secondly - send event!!!!!
        instance.onScreen = false;
        instance.counter.StopTimer();
        instance.gameObject.SetActive(false);
        Messenger.Send(EventId.StatTableVisibilityChange, new EventInfo_B(false));
        //Settings.HideSettingsBtn();
    }

    public static void Show(Dictionary<int, PlayerStat> statistics, int ownId, TableState _state)
    {
        // Дичайше закостылено для респауна при отсчёте до добровольного выхода из игры.
        scheduledShow = () =>
            {
                //!!!!!Firstly - do SetActive, secondly - send event!!!!!
                instance.gameObject.SetActive(true);
                instance.onScreen = true;
                instance._ShowTable(statistics, ownId, _state);
                Messenger.Send(EventId.StatTableVisibilityChange, new EventInfo_B(true));
            };

        if (instance.waitingForExit)
        {
            if (_state != TableState.AfterDeath)
                scheduledShow = null;

            return;
        }

        scheduledShow();
    }

    private void _ShowTable(Dictionary<int, PlayerStat> statistics, int ownId, TableState _state)
    {
        Init(EventId.PhotonJoinedRoom, null);

        state = _state;
        lblMyTeamScore.text = "";
        lblEnemyTeamScore.text = "";
        if (BattleController.Instance.IsTeamMode)
        {
            instance.FillRowsTeam(statistics, ownId);
            lblMyTeamScore.text = ScoreCounter.FriendTeamScore.ToString();
            lblEnemyTeamScore.text = ScoreCounter.EnemyTeamScore.ToString();
        }
        else
            instance.FillRowsDM(statistics, ownId);

        switch (state)
        {
            case TableState.AfterDeath:
                afterDeathPart.SetActive(true);
                battleEndPart.SetActive(false);
                quitPart.SetActive(false);
                waitingExitPart.SetActive(false);
                btnHasten.Activated = ProfileInfo.CanBuy (new ProfileInfo.Price(GameData.hastenPrice, ProfileInfo.PriceCurrency.Gold)) || ProfileInfo.IsPlayerVip;
                counter.StartTimer(GameData.respawnTime - respawnTimeCorrection, !exitCounter.IsActive);
                respawnTimeCorrection = 0; // Дичайше закостылено для респауна при отсчёте до добровольного выхода из игры.
                RefreshBonusButtons();
                //Settings.HideSettingsBtn();
                return;
            case TableState.BattleEnd:
                counter.StopTimer();
                afterDeathPart.SetActive(false);
                battleEndPart.SetActive(true);
                quitPart.SetActive(false);
                waitingExitPart.SetActive(false);
                if (statistics.Count > 1)
                    IsEnoughPlayers = true;
                //Settings.HideSettingsBtn();

                return;
            case TableState.Quit:
                afterDeathPart.SetActive(false);
                battleEndPart.SetActive(false);
                quitPart.SetActive(true);
                waitingExitPart.SetActive(false);
                //Settings.ShowSettingsBtn();
                return;
            case TableState.ExitWaiting:
                afterDeathPart.SetActive(false);
                battleEndPart.SetActive(false);
                quitPart.SetActive(false);
                waitingExitPart.SetActive(true);
                waitingForExit = true;
                exitCounter.OnStop += delegate
                {
                    Messenger.Send(EventId.PlayerFled, new EventInfo_SimpleEvent());
                    Manager.BattleServer.EndBattle(BattleConnectManager.NORMAL_DISCONNECT_CAUSE);
                    BattleController.ExitToHangar(true);
                };
                exitCounter.StartTimer(Mathf.Min(BattleController.TimeRemaining, GameData.respawnTime), true);
                return;
        }
    }

    private void QuitCancel()
    {
        // Дичайше закостылено для респауна при отсчёте до добровольного выхода из игры.

        respawnTimeCorrection = exitCounter.CountAtStart - exitCounter.CurrentCount;

        exitCounter.StopTimer();

        waitingForExit = false;

        Hide();

        if (!BattleController.MyVehicle.IsAvailable && scheduledShow != null)
            scheduledShow();
    }

    public static void Refresh(Dictionary<int, PlayerStat> statistics, int ownId)
    {
        if (BattleController.Instance.IsTeamMode)
            instance.FillRowsTeam(statistics, ownId);
        else
            instance.FillRowsDM(statistics, ownId);
    }

    public static bool OnScreen
    {
        get
        {
            if (instance == null)
                return false;

            return instance.onScreen;
        }
    }

    /* PRIVATE SECTION */
    private StatTableRow CreateRow(RowType rowType, bool firstBody)
    {
        StatTableRow newRow = null;
        switch (rowType)
        {
            case RowType.Own:
                newRow = (StatTableRow)Instantiate(ownRowPrefab);
                break;
            case RowType.Friend:
                newRow = (StatTableRow)Instantiate(friendRowPrefab);
                break;
            case RowType.Enemy:
                newRow = (StatTableRow)Instantiate(otherRowPrefab);
                break;
        }

        newRow.transform.parent = firstBody ? tableBody : tableBody2;
        newRow.gameObject.SetActive(false);

        return newRow;
    }

    private void CreateEmptyRows()
    {
        friendEmptyRows = new GameObject[GameData.maxPlayers / 2];
        enemyEmptyRows = new GameObject[GameData.maxPlayers / 2];
        for (int i = 0; i < GameData.maxPlayers / 2; i++)
        {
            friendEmptyRows[i] = (GameObject)Instantiate(friendEmptyRow);
            friendEmptyRows[i].transform.parent = tableBody;
            friendEmptyRows[i].transform.localPosition = Vector3.down * i * rowHeight;
            enemyEmptyRows[i] = (GameObject)Instantiate(enemyEmptyRow);
            enemyEmptyRows[i].transform.parent = tableBody2;
            enemyEmptyRows[i].transform.localPosition = Vector3.down * i * rowHeight;
        }
    }

    private void FillRowBuffer(List<StatTableRow> rows, RowType type, bool firstBody)
    {
        for (int i = 0; i < rows.Capacity; i++)
            rows.Add(CreateRow(type, firstBody));
    }

    private void FillEmptyRows(GameObject[] fillers, int fromRank)
    {
        for (int i = 0; i < fillers.Length; i++)
        {
            if (fillers[i])
                fillers[i].SetActive(i > fromRank - 1);
        }
    }

    private int FillRowsSpecial(List<PlayerStat> statistics, ref List<StatTableRow> rows, int count, ref GameObject[] fillers, bool hasOwnStat)
    {
        int rank = 0;
        int teamId = hasOwnStat ? BattleController.MyVehicle.data.teamId : 1 - BattleController.MyVehicle.data.teamId;
        int ownId = BattleController.MyPlayerId;
        bool mainInTable = false;
        int otherCount = 0;
        int statTableRowNum = 0;

        foreach (PlayerStat stat in statistics)
        {
            if (stat.teamId != teamId)
                continue;

            StatTableRow row;
            rank++;
            if (stat.playerId == ownId)
            {
                row = mainRow;
                mainInTable = true;
                if (state == TableState.BattleEnd)
                {
                    MyVehicleRank = rank;
                }
            }
            else
            {
                if (otherCount < (hasOwnStat ? count - 1 : count))
                {
                    otherCount++;
                    row = rows[otherCount - 1];
                }
                else
                {
                    if (!mainInTable)
                        continue;
                    break;
                }
            }

            row.Premium = stat.vip;
            row.flagGroup.SetFlag(stat);
            row.AlignGoldRushIconIfNeeded();

            row.PlayerName = stat.playerName;
            row.ClanName = stat.clanName ?? "";
            row.Rank = rank;
            row.Score = stat.score;
            row.Kills = stat.kills;
            row.Deaths = stat.deaths;
            row.transform.localPosition = Vector3.down * (rank <= count ? rank - 1 : count - 1) * rowHeight;
            row.flagGroup.Offender = (stat.playerId == BattleController.LastOffender);
            row.gameObject.SetActive(true);
            row.sprGoldRushLeader.gameObject.SetActive(GoldRush.Leader == stat.playerId);
            //Для чужих строчек таблицы меняем фон через строчку
            if (stat.profileId != ProfileInfo.profileId)
            {
                row.SetupInterlacedBackgrounds(statTableRowNum);
            }

            statTableRowNum++;
        }

        return otherCount;
    }

    private void ClearRows(List<StatTableRow> rows)
    {
        for (int i = 0; i < rows.Count; i++)
        {
            rows[i].flagGroup.Offender = false;
            rows[i].gameObject.SetActive(false);
        }
    }

    private void FillRowsTeam(Dictionary<int, PlayerStat> statistics, int ownId)
    {
        ClearRows(friendRows);
        ClearRows(enemyRows);

        var statSorted =
            from playerStat in statistics.Values
            .OrderBy(x => x.teamId)
            .ThenByDescending(x => x.score)
            .ThenByDescending(x => x.kills)
            .ThenBy(x => x.deaths)
            select playerStat;

        List<PlayerStat> statSortedList = statSorted.ToList();
        int enemyCount = 0, friendCount = 0;
        friendCount = FillRowsSpecial(statSortedList, ref friendRows, GameData.maxPlayers / 2, ref friendEmptyRows, true);
        enemyCount = FillRowsSpecial(statSortedList, ref enemyRows, GameData.maxPlayers / 2, ref enemyEmptyRows, false);

        FillEmptyRows(friendEmptyRows, friendCount);
        FillEmptyRows(enemyEmptyRows, enemyCount);
    }

    private void FillRowsDM(Dictionary<int, PlayerStat> statistics, int ownId)
    {
        List<PlayerStat> statSorted =
            statistics.Select(x => x.Value)
            .OrderByDescending(x => x.score)
            .ThenByDescending(x => x.kills)
            .ThenBy(x => x.deaths)
            .ToList();

        GameObject[] temp = new GameObject[0];
        ClearRows(enemyRows);
        FillRowsSpecial(statSorted, ref enemyRows, GameData.maxPlayers, ref temp, true);
    }

    private void StopTimer()
    {
        if (OnStopTimer != null)
            OnStopTimer();
    }

    private void RefreshBonusButtons()
    {
        buyAttack.Blocked = false;
        buyReload.Blocked = false;
        buyAttack.Price = currentBonusPrice;
        buyReload.Price = currentBonusPrice;
    }

    private void SetLoading (bool flag = true)
    {
        isLoading = flag;
        buyAttack.Loading = isLoading;
        buyReload.Loading = isLoading;
        btnHasten.Activated = !flag;
    }

    //Key presses
    private void OnHasten(tk2dUIItem item)
    {
        //if (!ProfileInfo.IsPlayerVip)
        //    ProfileInfo.WriteOffBalance (new ProfileInfo.Price (GameData.hastenPrice, ProfileInfo.PriceCurrency.Gold));
        SetLoading ();
        Http.Manager.BattleServer.BuyHastenRespawn (delegate (bool result) {
            SetLoading (false);
            if (result) {
                counter.MoveToEnd ();
            }

#region Google Analytics: respawn hasten bought

            GoogleAnalyticsWrapper.LogEvent(
                new CustomEventHitBuilder()
                    .SetParameter(GAEvent.Category.RespawnHastenBuying)
                    .SetParameter<GAEvent.Action>()
                    .SetSubject(GAEvent.Subject.MapName, GameManager.CurrentMap)
                    .SetParameter<GAEvent.Label>()
                    .SetSubject(GAEvent.Subject.VehicleID, ProfileInfo.currentVehicle)
                    .SetValue(ProfileInfo.Level));

#endregion
        });
    }

    private void OnBuyBonusClick(tk2dUIItem item)
    {
        if (isLoading) {
            return;
        }
        if (!item.name.StartsWith("btnBuyBonus"))
        {
            Debug.Log("Incorrect button name for buying bonus", item.gameObject);
            return;
        }

        string bonusName = item.name.Substring(11);
        BonusItem.BonusType bonusType;
        BonusBuyingInfo bought;

        switch (bonusName)
        {
            case "Attack":
                bonusType = BonusItem.BonusType.Attack;
                bought = buyAttack;
                break;
            //case "RocketAttack": // TODO: такого респаун-бонуса нет в игре. Кейс добавлен на всякий случай.
            //    bonusType = BonusItem.BonusType.RocketAttack;
            //    bought = buyAttack;
            //    other = buyReload;
            //    break;
            case "Reload":
                bonusType = BonusItem.BonusType.Reload;
                bought = buyReload;
                break;
            default:
                DT.LogError(item.gameObject, "Unknown bonus type from button name ({0} = {1})", item.name, bonusName);
                return;
        }

        SetLoading ();

        Http.Manager.BattleServer.BuyRespawnBonus (bonusType, bought.Price,
            delegate (Http.Response result) {
                SetLoading (false);

                bonusBought = true;
                BattleController.RememberBonus(bonusType, currentBonusPrice);
                bought.Blocked = true;

                currentBonusPrice = (int)(currentBonusPrice * GameManager.BONUS_PRICE_INCREASE_RATIO);

                buyAttack.Price = currentBonusPrice;
                buyReload.Price = currentBonusPrice;

#region Google Analytics: booster bought

                GoogleAnalyticsWrapper.LogEvent(
                    new CustomEventHitBuilder()
                        .SetParameter(GAEvent.Category.RespawnBonusBuying)
                        .SetParameter<GAEvent.Action>()
                        .SetSubject(GAEvent.Subject.MapName, GameManager.CurrentMap)
                        .SetParameter<GAEvent.Label>()
                        .SetSubject(GAEvent.Subject.BonusType, bonusType)
                        .SetValue(ProfileInfo.Level));

                GoogleAnalyticsWrapper.LogEvent(
                    new CustomEventHitBuilder()
                        .SetParameter(GAEvent.Category.RespawnBonusBuying)
                        .SetParameter<GAEvent.Action>()
                        .SetSubject(GAEvent.Subject.MapName, GameManager.CurrentMap)
                        .SetParameter<GAEvent.Label>()
                        .SetSubject(GAEvent.Subject.VehicleID, ProfileInfo.currentVehicle)
                        .SetValue(ProfileInfo.Level));

#endregion
            },
            delegate (Http.Response result) {
                SetLoading (false);
            }
        );

    }

    private void OnMyTankRespawned(EventId id, EventInfo info)
    {
        if (!bonusBought)
            return;

        RefreshBonusButtons();

        bonusBought = false;
    }

    private void BeforeReconnect(EventId id, EventInfo info)
    {
        Hide();
    }

    private void OnTroubleDiconnect(EventId eid, EventInfo ei)
    {
        Hide();
    }

    private void OnBackToGameClick(tk2dUIItem item)
    {
        Hide();
    }

    private void OnExitToHangarClick()
    {
        Show(BattleController.GameStat, BattleController.MyPlayerId, TableState.ExitWaiting);

        if (TopPanelValues.CriticalTime)
            TopPanelValues.ShowCriticalTime(false);
    }

    private void OnEndClick()
    {
        gameObject.SetActive(false);
        BattleController.ExitToHangar(true);
    }
    public void OnShareButtonPress()
    {
        StartCoroutine(Share());
        Debug.LogWarning("Share button pressed");
    }

    private IEnumerator Share()
    {
        yield return new WaitForEndOfFrame();
        SocialSettings.GetSocialService().Post(Localizer.GetText("textBattleRatingPost"), MiscTools.GetScreenshot());
    }

    private void OnTeamScoreChanged(EventId eid, EventInfo ei)
    {
        lblMyTeamScore.text = ScoreCounter.FriendTeamScore.ToString();
        lblEnemyTeamScore.text = ScoreCounter.EnemyTeamScore.ToString();
    }
}