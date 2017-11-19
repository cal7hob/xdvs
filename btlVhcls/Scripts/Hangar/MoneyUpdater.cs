using System;
using UnityEngine;

public class MoneyUpdater : MonoBehaviour
{
    public tk2dTextMesh goldLable;
    public tk2dTextMesh silverLable;
    
    void Awake()
    {
        Dispatcher.Subscribe(EventId.ProfileMoneyChange, MoneyChanged_Handler);
        Dispatcher.Subscribe(EventId.AfterHangarInit, MoneyChanged_Handler);
        UpdateMoney();
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.ProfileMoneyChange, MoneyChanged_Handler);
        Dispatcher.Unsubscribe(EventId.AfterHangarInit, MoneyChanged_Handler);
    }

    private void MoneyChanged_Handler(EventId eventId, EventInfo eventInfo)
    {
        UpdateMoney();
    }

    private void UpdateMoney()
    {
        goldLable.text = MiscTools.GetCultureSpecificFormatOfNumber(ProfileInfo.Gold);
        silverLable.text = MiscTools.GetCultureSpecificFormatOfNumber(ProfileInfo.Silver);
    }
}
