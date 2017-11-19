using System.Collections.Generic;
using UnityEngine;
using System.Collections;

/// <summary>
/// vip offer used in vip account shop
/// </summary>
public class VipOffer : IVipOffer
{
    private int _id;
    private ProfileInfo.Price _price;
    private int _durationInSeconds;

    public VipOffer(int id, ProfileInfo.Price price, int duration)
    {
        _id = id;
        _price = price;
        _durationInSeconds = duration;
    }


    public int Id
    {
        get { return _id; }
    }


    public ProfileInfo.Price Price
    {
        get { return _price; }
    }


    /// <summary>
    /// expiration time in seconds of vip account offer
    /// </summary>
    public int DurationInSeconds
    {
        get { return _durationInSeconds; }
    }


    /// <summary>
    /// expiration time in days of vip account offer
    /// </summary>
    public int DurationInDays
    {
        get { return DurationInSeconds / Clock.DAY_SECONDS; }
    }


    /// <summary>
    /// parce dictionary and get IVipOffer
    /// </summary>
    /// <param name="offersDictionary"></param>
    /// <returns></returns>
    public static IVipOffer ParseOffers(IDictionary<string, object> offersDictionary)
    {
        JsonPrefs prefs = new JsonPrefs(offersDictionary);

        int id = prefs.ValueInt("id");
        int duration = prefs.ValueInt("duration");
        var price = prefs.ValuePrice("price");

        return price == null ? null : new VipOffer(id, price, duration);
    }
}

