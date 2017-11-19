using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XD
{
    /// <summary>
    /// Временное изменение параметров техники.
    /// </summary>
    public class TemporaryUnitParameterChanger : IUseConsumable
    {
        private VehicleController vehicle = null;
        private List<Buff> buffs = new List<Buff>();

        public void Use()
        {
            Buff buff = null;
            for (int i = 0; i < buffs.Count; i++)
            {
                buff = buffs[i];

                if (buff != null /*&& buff.Notification*/)
                {
                    //Buff buff = BuffDispatcher.GetNewBuff(buff.ConsumableOwnerID, (Setting)buff.ID, myConsumable.ID);
                    buff.performerID = vehicle.OwnerID;
                    buff.forShells = false;//КОСТЫЛЬ?
                    buff.ConsumableOwnerID = buff.ConsumableOwnerID;

                    StaticType.BuffDispatcher.Instance<IBuffDispatcher>().AddBuff(vehicle, buff);

                    //Debug.LogError("timer: " + myConsumable.Settings[Setting.Duration]);

                    Dispatcher.Send(
                        EventId.StartBuff,
                        new EventInfo_U(
                            vehicle.Data.playerId,
                            vehicle.OwnerID,
                            new[] { buff.ConsumableOwnerID, buff.ID, buff.MyConsumable.ID, (int)buff.MyConsumable.Settings[Setting.Duration].Max }),
                            Dispatcher.EventTargetType.ToOthers,
                        //victim.IsBot ? Dispatcher.EventTargetType.ToMaster : Dispatcher.EventTargetType.ToSpecific,
                        vehicle.Data.playerId);
                }

                StaticContainer.Get<IBuffDispatcher>(StaticType.BuffDispatcher).AddBuff(vehicle, buffs[i]);
            }
        }

        public TemporaryUnitParameterChanger(VehicleController target, VehicleController performer, Settings _settings, object[] _buffs)
        {
            vehicle = target;
            for (int i = 0; i < _buffs.Length; i++)
            {
                Buff buff = ((Buff)_buffs[i]).CreateNew();
                buff.impactType = ImpactType.Timer;
                buff.performerID = performer.id;
                buff.Refresh(_settings);
                buffs.Add(buff);
            }
        }
    }
    
    /// <summary>
    /// Снятие баффов
    /// </summary>
    public class BuffDestroyer : IUseConsumable
    {
        private IUnitBehaviour vehicle = null;
        private Settings settings = null;

        private bool isActive = false;

        public void Use()
        {
            if (isActive)
            {
                return;
            }

            StaticContainer.RoutineManager.StartStaticRoutine(vehicle, TimerRoutine());
        }

        private void Apply()
        {
            Setting paramName;
            Buff buff = null;
            List<int> intList = new List<int>();
            //Debug.LogError("BuffDestroyer: " + settings.Count + ", buffs: " + vehicle.ActiveBuffs.Count);

            for (int i = 0; i < settings.Count; i++)
            {
                paramName = settings.GetName(i);
                for (int j = 0; j < vehicle.ActiveDebuffs.Count; j++)
                {
                    buff = vehicle.ActiveDebuffs[j];

                    if (buff.Type == paramName)
                    {
                        //Debug.LogError("Снят дебафф: " + buff.Type + ", veh: " + vehicle.UnitBattle.Name);
                        StaticContainer.Get<IBuffDispatcher>(StaticType.BuffDispatcher).RemoveBuff((VehicleController)vehicle, buff.Type, buff.isNegative);
                    }
                }

                if (paramName != Setting.Cooldown && paramName != Setting.Duration)
                {
                    vehicle.BlockedDebuffs.Add(settings.GetName(i));
                    intList.Add((int)settings.GetName(i));
                }
            }

            if (intList.Count > 0)
            {
                Dispatcher.Send(EventId.UseBuffBlocker, new EventInfo_U(vehicle.Data.playerId, intList.ToArray(), true), Dispatcher.EventTargetType.ToOthers, vehicle.Data.playerId);
            }
        }

        private IEnumerator TimerRoutine()
        {
            Clamper time = settings[Setting.Duration];
            WaitForSeconds waiter = new WaitForSeconds(1);
            isActive = true;
            time.Refresh();
            Apply();

            while (!time.Minimum)
            {
                //Apply();
                time -= 1;
                yield return waiter;
            }

            isActive = false;
            //Debug.LogError("BuffDestroyer: " + settings.Count + ", buffs: " + vehicle.ActiveBuffs.Count);

            List<int> intList = new List<int>();
            for (int i = 0; i < settings.Count; i++)
            {
                vehicle.BlockedDebuffs.Remove(settings.GetName(i));
                intList.Add((int)settings.GetName(i));
            }

            Dispatcher.Send(EventId.UseBuffBlocker, new EventInfo_U(vehicle.Data.playerId, intList.ToArray(), false), Dispatcher.EventTargetType.ToOthers, vehicle.Data.playerId);
        }

        public BuffDestroyer(IUnitBehaviour _vehicle, Settings _settings)
        {
            vehicle = _vehicle;
            settings = _settings.Clone();
        }
    }

    /// <summary>
    /// Изменение параметров техники на весь бой.
    /// </summary>
    public class PermanentUnitParameterChanger : IUseConsumable
    {
        //private VehicleController vehicle = null;
        private List<Buff> buffs = new List<Buff>();

        public void Use()
        {
            //Debug.LogError("Permanent Use(), " + vehicle.name + ", buffs: " + buffs.Count);
            /*for (int i = 0; i < buffs.Count; i++)
            {
                StaticContainer.Get<IBuffDispatcher>(StaticType.BuffDispatcher).AddBuff(vehicle, buffs[i]);
            }*/
        }

        public PermanentUnitParameterChanger(VehicleController _vehicle, VehicleController performer, Settings _settings, object[] _buffs)
        {
            //vehicle = _vehicle;
            Clamper duration = _settings[Setting.Duration];

            if (duration == null)
            {
                duration = _settings.Add(Setting.Duration, new Clamper());
            }

            duration.Set(-1, -1, -1);

            for (int i = 0; i < _buffs.Length; i++)
            {
                Buff buff = (Buff)_buffs[i];
                buff.impactType = ImpactType.Permanent;
                buff.performerID = performer.id;
                buff.Refresh(_settings);
                buffs.Add(buff);
            }
        }
    }
    
    /// <summary>
    /// Изменение параметров расходников.
    /// </summary>
    public class ConsumablesParameterChanger : IUseConsumable
    {
        private IConsumableBattle targetCons = null;
        private IConsumableBattle performerCons = null;
        private List<Buff> buffs = new List<Buff>();
        private Settings settings = null;

        public void Use()
        {
            if (targetCons == null)
            {
                Debug.LogError("Target == null");
                return;
            }
            
            //Debug.LogError("Use ShellChanger: " + targetCons.Name + ", amount: " + targetCons.Amount);

            Setting paramName;
            for (int i = 0; i < settings.Count; i++)
            {
                paramName = settings.GetName(i);

                if (targetCons.Settings.Contains(paramName))
                {
                    //Debug.LogError(paramName + " Summ: " + targetCons.Settings[paramName] + " + " + settings[paramName]);
                    targetCons.Settings[paramName].Summ(settings[paramName]);
                }
                else
                {
                    targetCons.Settings.Add(paramName, settings[paramName].Clone());
                }
            }

            //Debug.LogError("act: " + buffs.Count + ", " + target.Name + ": " + target.Buffs.Count);
            for (int i = 0; i < buffs.Count; i++)
            {
                Buff buff = buffs[i];

                if (!targetCons.HasBuff(buff.Type))
                {
                    //Debug.LogError("Добавлен бафф в расходку: " + buff.name + ",  " + targetCons.Name);
                    Buff added = buff.CreateNew();
                    targetCons.AddBuff(added);
                    added.MyConsumable = targetCons;
                    added.forShells = false;
                    continue;
                }

                ChangeParameters(targetCons.GetBuff(buff.Type, false), buff);
            }

            if (performerCons.SlotType == ConsumableSlotType.Consumables && performerCons.Type == ConsumableType.ShellChanger && settings[Setting.Duration].Max > 0)
            {
                StaticContainer.RoutineManager.StartStaticRoutine(targetCons, TimeChangeRoutine(settings[Setting.Duration].Max));
            }

            //Debug.LogError("Use: " + target.Name + "! buffs" + target.Buffs.Count);
        }

        private IEnumerator TimeChangeRoutine(float duration)
        {
            //Debug.LogError("TimeChangeRoutine: " + target.Name);
            yield return new WaitForSeconds(duration);

            if (targetCons == null)
            {
                yield break;
            }

            Revoke();
        }

        private void Revoke()
        {
            //Debug.LogError("Revoke: " + targetCons.Name);
            if (targetCons == null)
            {
                Debug.LogError("Revokie() Target == null");
                return;
            }

            Setting paramName;

            for (int i = 0; i < settings.Count; i++)
            {
                paramName = settings.GetName(i);

                if (targetCons.Settings.Contains(paramName))
                {
                    targetCons.Settings[paramName].Subtract(targetCons.Settings[paramName]);
                }
            }

            for (int i = 0; i < buffs.Count; i++)
            {
                Buff buff = buffs[i];

                if (targetCons.HasBuff(buff.Type))
                {
                    //Debug.LogError("Удален бафф из расходки: " + buff.name + ",  " + target.Name);
                    ChangeParameters(targetCons.GetBuff(buff.Type, false), buff.CreateNew(), true);
                }
            }
        }

        private void ChangeParameters(Buff targetBuff, Buff buff, bool revoke = false)
        {
            Setting paramName;
            for (int j = 0; j < buff.Settings.Count; j++)
            {
                paramName = buff.Settings.GetName(j);

                switch (paramName)
                {
                    case Setting.BurnProbability:
                    case Setting.CritProbability:
                    case Setting.DamageAbsorptionProbability:
                    case Setting.MoveSpeedChangeProbability:
                    case Setting.OneShotProbability:
                    case Setting.RPMChangeProbability:
                    case Setting.TurretSpeedChangeProbability:
                    case Setting.TurnSpeedChangeProbability:

                        if (targetBuff.Settings.Contains(paramName))
                        {
                            if (revoke)
                            {
                                //Debug.LogError(paramName + " Sub: " + targetBuff.Settings[paramName] + " - " + buff.Settings[paramName]);
                                targetBuff.Settings[paramName].Subtract(buff.Settings[paramName]);
                            }
                            else
                            {
                                //Debug.LogError(paramName + " Summ: " + targetCons.Settings[paramName] + " + " + settings[paramName]);
                                targetBuff.Settings[paramName].Summ(buff.Settings[paramName]);
                            }
                        }
                        else if (!revoke)
                        {
                            targetBuff.Settings.Add(paramName, buff.Settings[paramName].Clone());
                        }

                        continue;

                    case Setting.Duration:
                        continue;
                }

                if (targetBuff.Settings.Contains(paramName))
                {
                    if (revoke)
                    {
                        //if (paramName == Setting.BurningDamage)
                        //    Debug.LogError(paramName + " Sub: " + targetBuff.Settings[paramName] + " - " + buff.Settings[paramName]);
                        targetBuff.Settings[paramName].Subtract(buff.Settings[paramName]);
                    }
                    else
                    {
                        //if (paramName == Setting.BurningDamage)
                        //    Debug.LogError(targetBuff.name + " / " + buff.name + ", " + paramName + " Summ: " + targetBuff.Settings[paramName] + " + " + buff.Settings[paramName]);
                        targetBuff.Settings[paramName].Summ(buff.Settings[paramName]);
                    }
                }
                else if (!revoke)
                {
                    targetBuff.Settings.Add(paramName, buff.Settings[paramName].Clone());
                }
            }
        }

        public ConsumablesParameterChanger(IConsumableBattle _performerCons, IConsumableBattle _targetCons, Settings _settings, List<Buff> _buffs)
        {
            performerCons = _performerCons;
            targetCons = _targetCons;
            buffs = _buffs;
            settings = _settings.Clone();
        }
    }
}