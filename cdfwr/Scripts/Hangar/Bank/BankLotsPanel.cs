using UnityEngine;

public class BankLotsPanel : ItemsPanel
{
    [SerializeField] private GameObject tapjoyPrefab;
    [SerializeField] private GameObject freeVehicleLotsPrefab;
    [SerializeField] private GameObject purchasebleLotsPrefab;

    public BankLotBase TapjoyLot { get; private set; }
    public BankLotBase FreeVehicleLot { get; private set; }
    public BankLot PurchasebleLot { get; private set; }

    public void CreateTapjoyLot() 
    {
        TapjoyLot = CreateLotByGivenPrefab(tapjoyPrefab) as BankLotBase;

        if (TapjoyLot != null)
        {
            TapjoyLot.SetBtnAction(ShowTapJoyOfferWall);
            TapjoyLot.name = "TapjoyOffer";
            ScrollableItemsBehaviour.SetActiveBankLot(TapjoyLot.gameObject, false);
        }
    }

    public void CreateFreeVehicleLot()
    {
        FreeVehicleLot = CreateLotByGivenPrefab(freeVehicleLotsPrefab) as BankLotBase;

        if (FreeVehicleLot != null)
        {
            FreeVehicleLot.SetBtnAction(VehicleOffersController.Instance.ShowFreeVehicleDetails);
            FreeVehicleLot.name = "FreeVehicleOffer";
        }
    }

    public BankLot CreatePurchasebleLot()
    {
        PurchasebleLot = CreateLotByGivenPrefab(purchasebleLotsPrefab) as BankLot;

        return PurchasebleLot;
    }

    public void ShowTapjoyFrame()
    {
        ScrollableItemsBehaviour.SetActiveBankLot(TapjoyLot.gameObject, true);
    }

    public static void ShowTapJoyOfferWall(tk2dUIItem tk2dUiItem)
    {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        TapjoyHangar.BankPlacement.ShowContent();
#endif
    }
}
