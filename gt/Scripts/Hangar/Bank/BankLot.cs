using System.Collections.Generic;
using UnityEngine;

public class BankLot : BankLotBase
{
    [SerializeField] private PriceRenderer priceRenderer;
    [SerializeField] private OldPriceRenderer oldPriceRenderer;
    [SerializeField] private SaleSticker saleSticker;
    [SerializeField] private tk2dTextMesh localizedPrice;
    [SerializeField] private tk2dSprite mainSprite;
    
    
    [SerializeField] private HorizontalLayout[] horizontalLayouts;
    [SerializeField] private Color extraFreeCustomColor = Color.white;
    [SerializeField] private GameObject[] objectsActivatedForGold;
    [SerializeField] private GameObject[] objectsActivatedForSilver;
    private tk2dTextMesh lbl = null;
    private int val = 0;
    public string IapId { get; set; }

    public string UnibillerItemId { get; set; }

    public void Init()
    {
        if (!BankData.prices.ContainsKey(UnibillerItemId))
            return;
        //Debug.LogErrorFormat("Bank lot {0} Inited", UnibillerItemId);

        var lot = BankData.prices[UnibillerItemId];

        mainSprite.SetSprite(lot.SpriteName);
        localizedPrice.text = PriceLocalizationAgent.GetLocalizedString(UnibillerItemId, localizedPrice, lot.LocalizationKey);

        MiscTools.SetObjectsActivity(objectsActivatedForGold, lot.currency == ProfileInfo.PriceCurrency.Gold);
        MiscTools.SetObjectsActivity(objectsActivatedForSilver, lot.currency == ProfileInfo.PriceCurrency.Silver);

        #region Меняем параметры в соответствии с текущими акциями
        Dictionary<string, BankOffer> currentBankOffers = BankOffersController.GetBankOffersByCurrency(lot.currency);

        saleSticker.SetActive(false);
        oldPriceRenderer.SetActive(false);

        bool isLimitReached = (
            ((lot.currency == ProfileInfo.PriceCurrency.Gold) && ProfileInfo.IsGoldDiscountLimitReached)
            ||
            ((lot.currency == ProfileInfo.PriceCurrency.Silver) && ProfileInfo.IsSilverDiscountLimitReached)
        );

        if (currentBankOffers.ContainsKey(UnibillerItemId) && !isLimitReached)//если акция на текущий лот
        {
            oldPriceRenderer.SetActive(true);
            saleSticker.SetActive(true);

            priceRenderer.Price = currentBankOffers[UnibillerItemId].Price;
            oldPriceRenderer.OldPrice = BankData.prices[UnibillerItemId].FullPrice;
            SetSaleStickerText(saleSticker.lblSaleText, currentBankOffers[UnibillerItemId].Price.value - BankData.prices[UnibillerItemId].FullPrice.value);
        }
        else if (currentBankOffers.Count > 0)//акция есть, но не на текущий лот
        {
            priceRenderer.Price = BankData.prices[UnibillerItemId].FullPrice;
        }
        else//Если нет акции
        {
            priceRenderer.Price = BankData.prices[UnibillerItemId].FullPrice;
            saleSticker.SetActive(true);
            SetSaleStickerText(saleSticker.lblSaleText, BankData.prices[UnibillerItemId].ExtraFreeValue);
        }
        #endregion

        if (horizontalLayouts != null) //Перестраховка, чтобы не рассчитывать на порядок выполнения скриптов и выполнить выравнивание после включения текста с зачеркнутой ценой
            for (int i = 0; i < horizontalLayouts.Length; i++)
                if (horizontalLayouts[i])
                    horizontalLayouts[i].Align();
    }

    private void SetSaleStickerText(tk2dTextMesh lbl, int val)
    {
        saleSticker.SetTextWithFormatString(
            MiscTools.GetCultureSpecificFormatOfNumber(val),
            lbl.inlineStyling ? HelpTools.To2DToolKitColorFormatString(extraFreeCustomColor) : "",
            Localizer.GetText("lblFree"));
    }

    private void OnClick()
    {
        if (Bank.Instance.goldScrollArea.IsSwipeScrollingInProgress || Bank.Instance.silverScrollArea.IsSwipeScrollingInProgress)
            return;
        //BankAndroid_Unibill.Instance.BuyUnibillerItemClick(mainSprite.transform, UnibillerItemId);
        IapManager.BuyProductId(mainSprite.transform, UnibillerItemId);
    }
}
