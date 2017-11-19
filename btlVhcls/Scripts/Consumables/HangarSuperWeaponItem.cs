using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

//Если начнет сильно отличаться от HangarConsumableItem - убрать наследование и копирнуть
public class HangarSuperWeaponItem : HangarConsumableItem
{
    [Header("HangarSuperWeaponItem")]
    [SerializeField] private tk2dTextMesh lblBuy;
    [SerializeField] private tk2dTextMesh lblDuration;

    private bool lastLifeState = false;

    protected override void Awake()
    {
        Dispatcher.Subscribe(EventId.HangarTimerTick, OnTimerTick);
        base.Awake();
    }

    protected override void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.HangarTimerTick, OnTimerTick);
        base.OnDestroy();
    }

    public override void UpdateElements()
    {
        lblDescription.text = GameData.consumableInfos[ConsumableId].LocalizedDescription;
        setPriceScript.Price = GameData.consumableInfos[ConsumableId].price.ToPrice();
        HelpTools.SetSpriteToAllSpritesInCollection(sprites, GameData.consumableInfos[ConsumableId].GetIcon(withFrame: true));
        vipIconWrapper.SetActive(GameData.consumableInfos[ConsumableId].isVip);
        if (GameData.consumableInfos[ConsumableId].isHidden)
            btnBuy.Activated = false;
        //пока targetInventoryPanel не проснулась - не имеет смысла. Если еще не проснулась, значит будет автозаполнение в InventoryBase.Start()
        checkBox_ForBattle.IsOn = targetInventoryPanel.IsAwaked ? targetInventoryPanel.HasFactoryItemWithContentId(ConsumableId) : false;
        lblBuy.text = string.Format ("{0} ({1})", Localizer.GetText("lblBuy"), Clock.GetTimerString(Convert.ToInt64((int)GameData.consumableInfos[ConsumableId].lifetime), true));
        UpdateTimer();
    }

    private void UpdateTimer()
    {
        lastLifeState = IsAlive;
        lblDuration.text = lastLifeState ?
            Clock.GetTimerString(Convert.ToInt64((double)ProfileInfo.consumableInventory[ConsumableId].deathTime - GameData.CurrentTimeStamp), true) :
            "";
    }

    private void OnTimerTick(EventId evId, EventInfo ev)
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (IsAlive)
        {
            UpdateTimer();
        }
        else// !IsAlive
        {
            if (lastLifeState)//Убираем из панели инвентаря при окончании времени действия
            {
                ProfileInfo.consumableInventory.Remove(ConsumableId);//По идее можно и не удалять, есть же еще IsAlive, но так надежнее.
                Dispatcher.Send(EventId.ChangeConsumableInventoryState, new EventInfo_U(ConsumableId, false, -1));
            }
                
        }
    }
}
