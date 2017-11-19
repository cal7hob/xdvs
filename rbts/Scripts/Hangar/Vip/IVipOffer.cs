using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public interface IVipOffer
{
    int Id { get; }
    ProfileInfo.Price Price { get; }
    int DurationInSeconds { get; }
    int DurationInDays { get; }

    //IVipOffer ParseOffers(IDictionary<string, object> offersDictionary);
}