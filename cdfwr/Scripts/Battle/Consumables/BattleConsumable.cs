﻿using System;
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

    public readonly string prefabName;

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

    public BattleConsumable(VehicleController owner, ConsumableInfo consumableInfo)
    {
        this.consumableInfo = consumableInfo;
        id = consumableInfo.id;
        name = consumableInfo.name;
        readyTime = Time.time + consumableInfo.firstDelay;
        firstReload = true;
        prefabName = consumableInfo.prefabName;
        this.owner = owner;
    }

    public bool Use()
    {
        if (!CanBeUsed(owner))
        {
            return false;
        }

        Manager.BattleServer.UseConsumable(id, null);
        Amount -= 1;
        readyTime = Time.time + consumableInfo.reloadTime;
        firstReload = false;

        if (consumableInfo.effects != null)
        {
            ApplyEffects();
        }

        if (!string.IsNullOrEmpty(prefabName))
        {
            PhotonNetwork.Instantiate(string.Format("{0}/{1}", GameManager.CurrentResourcesFolder, prefabName),
            owner.transform.position, owner.transform.rotation, 0, new object[]{owner.data.playerId, (int)consumableInfo.id});
        }

        if (owner.PhotonView && owner.PhotonView.isMine)
            Dispatcher.Send(EventId.ConsumableUsed, new EventInfo_II(owner.data.playerId, id), Dispatcher.EventTargetType.ToAll);

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
                owner.ApplyEffect(effect);
            }
            else
            {
                Dispatcher.Send(EventId.TankEffectRequest, new EventInfo_IE(owner.Target.data.playerId, effect), owner.Target.IsBot ? Dispatcher.EventTargetType.ToMaster : Dispatcher.EventTargetType.ToSpecific, owner.Target.data.playerId);
            }
        }
    }
    
    private bool CanBeUsed(VehicleController user)
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
                return user.Target != null && StaticContainer.AreFriends(user, user.Target);
            case ConsumableTargetType.Enemy:
                return user.Target != null && !StaticContainer.AreFriends(user, user.Target);
        }

        return false;
    }
}