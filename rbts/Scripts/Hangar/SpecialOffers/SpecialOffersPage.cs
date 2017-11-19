using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class SpecialOffersPage : MonoBehaviour
{
    public int saleStickerUpdateInterval = 2;
    public SaleSticker saleSticker;
    public tk2dBaseSprite sprBlackFriday;
    private List<SpecialOfferPrefab> specialOfferFrames = new List<SpecialOfferPrefab>();
    [SerializeField] private tk2dTextMesh lblTimeLimitedSales;
    [SerializeField] private tk2dTextMesh lblRemains;
    [SerializeField] private GameObject remainsLayout;

    [Header("Frame prefab settings:")]
    public SpecialOfferPrefab bankOfferPrefab;
    public SpecialOfferPrefab decalOfferPrefab;
    public SpecialOfferPrefab patternOfferPrefab;
    public SpecialOfferPrefab vipOfferPrefab;
    public SpecialOfferPrefab vehicleOfferPrefab;
    public tk2dUIScrollableArea scrollArea;
    public float framesPosY;
    public Vector2 frameDimensions;
    public float framesOffset;

    public Color saleWordCustomColor = Color.white;
    public Color percentCustomColor = Color.white;
    public Color percentSymbolCustomColor = Color.white;

    public static bool IsSaleGoing
    {
        get
        { 
            return VehicleOffersController.Instance.AnyItemIsOnSale | VipOffersController.Instance.AnyItemIsOnSale |
                   DecalOffersController.Instance.AnyItemIsOnSale | PatternOffersController.Instance.AnyItemIsOnSale |
                   BankOffersController.Instance.AnyItemIsOnSale;
        }
    }

    public static string PageName { get; private set; }

    public static SpecialOffersPage Instance { get; private set; }
    public static tk2dUIScrollableArea ScrollArea { get { return Instance.scrollArea; } }
    public static List<SpecialOfferPrefab> SpecialOfferFrames { get { return Instance.specialOfferFrames; } }

    void Awake()
    {
        PageName = "SpecialOffersPage";
        Instance = this;
        Messenger.Subscribe(EventId.AfterHangarInit, Init, 4);
        Messenger.Subscribe(EventId.SpecialOffersInitialized, Init);
    }

    void OnDestroy()
    {
        Instance = null;
        Messenger.Unsubscribe(EventId.AfterHangarInit, Init);
        Messenger.Unsubscribe(EventId.SpecialOffersInitialized, Init);
    }

    public static void SetSaleSticker()
    {
        if (IsSaleGoing)
        {
            Instance.saleSticker.SetActive(true);
            SetBlackFridaySign();
        }
        else
        {
            Instance.saleSticker.SetActive(false);
        }
    }

    public static void SetBlackFridaySign()
    {
        Instance.sprBlackFriday.gameObject.SetActive(GameData.isBlackFriday);
        if(Instance.lblTimeLimitedSales != null)
            Instance.lblTimeLimitedSales.gameObject.SetActive(!GameData.isBlackFriday);
    }

    public void MeasureScrollArea()
    {
        if (specialOfferFrames.Any(frame => frame.gameObject.activeSelf))
            scrollArea.ContentLength = scrollArea.MeasureContentLength() + 2*framesOffset;
    }

    private void Init(EventId id, EventInfo info)
    {
        StartCoroutine(SetSaleStickerRoutine());
        StartCoroutine(SetOfferFramesPosition());
        InitSaleBtn();
    }

    private void InitSaleBtn()
    {
        if (GameData.isBlackFriday && remainsLayout)
        {
            remainsLayout.SetActive(false);
        }
        else if (lblRemains && HangarController.Instance.allOffersList.Count > 0)
        {
            HangarController.OnTimerTick += UpdateTimer;
        }
    }

    private IEnumerator SetOfferFramesPosition()
    {
        yield return null;

        if(specialOfferFrames == null || specialOfferFrames.Count == 0)
            yield break;

        specialOfferFrames.Shuffle();
        FramesReposition();
    }

    public void FramesReposition()
    {
        if(specialOfferFrames.Count == 0 || !specialOfferFrames.Any(frame => frame.gameObject.activeSelf))
        {
            if (GUIPager.ActivePage == PageName)
            {
                GUIPager.Back();
            }

            return;
        }

        var firstFramePos = new Vector3(frameDimensions.x * 0.5f + framesOffset, framesPosY);

        int i = 0;
        foreach (var offerFrame in specialOfferFrames.Where(frame => frame.gameObject.activeSelf))
        {
            offerFrame.transform.localPosition = Vector3.right * (framesOffset + frameDimensions.x) * i++ + firstFramePos;
        }

        MeasureScrollArea();
    }

    private IEnumerator SetSaleStickerRoutine()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        while (true)
        {
            SetSaleSticker();
            //if(!IsSaleGoing) yield break; // Отключено из-за лимитов. Может быть снова доступно игроку для покупок
            yield return new WaitForSeconds(saleStickerUpdateInterval);
        }
    }

    private void OpenSpecialOffersPage()
    {
        GUIPager.SetActivePage(
            pageName:               "SpecialOffersPage",
            addToHistory:           true,
            showBlackAlphaLayer:    true,
            voiceEventId:           (int)VoiceEventKey.OffersEnter);
    }

    private void UpdateTimer(double sec)
    {
        var remain = HangarController.Instance.allOffersList[0].Remain;
        lblRemains.text = Clock.GetTimerString(remain);
    }

    public static void BankOffersBtnClickHandler(tk2dUIItem uiItem)
    {
        var frame = uiItem.GetComponent<SpecialOfferBtnBuy>().frame;
        var offer = frame.GetComponent<BankOffer>();

        GUIPager.SetActivePage("Bank", true, true);
        Bank.Instance.SetNeededBankLot(offer.Price.currency, offer.XDevsKey);     
    }

    public static void DecalOffersBtnClickHandler(tk2dUIItem uiItem)
    {
        var frame = uiItem.GetComponent<SpecialOfferBtnBuy>().frame;
        var offer = frame.GetComponent<DecalOffer>();

        GUIPager.SetActivePage("DecalShop");
        DecalShop.Selectors[offer.Id].GetComponent<tk2dUIItem>().SimulateClick();
    }

    public static void PatternOffersBtnClickHandler(tk2dUIItem uiItem)
    {
        var frame = uiItem.GetComponent<SpecialOfferBtnBuy>().frame;
        var offer = frame.GetComponent<PatternOffer>();

        GUIPager.SetActivePage("PatternShop");
        PatternShop.Selectors[offer.Id].GetComponent<tk2dUIItem>().SimulateClick();
    }

    public static void VipOffersBtnClickHandler(tk2dUIItem uiItem)
    {
        GUIPager.SetActivePage("VipAccountShop");
    }

    public static void VehicleOffersBtnClickHandler(tk2dUIItem uiItem)
    {
        var frame = uiItem.GetComponent<SpecialOfferBtnBuy>().frame;
        var offer = frame.GetComponent<VehicleOffer>();

        GUIPager.SetActivePage("VehicleShopWindow");
        VehicleShop.Selectors[offer.Id].GetComponent<tk2dUIItem>().SimulateClick();
    }

    public string GetSaleStickerText(int maxDiscount)
    {
        string delimiter = "\n";
        switch (GameData.CurInterface)
        {
            //case Interface.BattleOfHelicopters: delimiter = " "; break;
            case Interface.Armada: delimiter = " "; break;
            //default: delimiter = "\n"; break;
        }
        return string.Format("{0}SALE{1}{2}{3}{4}% ", HelpTools.To2DToolKitColorFormatString(saleWordCustomColor), delimiter, HelpTools.To2DToolKitColorFormatString(percentCustomColor), maxDiscount, HelpTools.To2DToolKitColorFormatString(percentSymbolCustomColor));
    }
}
