using System.Linq;
using UnityEngine;

public class PatternOffer : ShopOffer
{
    protected Bodykit bodykit;

    public override bool IsOwned { get { return Shop.CurrentVehicle.Upgrades.OwnedCamouflages.Any(camo => camo.id == Id); } }
    protected override bool IsInShop { get { return PatternShop.Instance && GUIPager.ActivePage == PatternShop.Instance.GuiPageName; } }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Dispatcher.Unsubscribe(EventId.PatternShopFilled, UpdateSaleSticker);
    }

    public override void Initialize(int id, int discount, double endTime)
    {
        base.Initialize(id, discount, endTime);
        Dispatcher.Subscribe(EventId.PatternShopFilled, UpdateSaleSticker);
    }

    protected override void UpdateSaleSticker(EventId id, EventInfo info)
    {
        if (PatternShop.Selectors.ContainsKey(Id))
        {
            saleSticker = PatternShop.Selectors[Id].saleSticker;
        }

        UpdateItem();
    }

    protected override void OnTick(double tick)
    {
        base.OnTick(tick);

        if (Remain > 0 && !IsOwned) return;
        HangarController.OnTimerTick -= OnTick;
        UpdateItem();
    }

    public override void UpdateItem()
    {
        base.UpdateItem();

        SetPrice();

        if (Remain > 0)
        {
            if (!IsOwned)
            {
                offerFrame.SetActive(true);
                if (!saleSticker) return;
                saleSticker.SetActive(true);
                //saleSticker.Text = "SALE\n" + Discount + "%";    
                saleSticker.Text = SpecialOffersPage.Instance.GetSaleStickerText(Discount);

                return;
            }

            offerFrame.SetActive(false);
            ResetItem();
            return;
        }

        ResetItem();
    }

    protected override void SetBtn()
    {
        base.SetBtn();
        offerFrame.btnBuy.OnClickUIItem += SpecialOffersPage.PatternOffersBtnClickHandler;
    }

    protected override void SetInfo(EventId eventId = 0, EventInfo info = null)
    {
        GetComponent<BonusStatsLabel>().Show(bodykit);
    }

    protected override void SetPrice()
    {
        offerFrame.oldPriceRenderer.OldPrice = bodykit.Price;
        discountPrice = new ProfileInfo.Price((int)(bodykit.Price.value * (1 - discount * 0.01f)), bodykit.Price.currency);
        offerFrame.priceRenderer.Price = discountPrice;
    }

    protected override void SetSprite()
    {
        offerFrame.sprProduct.SetSprite(bodykit.IconSprite.CurrentSprite.name);
    }

    protected override void SetShopItem()
    {
        bodykit = PatternPool.Instance.GetItemById(Id);
    }

    protected virtual void ResetItem()
    {
        if (saleSticker)
        {
            saleSticker.SetActive(false);
        }
        HangarController.OnTimerTick -= OnTick;
        if (!IsOwned && IsInShop)
        {
            MenuController.SetActionBoxType(ActionBoxType.Buy);
            MenuController.Instance.buyingBox.Price = bodykit.Price;
        }
    }
}
