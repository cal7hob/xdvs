using System.Collections.Generic;
using System;
using UnityEngine;

public class MapInfo
{
    public readonly GameManager.MapId id;
    public readonly bool isEnabled = false;
    public readonly int mapLevel = 0;
    public readonly bool isTutorialMap = false;
    public readonly int fuelRequired = 0;
    public readonly int order = 0;

    public bool IsAvailableByLevel { get { return mapLevel <= ProfileInfo.Level; } }
    //public bool IsEnoughFuel { get { return ProfileInfo.Fuel >= fuelRequired; } }

    public MapInfo(Dictionary<string, object> initDict, int order)
    {
        if (!initDict.Extract("mapId", ref id) ||
            !initDict.Extract("isEnabled", ref isEnabled) ||
            !initDict.Extract("mapLevel", ref mapLevel) ||
            !initDict.Extract("tutorMap", ref isTutorialMap) ||
            !initDict.Extract("fuelRequired", ref fuelRequired)
           )
        {
            Debug.LogError("Hangar maps parsing errors!!!");
        }

        //id = (GameManager.MapId)Convert.ToInt32(initDict["mapId"]);
        //isEnabled = (bool)initDict["isEnabled"];
        //mapLevel = Convert.ToInt32(initDict["mapLevel"]);
        //isTutorialMap = (bool)initDict["tutorMap"];
        //fuelRequired = Convert.ToInt32(initDict["fuelRequired"]);
    }
}
