using System.Linq;
using UnityEngine;

public class PatternOffer : ShopOffer
{
    protected Bodykit bodykit;

    public override bool IsOwned { get { return Shop.CurrentVehicle.Upgrades.OwnedCamouflages.Any(camo => camo.id == Id); } }
    protected override bool IsInShop { get { return PatternShop.Instance && GUIPager.ActivePageName == PatternShop.Instance.GuiPageName; } }

    override protected void Awake()
    {
        base.Awake ();
        Dispatcher.Subscribe(EventId.OnLanguageChange, SetInfo);
    }

    public override void Initialize(int id, int discount, double endTime)
    {
        base.Initialize(id, discount, endTime);
        Dispatcher.Subscribe(EventId.PatternShopFilled, UpdateSaleSticker);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy ();
        Dispatcher.Unsubscribe(EventId.PatternShopFilled, UpdateSaleSticker);
        Dispatcher.Unsubscribe(EventId.OnLanguageChange, SetInfo);
        offerFrame.btnBuy.OnClickUIItem -= SpecialOffersPage.PatternOffersBtnClickHandler;
    }

    protected override void UpdateSaleSticker(EventId eventId, EventInfo eventInfo)
    {
        if(PatternShop.Selectors.ContainsKey(Id))
            saleSticker = PatternShop.Selectors[Id].saleSticker;
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
                offerFrame.Show();
                if (!saleSticker) return; 
                saleSticker.SetActive(true);
                //saleSticker.Text = "SALE\n" + Discount + "%";    
                saleSticker.Text = SpecialOffersPage.Instance.GetSaleStickerText(Discount);

                return;
            }

            offerFrame.Hide();
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
        discountPrice = new ProfileInfo.Price((int)(bodykit.Price.value * (1f - discount * 0.01f)), bodykit.Price.currency);
        offerFrame.priceRenderer.Price = discountPrice;
    }

    protected override void SetSprite()
    {
        offerFrame.sprProduct.SetSprite(bodykit.IdString);
    }

    protected override void SetShopItem()
    {
        bodykit = PatternPool.Instance.GetItemById(Id);
    }

    protected virtual void ResetItem()
    {
        if (saleSticker) saleSticker.SetActive(false);
        HangarController.OnTimerTick -= OnTick;
        if (!IsOwned && IsInShop)
        {
            HangarController.Instance.SetActionBoxType(HangarController.ActionBoxType.Buy);
            HangarController.Instance.buyingBox.Price = bodykit.Price;
        }
    }
}
