using System;
using System.Collections.Generic;
using System.Linq;
using CodeStage.AntiCheat.ObscuredTypes;
using UnityEngine;

public class ModuleShop : Shop<ShopItemCell>
{
    [Serializable]
    public class UpgradeButtons
    {
        public tk2dUIItem btnItself;
        public tk2dTextMesh lblModuleLevel;

        public tk2dSprite SprUpgrade { get; set; }
    }

    public tk2dUIToggleButtonGroup moduleToggleGroup;
    public tk2dSprite sprUpgradeArrow;

    public UpgradeButtons btnCannon;
    public UpgradeButtons btnReloader;
    public UpgradeButtons btnArmor;
    public UpgradeButtons btnEngine;
    public UpgradeButtons btnTracks;

    [SerializeField]
    private bool changeUpgradeSpriteColorInsteadOfChangingSprite = false;//чтобы не менять спрайт на strelka / strlka red, а менять цвет
    [SerializeField]
    private Color moduleIndicatorColor = Color.white;// вместо спрайта strelka
    [SerializeField]
    private Color awaitedModuleIndicatorColor = Color.white;//вместо спрайта strelka red


    private static double upgradeRamainingTimeSec;

    private readonly List<GameObject> btnUpgradeArrows = new List<GameObject>();

    private bool watchDelivery;

    public TankModuleInfos.Module ModuleInView { get; private set; }
    public static ModuleShop Instance { get; private set; }

    public override string GuiPageName
    {
        get { return "Armory"; }
    }

    public override string OnToggleMethodName
    {
        get { throw new System.NotImplementedException(); }
    }

    protected override Shop.ItemType ComparingMode
    {
        get
        {
            return Shop.ItemType.Vehicle |
                   Shop.ItemType.Module;
        }
    }

    protected override IShopItem[] ShopItems
    {
        get { throw new System.NotImplementedException(); }
    }

    void Awake()
    {
        Instance = this;

        InstantiateUpgradeArrow(btnArmor);
        InstantiateUpgradeArrow(btnCannon);
        InstantiateUpgradeArrow(btnEngine);
        InstantiateUpgradeArrow(btnTracks);
        InstantiateUpgradeArrow(btnReloader);

        Dispatcher.Subscribe(EventId.ProfileInfoLoadedFromServer, ProfileLoadedFromServer);

        HangarModuleWindow.OnLevelChange += OnModuleLevelChanged;
        GUIPager.OnPageChange += OnPageChange;

        gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.ProfileInfoLoadedFromServer, ProfileLoadedFromServer);

        HangarModuleWindow.OnLevelChange -= OnModuleLevelChanged;
        GUIPager.OnPageChange -= OnPageChange;

        Instance = null;
    }

    void OnApplicationPause(bool paused)
    {
        if (paused)
            return;

        CheckIfModuleUpgradePossible();
        ShowUpgradeArrowsOnEachModule();
    }

    protected override void OnEnable()
    {
        GUIController.ListenButtonClick("btnBuy", OnBuyClick);
        GUIController.ListenButtonClick("btnDeliver", OnDeliverClick);

        HangarController.Instance.RecalcMaxStats(ComparingMode);

        ModuleClick("btnCannon", true);
        FocusOnAwaitedModule();
        ShowUpgradeArrowsOnEachModule();
    }

    protected override void OnDisable()
    {
        GUIController.RemoveButtonClickListener("btnBuy", OnBuyClick);
        GUIController.RemoveButtonClickListener("btnDeliver", OnDeliverClick);
    }

    private void OnModuleInFocusChange(tk2dUIToggleButtonGroup buttonGroup)
    {
        ModuleClick(buttonGroup.SelectedToggleButton.name, false);
    }

    private void ProfileLoadedFromServer(EventId id, EventInfo info)
    {
        if (ModuleInView != null)
            ModuleClick("btn" + ModuleInView.type, false);
    }

    private void ModuleClick(string buttonName, bool clickSimulate)
    {
        int level;
        int maxLevel;
        int index = -1;

        switch (buttonName)
        {
            case "btnCannon":
                ModuleInView = TankModules.cannon;
                index = 0;
                break;
            case "btnReloader":
                ModuleInView = TankModules.reloader;
                index = 1;
                break;
            case "btnArmor":
                ModuleInView = TankModules.armor;
                index = 2;
                break;
            case "btnEngine":
                ModuleInView = TankModules.engine;
                index = 3;
                break;
            case "btnTracks":
                ModuleInView = TankModules.tracks;
                index = 4;
                break;
            default:
                return;
        }

        level = Shop.CurrentVehicle.GetModuleLevel(ModuleInView.type) + 1;
        maxLevel = Mathf.Clamp(level, 1, Shop.CurrentVehicle.Info.GetMaxUpgradeLevel(ModuleInView.type));
        //DT3.Log("Shop.CurrentVehicle.info.GetMaxUpgradeLevel(moduleInView.type) = {0}, level = {1}, maxLevel = {2}", Shop.CurrentVehicle.info.GetMaxUpgradeLevel(moduleInView.type), level, maxLevel);

        if (clickSimulate)
            moduleToggleGroup.SelectedIndex = index;

        if (level == 1)
            maxLevel = 1;

        HangarModuleWindow.SetData(ModuleInView.type, level, maxLevel);

        if (Shop.CurrentVehicle.Upgrades.awaitedModule == ModuleInView.type)
        {
            HangarController.OnTimerTick -= ShowDeliveryInfo; // Защита от повторной подписки.
            HangarController.OnTimerTick += ShowDeliveryInfo;
            watchDelivery = true;
        }
        else if (watchDelivery)
        {
            HangarController.OnTimerTick -= ShowDeliveryInfo;
            watchDelivery = false;
        }

        HangarCameraController.Instance.OnModuleClicked();
    }

    private void FocusOnAwaitedModule()
    {
        switch (ProfileInfo.vehicleUpgrades[ProfileInfo.CurrentVehicle].awaitedModule)
        {
            case TankModuleInfos.ModuleType.Cannon:
                btnCannon.btnItself.SimulateClick();
                break;
            case TankModuleInfos.ModuleType.Reloader:
                btnReloader.btnItself.SimulateClick();
                break;
            case TankModuleInfos.ModuleType.Armor:
                btnArmor.btnItself.SimulateClick();
                break;
            case TankModuleInfos.ModuleType.Engine:
                btnEngine.btnItself.SimulateClick();
                break;
            case TankModuleInfos.ModuleType.Tracks:
                btnTracks.btnItself.SimulateClick();
                break;
        }
    }

    public void ShowUpgradeArrowsOnEachModule()
    {
        if (Shop.CurrentVehicle == null)
            return;

        foreach (var btn in btnUpgradeArrows)
            btn.SetActive(false);

        if (Shop.CurrentVehicle.Upgrades.awaitedModule != TankModuleInfos.ModuleType.None)
        {
            UpgradeButtons button = null;
            switch (Shop.CurrentVehicle.Upgrades.awaitedModule)
            {
                case TankModuleInfos.ModuleType.Armor:
                    button = btnArmor;
                    break;
                case TankModuleInfos.ModuleType.Cannon:
                    button = btnCannon;
                    break;
                case TankModuleInfos.ModuleType.Engine:
                    button = btnEngine;
                    break;
                case TankModuleInfos.ModuleType.Reloader:
                    button = btnReloader;
                    break;
                case TankModuleInfos.ModuleType.Tracks:
                    button = btnTracks;
                    break;
            }

            button.SprUpgrade.gameObject.SetActive(true);
            UpdateArrowSprite(button.SprUpgrade, isAwaitingModule: true);

            return;
        }
        foreach (var moduleLevel in Shop.CurrentVehicle.Upgrades.ModuleLevels)
        {
            if (moduleLevel.Value < Shop.CurrentVehicle.Info.GetMaxUpgradeLevel(moduleLevel.Key))
            {
                UpgradeButtons button = null;

                switch (moduleLevel.Key)
                {
                    case TankModuleInfos.ModuleType.Armor:
                        button = btnArmor;
                        break;
                    case TankModuleInfos.ModuleType.Cannon:
                        button = btnCannon;
                        break;
                    case TankModuleInfos.ModuleType.Engine:
                        button = btnEngine;
                        break;
                    case TankModuleInfos.ModuleType.Reloader:
                        button = btnReloader;
                        break;
                    case TankModuleInfos.ModuleType.Tracks:
                        button = btnTracks;
                        break;
                }

                button.SprUpgrade.gameObject.SetActive(true);
                UpdateArrowSprite(button.SprUpgrade, isAwaitingModule: false);
            }
        }
    }

    private void UpdateArrowSprite(tk2dBaseSprite arrow, bool isAwaitingModule = false)
    {
        if (changeUpgradeSpriteColorInsteadOfChangingSprite)
            arrow.color = isAwaitingModule ? awaitedModuleIndicatorColor : moduleIndicatorColor;
        else
            arrow.SetSprite(isAwaitingModule ? "strelka red" : "strelka");
    }

    public void CheckIfModuleUpgradePossible()
    {
        if (Shop.CurrentVehicle == null)
            return;

        sprUpgradeArrow.gameObject.SetActive(true);
        UpdateArrowSprite(sprUpgradeArrow, isAwaitingModule: false);

        if (Shop.CurrentVehicle.Upgrades.awaitedModule != TankModuleInfos.ModuleType.None)
        {
            UpdateArrowSprite(sprUpgradeArrow, isAwaitingModule: true);
            return;
        }

        if (Shop.CurrentVehicle.Upgrades.ModuleLevels.Any(moduleLevel => moduleLevel.Value < Shop.CurrentVehicle.Info.GetMaxUpgradeLevel(moduleLevel.Key)))
            return;

        sprUpgradeArrow.gameObject.SetActive(false);
    }

    private void OnModuleLevelChanged(TankModuleInfos.ModuleType type, int level)
    {
        Dictionary<VehicleInfo.VehicleParameter, ObscuredFloat> primaryParameters = Shop.CurrentVehicle.GetRealParameters(ignoreCamo: true, ignoreDecal: true);
        Dictionary<VehicleInfo.VehicleParameter, ObscuredFloat> secondaryParameters = Shop.CurrentVehicle.GetParameters(type, level);

        HangarController.Instance.FillParameterDelta(
            bar:    HangarController.Instance.armorBar,
            max:    HangarController.Instance.ArmorMax[Shop.CurrentVehicle.Info.vehicleGroup],
            prim:   primaryParameters[VehicleInfo.VehicleParameter.Armor],
            sec:    secondaryParameters[VehicleInfo.VehicleParameter.Armor]);

        HangarController.Instance.FillParameterDelta(
            bar:    HangarController.Instance.attackBar,
            max:    HangarController.Instance.DamageMax[Shop.CurrentVehicle.Info.vehicleGroup],
            prim:   primaryParameters[VehicleInfo.VehicleParameter.Damage],
            sec:    secondaryParameters[VehicleInfo.VehicleParameter.Damage]);

        HangarController.Instance.FillParameterDelta(
            bar:    HangarController.Instance.rocketAttackBar,
            max:    HangarController.Instance.RocketDamageMax[Shop.CurrentVehicle.Info.vehicleGroup],
            prim:   primaryParameters[VehicleInfo.VehicleParameter.RocketDamage],
            sec:    secondaryParameters[VehicleInfo.VehicleParameter.RocketDamage]);

        HangarController.Instance.FillParameterDelta(
            bar:    HangarController.Instance.speedBar,
            max:    HangarController.Instance.SpeedMax[Shop.CurrentVehicle.Info.vehicleGroup],
            prim:   primaryParameters[VehicleInfo.VehicleParameter.Speed],
            sec:    secondaryParameters[VehicleInfo.VehicleParameter.Speed]);

        HangarController.Instance.FillParameterDelta(
            bar:    HangarController.Instance.rofBar,
            max:    HangarController.Instance.ROFMax[Shop.CurrentVehicle.Info.vehicleGroup],
            prim:   primaryParameters[VehicleInfo.VehicleParameter.RoF],
            sec:    secondaryParameters[VehicleInfo.VehicleParameter.RoF]);

        HangarController.Instance.FillParameterDelta(
            bar:    HangarController.Instance.ircmRofBar,
            max:    HangarController.Instance.IRCMROFMax[Shop.CurrentVehicle.Info.vehicleGroup],
            prim:   primaryParameters[VehicleInfo.VehicleParameter.IRCMRoF],
            sec:    secondaryParameters[VehicleInfo.VehicleParameter.IRCMRoF]);

        //В цикле бы сделать, да чета нету ни одной коллекции на эту тему
        //DT3.LogError("Update Modules Level");
        btnCannon.lblModuleLevel.text = (Shop.CurrentVehicle.GetModuleLevel(TankModuleInfos.ModuleType.Cannon)).ToString();
        btnReloader.lblModuleLevel.text = (Shop.CurrentVehicle.GetModuleLevel(TankModuleInfos.ModuleType.Reloader)).ToString();
        btnArmor.lblModuleLevel.text = (Shop.CurrentVehicle.GetModuleLevel(TankModuleInfos.ModuleType.Armor)).ToString();
        btnEngine.lblModuleLevel.text = (Shop.CurrentVehicle.GetModuleLevel(TankModuleInfos.ModuleType.Engine)).ToString();
        btnTracks.lblModuleLevel.text = (Shop.CurrentVehicle.GetModuleLevel(TankModuleInfos.ModuleType.Tracks)).ToString();

        int currentLevel = Shop.CurrentVehicle.GetModuleLevel(type);

        if (Shop.CurrentVehicle.Upgrades.awaitedModule == type && level == currentLevel + 1)
        {
            HangarController.Instance.SetActionBoxType(HangarController.ActionBoxType.Deliver);
            ShowDeliveryInfo(GameData.CurrentTimeStamp);

            return;
        }

        if (level <= currentLevel)
        {
            HangarController.Instance.SetActionBoxType(HangarController.ActionBoxType.Installed);
            return;
        }

        switch (type)
        {
            case TankModuleInfos.ModuleType.Armor:
                ModuleInView = TankModules.armor;
                break;
            case TankModuleInfos.ModuleType.Cannon:
                ModuleInView = TankModules.cannon;
                break;
            case TankModuleInfos.ModuleType.Engine:
                ModuleInView = TankModules.engine;
                break;
            case TankModuleInfos.ModuleType.Reloader:
                ModuleInView = TankModules.reloader;
                break;
            case TankModuleInfos.ModuleType.Tracks:
                ModuleInView = TankModules.tracks;
                break;
        }

        HangarController.Instance.SetActionBoxType(HangarController.ActionBoxType.Buy);

        BuyingBox.Price = new ProfileInfo.Price(ModuleInView.GetPrice(level), ProfileInfo.PriceCurrency.Silver);
    }

    private void OnPageChange(string from, string to)
    {
        if (from == GuiPageName && to != "Bank" && to != from)
            ModuleInView = null;
    }

    private void ShowDeliveryInfo(double timestamp)
    {
        VehicleUpgrades upgrades = Shop.CurrentVehicle.Upgrades;

        int level = Shop.CurrentVehicle.Upgrades.GetModuleLevel(upgrades.awaitedModule) + 1;

        upgradeRamainingTimeSec = upgrades.moduleReadyTime - GameData.CurrentTimeStamp;

        DeliverBox.Price
            = new ProfileInfo.Price(
                TankModuleInfos.GetQuickDeliveryPrice((float)upgradeRamainingTimeSec / 60),
                ProfileInfo.PriceCurrency.Gold);
        DeliverBox.Remain = (long)upgradeRamainingTimeSec;
        DeliverBox.Progress = (float)(1 - upgradeRamainingTimeSec / TankModuleInfos.Instance.inventionTime[level - 1] / 60);
    }

    private void InstantiateUpgradeArrow(UpgradeButtons upgradesButtons)
    {
        upgradesButtons.SprUpgrade = Instantiate(sprUpgradeArrow);

        if (upgradesButtons.SprUpgrade == null)
            return;

        upgradesButtons.SprUpgrade.transform.parent = upgradesButtons.btnItself.transform;
        upgradesButtons.SprUpgrade.transform.localPosition = sprUpgradeArrow.transform.localPosition;
        upgradesButtons.SprUpgrade.gameObject.SetActive(false);
        upgradesButtons.SprUpgrade.gameObject.name = "spr" + upgradesButtons.btnItself.name + "UpgradeArrow";

        btnUpgradeArrows.Add(upgradesButtons.SprUpgrade.gameObject);
    }

    private void OnBuyClick(string buttonName)
    {
        if (HangarController.Instance.isWaitingForSaving)
            return;

        if (ModuleInView == null)
            return;

        if (Shop.CurrentVehicle.Upgrades.awaitedModule != TankModuleInfos.ModuleType.None)
        {
            MessageBox.Show(new MessageBox.Data(MessageBox.Type.Info, Localizer.GetText("WaitForModule"), FocusOnAwaitedModule));
            return;
        }

        ProfileInfo.Price price
            = new ProfileInfo.Price(
                ModuleInView.GetPrice(HangarModuleWindow.Level),
                ProfileInfo.PriceCurrency.Silver);

        if (!ProfileInfo.CanBuy(price))
        {
            HangarController.Instance.GoToBank(Bank.CurrencyToTab(price.currency), voiceRequired: true);

            #region Google Analytics: module buying failure "not enough money"

            GoogleAnalyticsWrapper.LogEvent(
                new CustomEventHitBuilder()
                    .SetParameter(GAEvent.Category.ModuleBuying)
                    .SetParameter(GAEvent.Action.NotEnoughMoney)
                    .SetSubject(GAEvent.Subject.VehicleID, Shop.CurrentVehicle.Info.id)
                    .SetParameter<GAEvent.Label>()
                    .SetSubject(GAEvent.Subject.ModuleType, ModuleInView.type.ToString())
                    .SetValue(ProfileInfo.Level));

            #endregion

            return;
        }

        BuyModule(
            userVehicle:    Shop.CurrentVehicle,
            module:         ModuleInView.type,
            finishCallback: delegate(Http.Response response, bool result)
                            {
                                if (result)
                                {
                                    HangarController.Instance.SetActionBoxType(HangarController.ActionBoxType.Deliver);
                                    ShowDeliveryInfo(GameData.CurrentTimeStamp);
                                    //DeliverBox.UpdateLblTimeRemainFrame();
                                    HangarController.OnTimerTick -= ShowDeliveryInfo;//Защита от повторной подписки
                                    HangarController.OnTimerTick += ShowDeliveryInfo;

                                    watchDelivery = true;

                                    HangarController.Instance.PlaySound(HangarController.Instance.buyingSound);

                                    ShowUpgradeArrowsOnEachModule();

                                    #region Google Analytics: module bought

                                    GoogleAnalyticsWrapper.LogEvent(
                                        new CustomEventHitBuilder()
                                            .SetParameter(GAEvent.Category.ModuleBuying)
                                            .SetParameter(GAEvent.Action.Bought)
                                            .SetSubject(GAEvent.Subject.VehicleID, Shop.CurrentVehicle.Info.id.ToString())
                                            .SetParameter<GAEvent.Label>()
                                            .SetSubject(GAEvent.Subject.ModuleType, Shop.CurrentVehicle.Upgrades.awaitedModule.ToString())
                                            .SetValue(ProfileInfo.Level));

                                    #endregion
                                }
                                else
                                {
                                    MessageBox.Show(new MessageBox.Data(MessageBox.Type.Info, Localizer.GetText("ApplicationError", response.ServerError), FocusOnAwaitedModule));
                                }
                            });
    }

    private void OnDeliverClick(string buttonName)
    {
        if (HangarController.Instance.isWaitingForSaving)
            return;

        if (!ProfileInfo.CanBuy(DeliverBox.Price))
        {
            HangarController.Instance.GoToBank(Bank.CurrencyToTab(DeliverBox.Price.currency), voiceRequired: true);

            #region Google Analytics: module delivery failure "not enough money"

            GoogleAnalyticsWrapper.LogEvent(
                new CustomEventHitBuilder()
                    .SetParameter(GAEvent.Category.ModuleDeliveryBuying)
                    .SetParameter(GAEvent.Action.NotEnoughMoney)
                    .SetSubject(GAEvent.Subject.VehicleID, Shop.CurrentVehicle.Info.id)
                    .SetParameter<GAEvent.Label>()
                    .SetSubject(GAEvent.Subject.ModuleType, ModuleInView.type.ToString())
                    .SetValue(ProfileInfo.Level));

            #endregion

            return;
        }

        HangarController.OnTimerTick -= Shop.CurrentVehicle.ModuleReceiving;

        TankModuleInfos.ModuleType moduleType = ModuleInView.type;

        DeliverModule(
            userVehicle:    Shop.CurrentVehicle,
            finishCallback: delegate(Http.Response response, bool result)
                            {
                                if (!result)
                                    return;

                                ModuleReceived(Shop.CurrentVehicle, moduleType);

                                #region Google Analytics: module delivery

                                GoogleAnalyticsWrapper.LogEvent(
                                    new CustomEventHitBuilder()
                                        .SetParameter(GAEvent.Category.ModuleDeliveryBuying)
                                        .SetParameter(GAEvent.Action.Bought)
                                        .SetSubject(GAEvent.Subject.VehicleID, Shop.CurrentVehicle.Info.id.ToString())
                                        .SetParameter<GAEvent.Label>()
                                        .SetSubject(GAEvent.Subject.ModuleType, moduleType.ToString())
                                        .SetValue(ProfileInfo.Level));

                                #endregion
                            });
    }

    public void ModuleReceived(UserVehicle vehicle, TankModuleInfos.ModuleType moduleType)
    {
        if (Shop.CurrentVehicle.Info.id == vehicle.Info.id &&
            ModuleInView != null &&
            ModuleInView.type == moduleType)
        {
            ModuleClick(String.Format("btn{0}", moduleType), true);
        }

        CheckIfModuleUpgradePossible();
        ShowUpgradeArrowsOnEachModule();

        Dispatcher.Send(EventId.ModuleReceived, null);

        HangarController.Instance.PlaySound(HangarController.Instance.moduleInstallSound);
    }

    private void BuyModule(UserVehicle userVehicle, TankModuleInfos.ModuleType module, Action<Http.Response, bool> finishCallback)
    {
        HangarController.Instance.isWaitingForSaving = true;
        BuyingBox.SetButtonActivated(false);

        var request = Http.Manager.Instance().CreateRequest("/shop/buyModule");

        request.Form.AddField("tankId", (int)userVehicle.Info.id);
        request.Form.AddField("module", module.ToString());

        Http.Manager.StartAsyncRequest(
            request:            request,
            successCallback:    delegate(Http.Response result)
                                {
                                    if (HangarController.Instance == null)
                                        return;

                                    HangarController.Instance.isWaitingForSaving = false;
                                    BuyingBox.SetButtonActivated();
                                    finishCallback(result, true);
                                    Dispatcher.Send(EventId.ModuleBought, new EventInfo_I(HangarModuleWindow.Level));  
                                },
            failCallback:       delegate(Http.Response result)
                                {
                                    if (HangarController.Instance == null)
                                        return;

                                    HangarController.Instance.isWaitingForSaving = false;
                                    BuyingBox.SetButtonActivated();
                                    finishCallback(result, false);
                                });
    }

    private void DeliverModule(UserVehicle userVehicle, Action<Http.Response, bool> finishCallback)
    {
        HangarController.Instance.isWaitingForSaving = true;
        BuyingBox.SetButtonActivated(false);

        var request = Http.Manager.Instance().CreateRequest("/shop/deliverModule");
        request.Form.AddField("tankId", (int)userVehicle.Info.id);

        Http.Manager.StartAsyncRequest(
            request:            request,
            successCallback:    delegate(Http.Response result)
                                {
                                    if (HangarController.Instance == null)
                                        return;

                                    HangarController.Instance.isWaitingForSaving = false;
                                    BuyingBox.SetButtonActivated();
                                    finishCallback(result, true);
                                },
            failCallback:       delegate(Http.Response result)
                                {
                                    if (HangarController.Instance == null)
                                        return;

                                    HangarController.Instance.isWaitingForSaving = false;
                                    BuyingBox.SetButtonActivated();
                                    finishCallback(result, false);
                                });
    }
}
