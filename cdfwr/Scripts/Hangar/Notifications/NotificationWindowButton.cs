using System;
using Http;
using UnityEngine;
using XDevs.Notifications.Models;
using Action = System.Action;
using Type = XDevs.Notifications.Models.Type;

public class NotificationWindowButton : MonoBehaviour
{
    [SerializeField] private Button button;
    public Button Button { get { return button; } }

    [SerializeField] private ActionButton actionButton;
    [SerializeField] private bool subscribed;

    [SerializeField] private int notificationId;

    private Action clickedAction;

    public void Setup(ActionButton actionButton, int notificationId, Action clickedAction)
    {
        if (NotificationsManager.Dbg)
            Debug.LogError(gameObject.name + ": Setup()");

        this.actionButton = actionButton;
        this.notificationId = notificationId;

        GetComponent<tk2dUIItem>().OnClick += ClickedHandler;
        subscribed = true;

        this.clickedAction = clickedAction;
    }

    private void OnDisable()
    {
        if (subscribed)
        {
            if (NotificationsManager.Dbg)
                Debug.LogError(gameObject.name + ": OnDisable() unsubscribing");

            GetComponent<tk2dUIItem>().OnClick -= ClickedHandler;
            subscribed = false;
        }
    }

    private void ClickedHandler()
    {
        if (NotificationsManager.Dbg)
            Debug.LogError(gameObject.name + ": ClickedHandler()");

        switch (actionButton.Button)
        {
            case Button.OK:
                CallbackOnPress(notificationId, Button.OK);
                break;

            case Button.Close:
                CallbackOnPress(notificationId, Button.Close);
                break;

            case Button.Acquire:
                CallbackOnPress(notificationId, Button.Acquire);
                break;

            case Button.Action:
                switch (actionButton.Action.Type)
                {
                    case Type.URL:
                        if (!string.IsNullOrEmpty(actionButton.Action.Value))
                            Application.OpenURL(actionButton.Action.Value);

                        CallbackOnPress(notificationId, Button.Action);
                        break;

                    case Type.Page:
                        switch (actionButton.Action.Location)
                        {
                            case Page.Bank:
                                if (actionButton.Action.BankTabAction != null)
                                {
                                    switch (actionButton.Action.BankTabAction.Tab)
                                    {
                                        case Bank.Tab.Gold:
                                        case Bank.Tab.Silver:
                                            GoToBankAction(actionButton.Action.BankTabAction);
                                            break;

                                        case Bank.Tab.Vip:
                                            GoToVipShopAction(actionButton.Action.BankTabAction.Value);
                                            break;
                                    }
                                }
                                break;

                            case Page.Shop:
                                if (actionButton.Action.ShopTabAction != null)
                                {
                                    switch (actionButton.Action.ShopTabAction.Tab)
                                    {
                                        case ShopTab.Tank:
                                            GoToVehicleShopAction(actionButton.Action.ShopTabAction.Value);
                                            break;
                                        case ShopTab.Camouflage:
                                            GoToPatternShopAction(actionButton.Action.ShopTabAction.Value);
                                            break;
                                        case ShopTab.Decal:
                                            GoToDecalShopAction(actionButton.Action.ShopTabAction.Value);
                                            break;
                                    }
                                }
                                break;
                        }

                        CallbackOnPress(notificationId, Button.Action);
                        break;
                }
                break;
        }

        clickedAction.SafeInvoke();
    }

    #region Deeperlinks-grade shit

    private void GoToBankAction(BankTabAction bankTabAction)
    {
        HangarController.Instance.GoToBank(bankTabAction.Tab, false);

        ProfileInfo.PriceCurrency currency;
        HelpTools.TryParseToEnum(bankTabAction.Tab.ToString(), out currency, true);

        Bank.Instance.SetNeededBankLot(currency, bankTabAction.Value);
    }

    private void GoToVipShopAction(string iapId)
    {
        HangarController.Instance.NavigateToVipShop();
        VipShopPage.Instance.GoToNeededOffer(iapId);
    }

    private void GoToVehicleShopAction(string vehicleId)
    {
        GUIPager.SetActivePage(ShopManager.Instance.vehicleShop.GuiPageName);
        VehicleShop.Instance.SelectVehicle(Convert.ToInt32(vehicleId));
    }

    private void GoToPatternShopAction(string tankId)
    {
        GUIPager.SetActivePage(ShopManager.Instance.patternShop.GuiPageName);
        PatternShop.Instance.SelectBodyKit(Convert.ToInt32(tankId));
    }

    private void GoToDecalShopAction(string tankId)
    {
        GUIPager.SetActivePage(ShopManager.Instance.decalShop.GuiPageName);
        DecalShop.Instance.SelectBodyKit(Convert.ToInt32(tankId));
    }

#endregion

    private void CallbackOnPress(int notificationId, Button pressedButton)
    {
        var request = Manager.Instance().CreateRequest("/player/notification/callback");
        request.Form.AddField("id", notificationId);
        request.Form.AddField("button", pressedButton.ToString());

        Manager.StartAsyncRequest(
            request: request,
            failCallback: result =>
            {
                Debug.LogError("Notification acknowledged erroneously");
            },
            successCallback: result =>
            {
                if (NotificationsManager.Dbg)
                    Debug.LogError("Notification acknowledged successfully");
            });
    }
}
