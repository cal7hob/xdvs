using CodeStage.AntiCheat.ObscuredTypes;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using XD;


[System.Serializable]
public class Weapon : IUnitWeapon
{
    #region ISender
    public string Description
    {
        get
        {
            return "[Weapon] ";
            //return "[ARGUEStatic]" + name;
        }

        set
        {
            //name = value;
        }
    }

    private List<ISubscriber> subscribers = null;

    public List<ISubscriber> Subscribers
    {
        get
        {
            if (subscribers == null)
            {
                subscribers = new List<ISubscriber>();
            }
            return subscribers;
        }
    }

    public void AddSubscriber(ISubscriber subscriber)
    {
        if (Subscribers.Contains(subscriber))
        {
            return;
        }
        Subscribers.Add(subscriber);
    }

    public void RemoveSubscriber(ISubscriber subscriber)
    {
        Subscribers.Remove(subscriber);
    }

    public void Event(Message message, params object[] _parameters)
    {
        for (int i = 0; i < Subscribers.Count; i++)
        {
            Subscribers[i].Reaction(message, _parameters);
        }
    }
    #endregion

    private static readonly ObscuredFloat   FIRE_RATE_SECONDS = 60.0f;

    private readonly GunShellInfo.ShellType shellType = GunShellInfo.ShellType.Usual;
    private readonly VehicleController      vehicleController = null;

    public IConsumableBattle                consumable = null;

    [Header("DEBUG")]
    public XD.Settings                      settings = new XD.Settings();

    public List<Buff> buffs = null;

    public Weapon(VehicleController vehicleController, GunShellInfo.ShellType shellType)
    {
        this.vehicleController = vehicleController;
        this.shellType = shellType;
    }

    public bool IsReady
    {
        get
        {
            return !vehicleController.IsMine || (IsReloaded && !IsOverheat && !IsEmpty);
        }
    }

    public bool IsEmpty
    {
        get
        {
            if (vehicleController.IsBot)
            {
                return false;
            }

            bool res = consumable == null || consumable.Amount == 0;
            /*if (res)
            {
                Debug.LogError("OUT OF SHELLS: " + (consumable != null ? consumable.name : "null"));
            }*/

            return res;
        }
    }

    public bool IsOverheat
    {
        get
        {
            bool overheat = vehicleController.IsMine && HeatingProgress > 1 - vehicleController.GetHeating(shellType);

            if (overheat)
            {
                Dispatcher.Send(EventId.WeaponOverheated, new EventInfo_I((int)shellType));
            }

            return overheat;
        }
    }

    public bool IsReloaded
    {
        get; private set;
    }

    public float ReloadRemainingSeconds
    {
        get
        {
            return ReloadingTimeSeconds * (1 - ReloadingProgress);
        }
    }

    public float ReloadingProgress
    {
        get;
        private set;
    }

    public ObscuredFloat HeatingProgress
    {
        get; private set;
    }

    public ObscuredFloat ReloadingTimeSeconds
    {
        get
        {
            return FIRE_RATE_SECONDS / vehicleController.GetROF(shellType);
        }
    }

    public void FirstCharge()
    {
        IConsumableBattle shells = (IConsumableBattle)vehicleController.InstalledBattleConsumables.GetBySlot(9);
        if (shells == null)
        {
            shells = (IConsumableBattle)vehicleController.InstalledBattleConsumables.GetBySlot(8);
        }

        if (shells == null)
        {
            Debug.LogError(vehicleController.name + " Weapon was not be charged! shells == NULL", vehicleController.gameObject);
            return;
        }

        Charge(shells);

        //ConsumablePanels.StaticSelectConsumable(shells.SlotID);
    }

    public void UpdateReloadingProgress(float deltaTime)
    {
        //HeatingProgress = Mathf.Clamp01(HeatingProgress - vehicleController.GetCooling(shellType) * Time.deltaTime);

        if (IsReloaded || IsEmpty)
        {
            return;
        }

        ReloadingProgress += deltaTime / ReloadingTimeSeconds;

        if (ReloadingProgress >= 1)
        {
            IsReloaded = true;

            if (!vehicleController.IsMine)
            {
                return;
            }

            Dispatcher.Send(EventId.WeaponReloaded, new EventInfo_I((int)shellType));
        }
    }

    public void InstantReload()
    {
        ReloadingProgress = 1;
        HeatingProgress = 0;
    }

    public void RegisterShot()
    {
        IsReloaded = false;
        ReloadingProgress = 0;
        HeatingProgress += vehicleController.GetHeating(shellType);
        ConsumableActions();
        //Debug.LogError("RegisterShot!");
        vehicleController.SendMessageToFX(Message.EffectRequest, EffectTarget.Shot);
        Event(Message.WeaponUsed);
        //Event(Message.EffectRequest, EffectTarget.Shot);

        vehicleController.Event(Message.StatisticUpdate, StatisticParameter.Shots, 1f, consumable == null ? -1 : consumable.ID, vehicleController.Settings == null ? -1 : (int)vehicleController.Settings[Setting.Damage].Max);
    }

    private void ConsumableActions()
    {
        if (consumable == null)
        {
            return;
        }

        consumable.ChangeAmount(-1);
        vehicleController.UnitBattle.Event(Message.ConsumableUsed, consumable.ID, consumable.Amount, consumable.SlotType);
        //ConsumablePanels.UpdateConsumable(consumable);

        //если закончились снар€ды, переключаемс€ на другие
        if (consumable.Amount <= 0)
        {
            IConsumableBattle anotherShell;
            if (consumable.SlotID == 8)
            {
                anotherShell = (IConsumableBattle)vehicleController.InstalledBattleConsumables.GetBySlot(9);
                if (anotherShell != null && anotherShell.Amount > 0)
                {
                    Charge(anotherShell);
                }
            }
            else if (consumable.SlotID == 9)
            {
                anotherShell = (IConsumableBattle)vehicleController.InstalledBattleConsumables.GetBySlot(8);
                if (anotherShell != null && anotherShell.Amount > 0)
                {
                    Charge(anotherShell);
                }
            }
        }
    }

    /// <summary>
    /// «ар€дить оружие определенным снар€дом.
    /// </summary>
    public void Charge(IConsumableBattle shells)
    {
        if (shells == null)
        {
            Debug.LogError("Weapon is not charged! " + vehicleController.name);
            return;
        }

        if (consumable == shells || shells.Amount.Minimum)
        {
            return;
        }

        if (consumable != null)
        {
            consumable.SetActive(false);
        }

        consumable = shells;
        IsReloaded = false;
        ReloadingProgress = 0;
        vehicleController.Settings[Setting.Damage].Set(vehicleController.UnitBattle.GetDamageByShell(shells, true));
        vehicleController.Settings[Setting.CritProbability].Current = consumable.Settings[Setting.CritProbability].Current;
        vehicleController.Settings[Setting.CritFactor].Current = consumable.Settings[Setting.CritFactor].Current;
        consumable.SetActive(true);
        vehicleController.UnitBattle.Event(Message.ConsumableUsed, consumable.ID, consumable.Amount, consumable.SlotType);
        settings = consumable.Settings;
        buffs = consumable.Buffs;
        //Debug.LogError("CHarge: " + shells.Name);
    }
}