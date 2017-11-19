using System.Collections.Generic;
using UnityEngine;
using System.Collections;

/// <summary>
/// vip offer used in vip account shop
/// </summary>
public class VipOffer
{
    private string   stringID = "";
    private int      durationInSeconds = 0;

    public VipOffer(string id, int duration)
    {
        stringID = id;
        durationInSeconds = duration;
    }

    public string StringId
    {
        get
        {
            return stringID;
        }
    }

    /// <summary>
    /// expiration time in seconds of vip account offer
    /// </summary>
    public int Duration
    {
        get
        {
            return durationInSeconds;
        }
    }
}

