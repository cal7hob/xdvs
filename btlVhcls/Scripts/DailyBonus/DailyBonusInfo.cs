using System;
using UnityEngine;
using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;
using XDevs;

public class DailyBonusInfo
{
    public readonly ObscuredInt day;
    public BgType bgType;
    public Entity entity;

    public DailyBonusInfo(Dictionary<string, object> initDict)
    {
        int intDay = 0;

        bool allDataReceived = true;
        allDataReceived &= initDict.Extract("day", ref intDay);
        initDict.Extract("backColor", ref bgType, false);//Может и не прийти, поэтому не проверяем в allDataReceived

        if (!allDataReceived)
            Debug.LogErrorFormat("Hangar DailyBonus parsing errors!!! day number  = {0}", intDay);

        day = intDay;

        entity = new Entity(initDict);
    }
}
