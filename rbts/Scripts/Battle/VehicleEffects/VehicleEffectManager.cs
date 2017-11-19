using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Hashtable = ExitGames.Client.Photon.Hashtable;

public class VehicleEffectManager : IDisposable/*, IVehicleEffectManager*/
{
    private VehicleController owner;
    private readonly List<VehicleEffect> effects = new List<VehicleEffect>(2);
    private readonly Dictionary<string, VehicleEffect> effectsDic = new Dictionary<string, VehicleEffect>(2);
    private List<VehicleEffect> effectsToCancel = new List<VehicleEffect>(2);
    private int nextEffectId;

    private VehicleFXManager fxManager;

    
    public VehicleEffectManager(VehicleController owner)
    {
        this.owner = owner;

        Messenger.Subscribe(EventId.VehicleRespawned, OnVehicleRespawned);
        Messenger.Subscribe(EventId.VehicleKilled, OnVehicleKilled);
        Messenger.Subscribe(EventId.BeforeReconnecting, OnReconnect);
        Messenger.Subscribe(EventId.BattleEnd, OnBattleEnd);
        
        fxManager = new VehicleFXManager(owner);

        if (owner.IsBot || owner.IsMain)
        {
            Messenger.Subscribe(EventId.VehicleEffectRequest, OnVehicleEffectRequest);
        }
    }

    public void Dispose()
    {
        Messenger.Unsubscribe(EventId.VehicleEffectRequest, OnVehicleEffectRequest);
        Messenger.Unsubscribe(EventId.VehicleRespawned, OnVehicleRespawned);
        Messenger.Unsubscribe(EventId.VehicleKilled, OnVehicleKilled);
        Messenger.Unsubscribe(EventId.BeforeReconnecting, OnReconnect);
        Messenger.Unsubscribe(EventId.BattleEnd, OnBattleEnd);
    }
    
    private void OnVehicleEffectRequest(EventId eventId, EventInfo ei)
    {
        EventInfo_U info = (EventInfo_U)ei;
        if (!owner.PhotonView.isMine || owner.data.playerId != (int)info[0])
            return;

        SetPhotonPropertiesForEffect((VehicleEffectData) info[1]);
    }

    private void SetPhotonPropertiesForEffect(VehicleEffectData effectData)
    {
        Hashtable properties = new Hashtable();
        effectData.endTime = PhotonNetwork.time + effectData.Duration;

        VehicleEffect similarEffect = GetSimilarEffect(effectData);
        if (similarEffect != null)
        {
            properties[similarEffect.effectPropertyKey] = effectData;
        }
        else
        {
            properties[GetNewEffectPropertyKey()] = effectData;
        }

        if (owner.IsBot)
        {
            PhotonNetwork.room.SetCustomProperties(properties);
        }
        else
        {
            owner.Player.SetCustomProperties(properties);
        }
    }

    private string GetNewEffectPropertyKey(int id = -1)
    {
        if (id == -1)
        {
            id = nextEffectId++;
        }

        string output = owner.IsBot ? string.Format("bt{0}", owner.data.playerId) : "";
        output += string.Format("ef{0}", id);

        return output;
    }

    private VehicleEffect GetSimilarEffect(VehicleEffectData effectData)
    {
        foreach(VehicleEffect effect in effectsDic.Values)
        {
            if (effect.ParamType == effectData.ParameterType && effect.IsPositive == effectData.IsPositive())
                return effect;
        }

        return null;
    }


    private void OnVehicleRespawned(EventId eid, EventInfo ei)
    {
        EventInfo_I info = (EventInfo_I) ei;
        if (info.int1 != owner.data.playerId)
            return;
    }

    private void OnVehicleKilled(EventId eid, EventInfo ei)
    {
        EventInfo_II info = (EventInfo_II)ei;
        if (info.int1 != owner.data.playerId)
            return;

        CancelAllEffects();
    }

    private void OnReconnect(EventId eid, EventInfo ei)
    {
        CancelAllEffects();
    }

    private void OnBattleEnd(EventId eid, EventInfo ei)
    {
        CancelAllEffects();
    }

    public void Update()
    {
        double photonTime = PhotonNetwork.time;

        for(int i = 0; i < effects.Count; ++i)
        {
            if (photonTime >= effects[i].EndTime)
            {
                effectsToCancel.Add(effects[i]);
            }
        }

        if (effectsToCancel.Count == 0)
            return;

        for (int i = 0; i < effectsToCancel.Count; ++i)
        {
            CancelEffect(effectsToCancel[i]);
        }

        effectsToCancel.Clear();
    }

    /// <summary>
    /// Запрашивает наложение эффекта у владельца
    /// </summary>
    /// <param name="effectData"></param>
    public void RequestEffect(VehicleEffectData effectData)
    {
            Messenger.Send(
                EventId.VehicleEffectRequest, new EventInfo_U(owner.data.playerId, effectData),
                owner.IsBot
                    ? Messenger.EventTargetType.ToMaster
                    : Messenger.EventTargetType.ToSpecific,
                owner.data.playerId);
    }

    public void AcceptEffectSignal(string key, VehicleEffectData effectData)
    {
        VehicleEffect effect;
        if (effectsDic.TryGetValue(key, out effect))
        {
            effect.ChangeData(effectData);
            fxManager.ReactToEffectModify(effect);
        }
        else
        {
            int id;
            if (int.TryParse(key.Substring(2), out id))
            {
                effect = AddEffect(effectData, id);
                if (effect != null)
                {
                    fxManager.ReactToEffectApply(effect);
                }
            }
        }

        
    }

    /// <summary>
    /// Adds effect. Works LOCALLY (without sending network message). Every client must run it manually.
    /// </summary>
    private VehicleEffect AddEffect(VehicleEffectData effectData, int id)
    {
        string propertyKey = GetNewEffectPropertyKey(id);
        VehicleEffect effect = new VehicleEffect(effectData, owner, propertyKey);
        
        // Если эффект перманентный, не сохраняем его в PlayerProperties, а только изменяем параметры юнита (в конструкторе VehicleEffect)
        if (!effect.MustBeReturned) 
            return null;

        effects.Add(effect);
        if (!owner.IsBot)
        {
            effectsDic[propertyKey] = effect;
        }
        else
        {
            int playerPropIndex = MiscTools.FindFirstInString(propertyKey, char.IsLetter, 2);
            effectsDic[propertyKey.Substring(playerPropIndex)] = effect;
        }

        if (owner.IsMain)
        {
            IEPanel.Instance.AddCell(effect);
        }

        return effect;
    }

    /// <summary>
    /// Cancels effect. Works LOCALLY (without sending network message). Every client must run it manually.
    /// </summary>
    private void CancelEffect(VehicleEffect effect)
    {
        effect.Cancel();
        fxManager.ReactToEffectCancel(effect);
        effects.Remove(effect);
        string keyToRemove = null;
        foreach (var efPair in effectsDic)
        {
            if (efPair.Value == effect)
            {
                keyToRemove = efPair.Key;
                break;
            }
        }

        if (keyToRemove != null)
        {
            effectsDic.Remove(keyToRemove);
        }

        if (owner.PhotonView.isMine)
        {
            DeletePlayerProperty(effect);
        }
    }

    /// <summary>
    /// Cancels all effects. Works LOCALLY (without sending network message). Every client must run it manually.
    /// </summary>
    public void CancelAllEffects()
    {
        foreach (var effect in effects)
        {
            effect.Cancel();
            fxManager.ReactToEffectCancel(effect);

            if (owner.PhotonView.isMine)
            {
                DeletePlayerProperty(effect);
            }
        }

        effects.Clear();
        effectsDic.Clear();

        if (owner.IsMain)
        {
            IEPanel.Instance.RemoveAllCells();
        }
    }

    private void DeletePlayerProperty(VehicleEffect effect)
    {
        Hashtable prps = new Hashtable() { {effect.effectPropertyKey, null} };
        if (owner.IsBot)
        {
            PhotonNetwork.room.SetCustomProperties(prps);
        }
        else
        {
            PhotonNetwork.player.SetCustomProperties(prps);
        }
    }
}