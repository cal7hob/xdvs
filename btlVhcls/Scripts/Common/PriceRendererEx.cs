using UnityEngine;
using System.Collections;

public class PriceRendererEx : PriceRenderer
{
    [SerializeField] private tk2dBaseSprite[] goldSprites;
    [SerializeField] private tk2dBaseSprite[] silverSprites;
    
    protected override void UpdateElements()
    {
        MiscTools.SetObjectsActivity(goldSprites, Price.currency == ProfileInfo.PriceCurrency.Gold);
        MiscTools.SetObjectsActivity(silverSprites, Price.currency == ProfileInfo.PriceCurrency.Silver);

        if(sprCoins)
            sprCoins.SetSprite(Price.SpriteName);

        amount.text = Price.LocalizedValue;
        amount.SetMoneySpecificColorIfCan(Price);
    }
}
