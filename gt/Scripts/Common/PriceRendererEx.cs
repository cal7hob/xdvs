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

        amount.text = Price.LocalizedValue;
        Price.SetMoneySpecificColorIfCan(amount);
    }
}
