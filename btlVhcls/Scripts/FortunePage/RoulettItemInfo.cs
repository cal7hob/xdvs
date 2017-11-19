using System;
using UnityEngine;
using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;
using XDevs;

public class RoulettItemInfo
{
    public readonly ObscuredInt sector;
    public Entity entity;
    public BgType bgType;

    public int SectorIndex { get { return sector - 1; } }

    public RoulettItemInfo(Dictionary<string, object> initDict)
    {
        int intSector = 0;

        bool allDataReceived = true;
        allDataReceived &= initDict.Extract("sector", ref intSector);
        initDict.Extract("backColor", ref bgType, false);//Может и не прийти, поэтому не проверяем в allDataReceived

        if (!allDataReceived)
            Debug.LogErrorFormat("Hangar Roulett parsing errors!!! Sector  = {0}", intSector);

        sector = intSector;

        entity = new Entity(initDict);
    }
}
