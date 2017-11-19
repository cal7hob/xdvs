using System;
using System.Linq;
using UnityEngine;

public class DecalOffer : PatternOffer
{
    public override bool IsOwned { get { return Shop.CurrentVehicle.Upgrades.OwnedDecals.Any(decal => decal.id == Id); } }
    protected override bool IsInShop { get { return DecalShop.Instance && GUIPager.ActivePage == DecalShop.Instance.GuiPageName; } }

    public override void SubscribeOnTimer()
    {
        HangarController.OnTimerTick += OnTick;      
    }

    public override void Initialize(int id, int discount, double endTime)
    {
        this.id = id;
        this.discount = discount;
        this.endTime = endTime;

        InitializeOfferFrame();
        Messenger.Subscribe(EventId.DecalShopFilled, UpdateSaleSticker);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy ();
        Messenger.Unsubscribe(EventId.DecalShopFilled, UpdateSaleSticker);
        Messenger.Unsubscribe(EventId.OnLanguageChange, SetInfo);
        offerFrame.btnBuy.OnClickUIItem -= SpecialOffersPage.DecalOffersBtnClickHandler;
    }

    protected override void UpdateSaleSticker(EventId eventId, EventInfo eventInfo)
    {   
        saleSticker = DecalShop.Selectors[Id].saleSticker;
        UpdateItem();
    }

    protected override void OnTick(double tick)
    {
        base.OnTick(tick);

        if (Remain > 0 && !IsOwned) return;
        HangarController.OnTimerTick -= OnTick;
        UpdateItem();
    }

    protected override void ResetItem()
    {
        if (saleSticker) saleSticker.SetActive(false);
        HangarController.OnTimerTick -= OnTick;
        if (!IsOwned && IsInShop)
        {
            HangarController.Instance.SetActionBoxType(HangarController.ActionBoxType.Buy);
            HangarController.Instance.buyingBox.Price = bodykit.Price;
        }
    }

    protected override void SetInfo(EventId eventId = 0, EventInfo info = null)
    {
        GetComponent<BonusStatsLabel>().Show(bodykit);
    }

    //protected override void SetPrice()
    //{
    //    offerFrame.oldPriceRenderer.OldPrice = bodykit.Price;
    //    discountPrice = new ProfileInfo.Price((int) (bodykit.Price.value*(1 - discount*0.01f)), bodykit.Price.currency);
    //    offerFrame.priceRenderer.Price = discountPrice;
    //}

    protected override void SetBtn()
    {
        offerFrame.lblBuy.text = Localizer.GetText("lblBuy");
        offerFrame.btnBuy.OnClickUIItem += SpecialOffersPage.DecalOffersBtnClickHandler;
    }

    protected override void SetSprite()
    {
        offerFrame.sprProduct.SetSprite(bodykit.IdString);
    }

    protected override void SetShopItem()
    {
        bodykit = DecalPool.Instance.GetItemById(Id);
    }
}
