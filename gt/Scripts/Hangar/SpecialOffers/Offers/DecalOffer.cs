using System.Linq;

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
        base.Initialize(id, discount, endTime);
        Dispatcher.Subscribe(EventId.DecalShopFilled, UpdateSaleSticker);
    }

    protected override void OnDestroy()
    {
        UnsubscribeFromTimer();
        Dispatcher.Unsubscribe(EventId.DecalShopFilled, UpdateSaleSticker);
        Dispatcher.Unsubscribe(EventId.OnLanguageChange, SetInfo);
    }

    protected override void UpdateSaleSticker(EventId id, EventInfo info)
    {
        if (DecalShop.Selectors.ContainsKey(Id))
        {
            saleSticker = DecalShop.Selectors[Id].saleSticker;
        }

        UpdateItem();
    }

    protected override void SetBtn()
    {
        offerFrame.lblBuy.text = Localizer.GetText("lblBuy");
        offerFrame.btnBuy.OnClickUIItem += SpecialOffersPage.DecalOffersBtnClickHandler;
    }

    protected override void SetSprite()
    {
        offerFrame.sprProduct.SetSprite(bodykit.IconSprite.CurrentSprite.name);
    }

    protected override void SetShopItem()
    {
        bodykit = DecalPool.Instance.GetItemById(Id);
    }
}
