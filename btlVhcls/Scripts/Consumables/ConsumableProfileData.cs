using System;
using UnityEngine;
using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;

public class ConsumableProfileData
{
    public readonly ObscuredInt id;
    public readonly ObscuredInt count;
    public readonly ObscuredInt deathTime;

    public ConsumableProfileData(Dictionary<string, object> initDict)
    {
        int intId = 0;
        int intCount = 0;
        int intDeathTime = 0;

        bool allDataReceived = true;
        allDataReceived &= initDict.Extract("id", ref intId);
        allDataReceived &= initDict.Extract("count", ref intCount);
        /*allDataReceived &= */initDict.Extract("deathTime", ref intDeathTime);

        if (!allDataReceived)
            Debug.LogErrorFormat("Profile consumable parsing errors!!! consumable id = {0}", intId);

        id = intId;
        count = intCount;
        deathTime = intDeathTime;
        //deathTime = (int)GameData.CurrentTimeStamp + 60;
    }
}
