using System.Linq;
using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;

public class UserVehicle
{
    private readonly VehicleInfo info;
    private readonly VehicleUpgrades upgrades;
    private readonly Dictionary<VehicleInfo.VehicleParameter, ObscuredFloat> parameters;

    public UserVehicle(VehicleInfo info, VehicleUpgrades upgrades = null)
    {
        this.info = info;
        this.upgrades = upgrades ?? new VehicleUpgrades(info);
        parameters = GetRealParameters();
    }

    public UserVehicle(VehicleInfo info, HangarVehicle hangarVehicle, VehicleUpgrades upgrades = null)
    {
        this.info = info;
        this.upgrades = upgrades ?? new VehicleUpgrades(hangarVehicle.Info);
        HangarVehicle = hangarVehicle;

        BodykitController = hangarVehicle.gameObject.GetComponent<BodykitController>();
        parameters = GetRealParameters();

        ExpiredCamouflageRemoving();
        ExpiredDecalRemoving();

        if (this.upgrades.awaitedModule != TankModuleInfos.ModuleType.None)
            HangarController.OnTimerTick += ModuleReceiving;

        if (this.upgrades.OwnedCamouflages.Any())
            HangarController.OnTimerTick += CamouflageRemoving;

        if (this.upgrades.OwnedDecals.Any())
            HangarController.OnTimerTick += DecalRemoving;
    }

    ~UserVehicle()
    {
        HangarController.OnTimerTick -= ModuleReceiving;
        HangarController.OnTimerTick -= CamouflageRemoving;
        HangarController.OnTimerTick -= DecalRemoving;
    }

    public float Armor
    {
        get { return parameters[VehicleInfo.VehicleParameter.Armor]; }
    }

    public float Damage
    {
        get { return parameters[VehicleInfo.VehicleParameter.Damage]; }
    }

    public float RocketDamage
    {
        get { return parameters[VehicleInfo.VehicleParameter.RocketDamage]; }
    }

    public float Speed
    {
        get { return parameters[VehicleInfo.VehicleParameter.Speed]; }
    }

    public float RoF
    {
        get { return parameters[VehicleInfo.VehicleParameter.RoF]; }
    }

    public float IRCMRoF
    {
        get { return parameters[VehicleInfo.VehicleParameter.IRCMRoF]; }
    }

    public HangarVehicle HangarVehicle
    {
        get; private set;
    }

    public VehicleInfo Info
    {
        get { return info; }
    }

    public VehicleUpgrades Upgrades
    {
        get { return upgrades; }
    }

    public BodykitController BodykitController
    {
        get; private set;
    }

    public static bool operator !=(UserVehicle tank1, UserVehicle tank2)
    {
        return !(tank1 == tank2);
    }

    public static bool operator ==(UserVehicle vehicle1, UserVehicle vehicle2)
    {
        return (vehicle1 as object == vehicle2 as object)
            || (vehicle1 as object != null && vehicle2 as object != null && vehicle1.Info.id == vehicle2.Info.id);
    }

    public override bool Equals (object obj) {
        var t = obj as UserVehicle;
        if (t == null) return false;
        if (info == null) return false;

        return info.id.Equals (t.info.id);
    }

    public override int GetHashCode () {
        if (info == null) {
            return 0;
        }
        return info.id.GetHashCode ();
    }

    public void EndCamouflageTrialWith(Pattern camo = null)
    {
        if (BodykitController == null)
            return;

        if (camo == null)
        {
            BodykitController.ResetCamouflageTexture();
            return;
        }

        BodykitController.DrawCamouflage(camo, info.id);
    }

    public void EndDecalTrialWith(Decal decal = null)
    {
        if (decal == null)
        {
            BodykitController.ResetDecal();
            return;
        }

        BodykitController.DrawDecal(decal);
    }

    public int GetModuleLevel(TankModuleInfos.ModuleType type)
    {
        return upgrades.GetModuleLevel(type);
    }

    public Dictionary<VehicleInfo.VehicleParameter, ObscuredFloat> GetParameters(TankModuleInfos.ModuleType type, int level)
    {
        Dictionary<VehicleInfo.VehicleParameter, ObscuredFloat> _parameters
            = new Dictionary<VehicleInfo.VehicleParameter, ObscuredFloat>(
                GetRealParameters(
                    ignoreCamo:     true,
                    ignoreDecal:    true));

        ApplyModule(_parameters, type, upgrades.GetModuleLevel(type), false);
        ApplyModule(_parameters, type, level, true);

        return _parameters;
    }

    public Dictionary<VehicleInfo.VehicleParameter, ObscuredFloat> GetParametersWith<TBodykit>(int bodyKitId)
        where TBodykit : Bodykit
    {
        bool bodyKitIsCamo = typeof(TBodykit) == typeof(Pattern);
        bool bodyKitIsDecal = typeof(TBodykit) == typeof(Decal);

        Dictionary<VehicleInfo.VehicleParameter, ObscuredFloat> _parameters
            = GetRealParameters(
                ignoreCamo:     bodyKitIsCamo,
                ignoreDecal:    bodyKitIsDecal);

        Bodykit camo = PatternPool.Instance.GetItemById(bodyKitId);

        if (camo != null && bodyKitIsCamo)
            ApplyBodykitEffect(_parameters, camo);

        Bodykit decal = DecalPool.Instance.GetItemById(bodyKitId);

        if (decal != null && bodyKitIsDecal)
            ApplyBodykitEffect(_parameters, decal);

        return _parameters;
    }

    public Dictionary<VehicleInfo.VehicleParameter, ObscuredFloat> GetRealParameters(bool ignoreCamo = false, bool ignoreDecal = false, bool ignoreModule = false)
    {
        Dictionary<VehicleInfo.VehicleParameter, ObscuredFloat> _parameters = new Dictionary<VehicleInfo.VehicleParameter, ObscuredFloat>(4);

        _parameters.Add(VehicleInfo.VehicleParameter.RoF, Info.baseROF);
        _parameters.Add(VehicleInfo.VehicleParameter.IRCMRoF, Info.baseIRCMROF);
        _parameters.Add(VehicleInfo.VehicleParameter.Armor, Info.baseArmor);
        _parameters.Add(VehicleInfo.VehicleParameter.Speed, Info.baseSpeed);
        _parameters.Add(VehicleInfo.VehicleParameter.Damage, Info.baseDamage);
        _parameters.Add(VehicleInfo.VehicleParameter.RocketDamage, Info.baseRocketDamage);

        if (!ignoreModule)
        {
            ApplyModule(_parameters, TankModuleInfos.ModuleType.Armor, upgrades.GetModuleLevel(TankModuleInfos.ModuleType.Armor), true);
            ApplyModule(_parameters, TankModuleInfos.ModuleType.Cannon, upgrades.GetModuleLevel(TankModuleInfos.ModuleType.Cannon), true);
            ApplyModule(_parameters, TankModuleInfos.ModuleType.Engine, upgrades.GetModuleLevel(TankModuleInfos.ModuleType.Engine), true);
            ApplyModule(_parameters, TankModuleInfos.ModuleType.Reloader, upgrades.GetModuleLevel(TankModuleInfos.ModuleType.Reloader), true);
            ApplyModule(_parameters, TankModuleInfos.ModuleType.Tracks, upgrades.GetModuleLevel(TankModuleInfos.ModuleType.Tracks), true);
        }

        Bodykit camo = PatternPool.Instance.GetItemById(upgrades.CamouflageId);

        if (camo != null && !ignoreCamo)
            ApplyBodykitEffect(_parameters, camo);

        Bodykit decal = DecalPool.Instance.GetItemById(upgrades.DecalId);

        if (decal != null && !ignoreDecal)
            ApplyBodykitEffect(_parameters, decal);

        return _parameters;
    }

    public void ReceiveModule()
    {
        if (upgrades.awaitedModule == TankModuleInfos.ModuleType.None)
            return;

        int level = upgrades.GetModuleLevel(upgrades.awaitedModule) + 1;

        upgrades.SetModuleLevel(upgrades.awaitedModule, level);

        ApplyModule(parameters, upgrades.awaitedModule, level - 1, false);
        ApplyModule(parameters, upgrades.awaitedModule, level, true);

        TankModuleInfos.ModuleType received = upgrades.awaitedModule;

        upgrades.awaitedModule = TankModuleInfos.ModuleType.None;
        upgrades.moduleReadyTime = float.MaxValue;

        HangarController.OnTimerTick -= ModuleReceiving;
        ModuleShop.Instance.ModuleReceived(this, received);
    }

    public void TryOnCamouflage(Pattern camo)
    {
        if (camo == null)
        {
            EndCamouflageTrialWith();
            return;
        }

        BodykitController.DrawCamouflage(camo, info.id);
    }

    public void TryOnDecal(Decal decal)
    {
        if (decal == null)
        {
            EndDecalTrialWith();
            return;
        }

        BodykitController.DrawDecal(decal);
    }

    private void ApplyBodykitEffect(Dictionary<VehicleInfo.VehicleParameter, ObscuredFloat> _params, Bodykit bodykit)
    {
        _params[VehicleInfo.VehicleParameter.Damage] += bodykit.damageGain * Info.baseDamage;
        _params[VehicleInfo.VehicleParameter.RocketDamage] += bodykit.rocketDamageGain * Info.baseRocketDamage;
        _params[VehicleInfo.VehicleParameter.Armor] += bodykit.armorGain * Info.baseArmor;
        _params[VehicleInfo.VehicleParameter.Speed] += bodykit.speedGain * Info.baseSpeed;
        _params[VehicleInfo.VehicleParameter.RoF] += bodykit.rofGain * Info.baseROF;
        _params[VehicleInfo.VehicleParameter.IRCMRoF] += bodykit.ircmRofGain * Info.baseIRCMROF;
    }

    public void ApplyModule(Dictionary<VehicleInfo.VehicleParameter, ObscuredFloat> _params, TankModuleInfos.ModuleType type, int level, bool positive)
    {
        if (level < 1)
            return;

        TankModuleInfos.Module moduleInfo = null;
        VehicleInfo.ModuleUpgrade moduleUpgrade = null;

        switch (type)
        {
            case TankModuleInfos.ModuleType.Reloader:
                moduleInfo = TankModules.reloader;
                moduleUpgrade = Info.reloaderUpgrades[level - 1];
                break;
            case TankModuleInfos.ModuleType.Cannon:
                moduleInfo = TankModules.cannon;
                moduleUpgrade = Info.cannonUpgrades[level - 1];
                break;
            case TankModuleInfos.ModuleType.Engine:
                moduleInfo = TankModules.engine;
                moduleUpgrade = Info.engineUpgrades[level - 1];
                break;
            case TankModuleInfos.ModuleType.Armor:
                moduleInfo = TankModules.armor;
                moduleUpgrade = Info.armorUpgrades[level - 1];
                break;
            case TankModuleInfos.ModuleType.Tracks:
                moduleInfo = TankModules.tracks;
                moduleUpgrade = Info.tracksUpgrades[level - 1];
                break;
        }

        if (moduleInfo == null)
            return;

        if (moduleInfo.primaryParameter != VehicleInfo.VehicleParameter.None)
            _params[moduleInfo.primaryParameter] += positive ? (float)moduleUpgrade.primaryGain : -moduleUpgrade.primaryGain;

        if (moduleInfo.secondaryParameter != VehicleInfo.VehicleParameter.None)
            _params[moduleInfo.secondaryParameter] += positive ? (float)moduleUpgrade.secondaryGain : -moduleUpgrade.secondaryGain;
    }
    private void CamouflageRemoving(double timeStamp)
    {
        ExpiredCamouflageRemoving();

        if (!upgrades.OwnedCamouflages.Any())
            HangarController.OnTimerTick -= CamouflageRemoving;
    }

    private void DecalRemoving(double timeStamp)
    {
        ExpiredDecalRemoving();

        if (!upgrades.OwnedDecals.Any())
            HangarController.OnTimerTick -= DecalRemoving;
    }

    private void ExpiredCamouflageRemoving()
    {
        if (!upgrades.OwnedCamouflages.Any())
            return;

        int nextCamoId;

        if (upgrades.OwnedCamouflages.TryToTakeAwayById(upgrades.CamouflageId, out nextCamoId))
        {
            Dispatcher.Send(EventId.PatternExpired, new EventInfo_I(upgrades.CamouflageId));

            upgrades.SetCamouflageById(nextCamoId);

            if (GUIPager.ActivePageName != "PatternShop")
                BodykitController.DrawCamouflage(PatternPool.Instance.GetItemById(upgrades.CamouflageId), info.id);
        }
    }

    private void ExpiredDecalRemoving()
    {
        if (!upgrades.OwnedDecals.Any())
            return;

        int nextDecalId;

        if (upgrades.OwnedDecals.TryToTakeAwayById(upgrades.DecalId, out nextDecalId))
        {
            Dispatcher.Send(EventId.DecalExpired, new EventInfo_I(upgrades.DecalId));

            upgrades.SetDecalById(nextDecalId);

            if (GUIPager.ActivePageName != "DecalShop")
                BodykitController.DrawDecal(DecalPool.Instance.GetItemById(upgrades.DecalId));
        }
    }

    public void ModuleReceiving(double timeStamp)
    {
        if (upgrades.awaitedModule != TankModuleInfos.ModuleType.None && upgrades.moduleReadyTime <= timeStamp)
            ReceiveModule();
    }

    public UserVehicle GetFullModuleUpgradedClone()
    {
        return new UserVehicle(info, HangarVehicle, VehicleUpgrades.GetFullModuleUpgrades(info));
    }
}