using UnityEngine;
using System.Collections;

public interface IVipOfferPrefab
{
    int OfferId{ get; set; }
    int ShopPosition { get; set; }
    float Margin { get; set; }
    float OfferWidth { get; }
    string OfferUnibillerId { get; set; }
}
