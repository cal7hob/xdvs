using System;
using UnityEngine;
using System.Collections;

public class MoneyUpdater : MonoBehaviour
{
    public tk2dTextMesh goldLable;
    public tk2dTextMesh silverLable;
    
    
    void Awake()
    {
        Dispatcher.Subscribe(EventId.ProfileMoneyChange, MoneyChanged_Handler);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.ProfileMoneyChange, MoneyChanged_Handler);
    }


    private void MoneyChanged_Handler(EventId eventId, EventInfo eventInfo)
    {
        int gold = ((EventInfo_II) eventInfo).int1;
        int silver = ((EventInfo_II)eventInfo).int2;

        goldLable.text = gold.ToString ("N0", GameData.instance.cultureInfo.NumberFormat);
        silverLable.text = silver.ToString ("N0", GameData.instance.cultureInfo.NumberFormat);
    }
}
