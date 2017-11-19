using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using XDevs;

public class ConsumableKitInfoPage : HangarPage
{
    public static ConsumableKitInfoPage Instance { get; private set; }
    public static int kitId = 0;

    [SerializeField] private Factory factory;
    [SerializeField] private tk2dTextMesh lblTitle;
    [SerializeField] private DiscountRenderer discountRenderer;
    [SerializeField] private GameObject[] vipObjects;

    [SerializeField] private ActivatedUpDownButton btnBuy;
    [SerializeField] private tk2dUIItem btnBackUIItem;

    private ConsumableKitInfo Data { get { return GameData.consumableKitInfos.ContainsKey(kitId) ? GameData.consumableKitInfos[kitId] : null; } }
    private bool isWaitingServerAnswer = false;

    protected override void Show()
    {
        base.Show();

        Messenger.Subscribe(EventId.DiscountStateChanged, OnDiscountStateChanged);
        Messenger.Subscribe(EventId.OnLanguageChange, OnLanguageChange);

        btnBackUIItem.OnClick += OnBtnBackClick;
        btnBuy.GetComponent<tk2dUIItem>().OnClick += OnBtnBuyClick;

        factory.DestroyAll();

        List<Entity> list = new List<Entity>();
        list.AddRange(Data.items);
        #region TestData
        //list.AddRange(Data.items);
        //list.AddRange(Data.items);
        //list.AddRange(Data.items);
        //list.Add(Data.items[0]);
        //list.Add(Data.items[1]);
        #endregion

        for (int i = 0; i < list.Count; i++)
            factory.Create(list[i], i);

        UpdateElements();
    }

    protected override void Hide()
    {
        base.Hide();

        Messenger.Unsubscribe(EventId.DiscountStateChanged, OnDiscountStateChanged);
        Messenger.Unsubscribe(EventId.OnLanguageChange, OnLanguageChange);

        btnBackUIItem.OnClick -= OnBtnBackClick;
        btnBuy.GetComponent<tk2dUIItem>().OnClick -= OnBtnBuyClick;
    }

    private void OnLanguageChange(EventId id, EventInfo info)
    {
        UpdateElements();
    }

    private void OnBtnBackClick()
    {
        GUIPager.Back();
    }

    private void OnBtnBuyClick()
    {
        if (Data.isVip && !ProfileInfo.IsPlayerVip)
        {
            HangarController.Instance.NavigateToVipShop(showMessageBox: true);

            return;
        }

        if (isWaitingServerAnswer)
            return;

        if (ProfileInfo.CanBuy(Data.CurPrice))
        {
            isWaitingServerAnswer = true;
            btnBuy.Activated = false;
            var request = Http.Manager.Instance().CreateRequest("/shop/buyKit");
            request.Form.AddField("kitId", kitId);

            Http.Manager.StartAsyncRequest(
                request: request,
                successCallback: delegate (Http.Response result)
                {
                    if (GameData.IsHangarScene)//На случай если ответ получим уже в бою
                    {
                        isWaitingServerAnswer = false;
                        btnBuy.Activated = true;
                        GUIPager.Back();
                        HangarController.Instance.PlayGUISound(HangarController.Instance.buyingSound);
                    }

                    //Dispatcher.Send(EventId.ConsumableBought, new EventInfo_I(id));
                },
                failCallback: delegate (Http.Response result)
                {
                    if (GameData.IsHangarScene)//На случай если ответ получим уже в бою
                    {
                        isWaitingServerAnswer = false;
                        btnBuy.Activated = true;
                    }
                });
        }
        else
            HangarController.Instance.GoToBank(Data.price.currency);
    }

    private void OnDiscountStateChanged(EventId id, EventInfo info)
    {
        EventInfo_U evInfo = (EventInfo_U)info;
        EntityTypes entityType = (EntityTypes)evInfo[0];
        int entityId = (int)evInfo[1];
        bool discountState = (bool)evInfo[2];

        if (entityType == EntityTypes.ConsumableKit && entityId == kitId)
            UpdateElements();
    }

    public void UpdateElements()
    {
        if (Data == null || !IsVisible)
            return;

        lblTitle.text = Localizer.GetText(Data.localizationKey);
        discountRenderer.SetupPrices(Data.CurPrice, Data.price, Data.discount != null && Data.discount.IsActive);
        MiscTools.SetObjectsActivity(Data.isVip, vipObjects);
    }
}
