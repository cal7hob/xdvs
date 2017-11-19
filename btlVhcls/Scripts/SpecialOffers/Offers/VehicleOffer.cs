public class VehicleOffer : ShopOffer
{
    private VehicleInfo vehicleShopItem;

    public override bool IsOwned { get { return ProfileInfo.vehicleUpgrades.ContainsKey(Id); } }
    protected override bool IsInShop { get { return GUIPager.ActivePageName == VehicleShop.Instance.GuiPageName; } }

    public override void Initialize(int id, int discount, double endTime)
    {
        base.Initialize(id, discount, endTime);

        if (VehicleShop.Selectors.ContainsKey(Id))
            saleSticker = VehicleShop.Selectors[Id].saleSticker;
        else
        {
            offerFrame.Hide();
            VehicleOffersController.Instance.Offers.Remove(id); 
            Destroy(this);
        }

        Dispatcher.Subscribe(EventId.VehicleShopFilled, UpdateSaleSticker);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy ();
        Dispatcher.Unsubscribe(EventId.VehicleShopFilled, UpdateSaleSticker);
        offerFrame.btnBuy.OnClickUIItem -= SpecialOffersPage.VehicleOffersBtnClickHandler;
    }

    protected override void UpdateSaleSticker(EventId eventId, EventInfo eventInfo)
    {
        saleSticker = VehicleShop.Selectors[Id].saleSticker;
        UpdateItem();
    }

    public override void UpdateItem()
    {
        base.UpdateItem();

        if (Remain > 0)
        {
            if (!ProfileInfo.vehicleUpgrades.ContainsKey(Id))
            {
                if(!saleSticker || VehicleShop.Selectors[Id].ShopItem.HideCondition || VehicleShop.Selectors[Id].ShopItem.ComingSoonCondition) return;
                offerFrame.Show();
                saleSticker = VehicleShop.Selectors[Id].saleSticker;
                saleSticker.SetActive(true);
                //saleSticker.Text = "SALE\n" + Discount + "%";
                saleSticker.Text = SpecialOffersPage.Instance.GetSaleStickerText(Discount);
                return; 
            }
            if (saleSticker) saleSticker.SetActive(false);
            offerFrame.Hide();
            return;
        }
        if (saleSticker) saleSticker.SetActive(false);
       
        if(!VehicleShop.Instance) return;

        if (VehicleShop.Instance.IsInShop && !ProfileInfo.vehicleUpgrades.ContainsKey(Shop.VehicleInView.Info.id) && Shop.VehicleInView.Info.id == Id)
        {
            HangarController.Instance.SetActionBoxType(HangarController.ActionBoxType.Buy);
            HangarController.Instance.buyingBox.Price = VehicleShop.Selectors[Id].UserVehicle.Info.price;
        }
    }

    protected override void SetInfo(EventId eventId = 0, EventInfo info = null)
    {
        offerFrame.info.text = VehiclePool.Instance.GetItemById(id).vehicleName;
    }

    protected override void SetBtn()
    {
        base.SetBtn();
        offerFrame.btnBuy.OnClickUIItem += SpecialOffersPage.VehicleOffersBtnClickHandler;
    }

    protected override void SetPrice()
    {
        offerFrame.oldPriceRenderer.OldPrice = vehicleShopItem.Price;
        discountPrice = new ProfileInfo.Price((int)(vehicleShopItem.Price.value * (1f - discount * 0.01f)), vehicleShopItem.Price.currency);
        offerFrame.priceRenderer.Price = discountPrice;
    }

    protected override void SetSprite()
    {
        
    }

    protected override void SetShopItem()
    {
        vehicleShopItem = VehiclePool.Instance.GetItemById(Id);
    }

    protected override void OnTick(double tick)
    {   
        base.OnTick(tick);
        if(Remain > 0 && !IsOwned)return;
        HangarController.OnTimerTick -= OnTick;
        UpdateItem();
    }
}
