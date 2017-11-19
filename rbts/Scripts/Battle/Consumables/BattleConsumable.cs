using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;
using Http;

public enum ConsumableTargetType
{
    None,
    Friend,
    Enemy
}

public enum DamageCalcContext
{
    None,
    Owner,
    Target
}

public class BattleConsumable
{
    public readonly ConsumableInfo consumableInfo;
    public readonly ObscuredInt id;
    public readonly VehicleController owner;
    public readonly string name;

    public ObscuredInt Amount
    {
        get { return BattleController.battleInventory[id]; }
        protected set { BattleController.battleInventory[id] = value; }
    }

    private readonly string prefabName;
    private readonly string mapPrefabName;

    public bool ReadyForUse
    {
        get { return Time.time >= readyTime; }
    }

    public float ReloadProgress
    {
        get
        {
            return Mathf.Clamp01(1 - (readyTime - Time.time) / (firstReload ? consumableInfo.firstDelay : consumableInfo.reloadTime));
        }
    }

    private float readyTime;
    private bool firstReload;

    public BattleConsumable(VehicleController owner, ConsumableInfo consumableInfo, string mapName = null)
    {
        this.consumableInfo = consumableInfo;
        id = consumableInfo.id;
        name = consumableInfo.name;
        readyTime = Time.time + consumableInfo.firstDelay;
        firstReload = !Mathf.Approximately(consumableInfo.firstDelay, 0f);
        prefabName = consumableInfo.prefabName;
        mapPrefabName = string.Format("{0}_{1}", prefabName, mapName);
        this.owner = owner;
    }

    public bool Use()
    {
        if (!CanBeUsed)
        {
            return false;
        }

        Manager.BattleServer.UseConsumable(id, null);
        --Amount;
        readyTime = Time.time + consumableInfo.reloadTime;
        firstReload = false;

        if (!string.IsNullOrEmpty(prefabName))
        {
            if (!InstantiatePrefab(mapPrefabName, false))
            {
                InstantiatePrefab(prefabName, true);
            }
        }
        else
        {
            if (consumableInfo.effects != null)
            {
                ApplyEffects();
            }
        }

        if (owner.PhotonView.isMine)
        {
            Messenger.Send(EventId.ConsumableUsed, new EventInfo_II(owner.data.playerId, id), Messenger.EventTargetType.ToAll);
        }

        return true;
    }

    public void Reload()
    {
        readyTime = Time.time;
    }

    private void ApplyEffects()
    {
        foreach (var effect in consumableInfo.effects)
        {
            if (consumableInfo.targetType == ConsumableTargetType.None)
            {
                owner.RequestEffect(effect);
            }
            else
            {
                owner.Target.RequestEffect(effect);
            }
        }
    }
    
    private bool CanBeUsed
    {
        get
        {
            if (!PhotonNetwork.inRoom || Amount < 1 || !ReadyForUse)
            {
                return false;
            }

            switch (consumableInfo.targetType)
            {
                case ConsumableTargetType.None:
                    return true;
                case ConsumableTargetType.Friend:
                    return owner.Target != null && VehicleController.AreFriends(owner, owner.Target);
                case ConsumableTargetType.Enemy:
                    return owner.Target != null && !VehicleController.AreFriends(owner, owner.Target);
            }

            return false;
        }
    }

    private bool InstantiatePrefab(string prefName, bool showIfAbsent)
    {
        int targetId = owner.Target != null ? owner.Target.data.playerId : -1;

        return PhotonNetwork.Instantiate(
            string.Format("{0}/ConsumableItems/{1}", GameManager.CurrentResourcesFolder, prefName),
            owner.transform.position, owner.transform.rotation, 0,
            new object[] {owner.data.playerId, (int) consumableInfo.id, targetId}, showIfAbsent) != null;
    }
}