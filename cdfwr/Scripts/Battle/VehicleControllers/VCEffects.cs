using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VCEffects 
{
    VehicleController vehicle;
    private readonly List<VehicleEffect> effectsToCancel = new List<VehicleEffect>(3); // Для отмены эффектов. После использования очищать.

    public VCEffects(VehicleController vehicle) 
    {
        this.vehicle = vehicle;
    }

    ///<summary>
    /// Fixates addition effect in tank.
    /// Returns true if new effect was created (not permanent), false otherwise (refreshing existed effect or add not cancellable effect)
    ///</summary>
    public bool FixateEffect(VehicleEffect effect)
    {
        List<VehicleEffect> sameCellEffects = new List<VehicleEffect>(3);
        HashSet<int> uiIdsToCancel = new HashSet<int>();

        foreach (VehicleEffect eff in vehicle.Effects.Values)
        {
            if (eff.UI_Id == effect.UI_Id)
            {
                sameCellEffects.Add(eff);
                continue;
            }

            if (eff.Type == effect.Type)
            {
                uiIdsToCancel.Add(eff.UI_Id);
            }
        }

        // Отключить все эффекты тех расходок, в которых есть эффект того же параметра, что у фиксируемого
        foreach (var uiId in uiIdsToCancel)
        {
            CancelAllEffectsForUI_ID(uiId);
            IEPanel.Instance.RemoveCell(uiId);
        }

        // Если уже есть эффекты под этот UI_ID, просто обновить их время в GUI
        if (sameCellEffects.Count != 0)
        {
            bool effectAlreadyPresents = false;
            foreach (var eff in sameCellEffects)
            {
                eff.SetEndTime(effect.EndTime);
                if (eff.Type == effect.Type)
                {
                    effectAlreadyPresents = true;
                }
            }
            if (!effectAlreadyPresents)
            {
                EffectItself(effect);
            }

            return false;
        }

        EffectItself(effect);
        return effect.MustBeReturned;
    }

    public void TakeEffectAway(int id)
    {
        VehicleEffect effect;
        if (!vehicle.Effects.TryGetValue(id, out effect))
        {
            return;
        }

        EffectItself(effect, true);
    }

    public void ApplyEffect(VehicleEffect effect)
    {
        if (!vehicle.IsMine)
        {
            return;
        }

        effect = new VehicleEffect(
                id: VehicleEffect.GetNewId(),
                efType: effect.Type,
                modType: effect.ModType,
                modValue: effect.ModValue,
                _duration: effect.Duration,
                _startTime: PhotonNetwork.time,
                bonusType: effect.Source,
                _icon: effect.Icon,
                consumableId: effect.ConsumableId);

        Dispatcher.Send(EventId.TankEffectApply, new EventInfo_IE(vehicle.data.playerId, effect), Dispatcher.EventTargetType.ToAll);
    }

    public virtual void EffectItself(VehicleEffect effect, bool inverted = false)
    {
        if (effect.MustBeReturned)
        {
            if (inverted)
            {
                vehicle.Effects.Remove(effect.Id);
            }
            else
            {
                vehicle.Effects.Add(effect.Id, effect);
            }
        }

        effect.ApplyToVehicle(vehicle, inverted);
    }

    protected VehicleEffect FindRelatedEffect(VehicleEffect effect)
    {
        foreach (VehicleEffect eff in vehicle.Effects.Values)
        {
            if (eff.UI_Id == effect.UI_Id && eff.Type == effect.Type)
            {
                return effect;
            }
        }

        return null;
    }

    protected void CancelAllEffects()
    {
        foreach (VehicleEffect effect in vehicle.Effects.Values)
        {
            effectsToCancel.Add(effect);
        }

        foreach (var effect in effectsToCancel)
        {
            Dispatcher.Send(EventId.TankEffectCancelled, new EventInfo_II(vehicle.data.playerId, effect.Id), Dispatcher.EventTargetType.ToAll);
        }
        effectsToCancel.Clear();
    }

    /// <summary>
    /// Отменить все эффекты связанные с опред. UI_ID
    /// </summary>
    /// <param name="uiId"></param>
    protected void CancelAllEffectsForUI_ID(int uiId)
    {
        foreach (VehicleEffect effect in vehicle.Effects.Values)
        {
            if (effect.UI_Id == uiId)
            {
                effectsToCancel.Add(effect);
            }
        }

        foreach (var effect in effectsToCancel)
        {
            Dispatcher.Send(EventId.TankEffectCancelled, new EventInfo_II(vehicle.data.playerId, effect.Id),
                Dispatcher.EventTargetType.ToAll);
        }

        effectsToCancel.Clear();
    }

    protected void UpdateEffects()
    {
        foreach (var effect in vehicle.Effects.Values)
        {
            if (PhotonNetwork.time >= effect.EndTime)
            {
                effectsToCancel.Add(effect);
            }
        }

        if (effectsToCancel.Count == 0)
        {
            return;
        }

        foreach (var effect in effectsToCancel)
        {
            Dispatcher.Send(EventId.TankEffectCancelled, new EventInfo_II(vehicle.data.playerId, effect.Id), Dispatcher.EventTargetType.ToAll);
        }
        effectsToCancel.Clear();
    }
}
