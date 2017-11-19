using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XDevs;

public class ConsumableKitItem : MonoBehaviour, IItem
{
    public tk2dSlicedSprite sizeBg;//для определения размера итема

    [SerializeField] private tk2dTextMesh lblDescription;
    [SerializeField] private tk2dBaseSprite sprite;//специфичная для итема текстура

    [SerializeField] private tk2dUIItem btnConsumableKitInfoUIItem;
    [SerializeField] private tk2dUIItem uiItem; // Использовать в дальнейшем для центрирования набора (как карты в MapSelector'е)
    [SerializeField] private SaleSticker saleSticker;
    [SerializeField] private GameObject[] vipObjects;

    public int ConsumableKitId { get; private set; }
    public ConsumableKitInfo Data { get { return GameData.consumableKitInfos[ConsumableKitId]; } }
    [SerializeField] private string localizationFormat = "ConsumableKitName_{0}";
    [SerializeField] private string iconFormat = "consumableKit-{0}";

    [Header("Для раскрашивания по ID")]
    [SerializeField] private tk2dSlicedSprite sprHeader;
    [SerializeField] private tk2dBaseSprite sprButtonUp;
    [SerializeField] private KitBg[] kitsBackgrounds;

    [Serializable]
    private class KitBg
    {
        public string headerSpriteName = null;
        public tk2dBaseSprite.Anchor headerSpriteAnchor = tk2dBaseSprite.Anchor.UpperRight;
        public Vector3 headerSpriteScale = new Vector3(-1, 1, 1);
        public string buttonSpriteName = null;
        public Color32 color = default(Color32);
    }

    private void OnDestroy()
    {
        Messenger.Unsubscribe(EventId.DiscountStateChanged, OnDiscountStateChanged);
        Messenger.Unsubscribe(EventId.OnLanguageChange, OnLanguageChange);

        btnConsumableKitInfoUIItem.OnClick -= OnBtnConsumableKitInfoClickHandler;
        //uiItem.OnClick -= OnItemClickHandler;
    }

    private void OnDiscountStateChanged(EventId id, EventInfo info)
    {
        EventInfo_U evInfo = (EventInfo_U)info;
        EntityTypes entityType = (EntityTypes)evInfo[0];
        int entityId = (int)evInfo[1];
        //bool discountState = (bool)evInfo[2];

        if (entityType == EntityTypes.ConsumableKit && entityId == ConsumableKitId)
            UpdateComponents();
    }

    private void OnLanguageChange(EventId id, EventInfo info)
    {
        UpdateComponents();
    }

    //private void OnItemClickHandler()
    //{
    //    Debug.LogError("OnItemClickHandler();");
    //}

    //private void OnClick(tk2dUIItem btn)
    //{
    //    switch(btn.name)
    //    {
    //        case "btnBuy":
    //            //ConsumablesPage.Instance.BuyConsumable(ConsumableId, () => { btnBuy.Activated = false; }, () => { btnBuy.Activated = true; UpdateComponents(); });
    //            break;
    //    }
    //}

    private void OnBtnConsumableKitInfoClickHandler()
    {
        ConsumableKitInfoPage.kitId = ConsumableKitId;//Передачу параметров надо бы делать черезу GuiPager.
        GUIPager.SetActivePage("ConsumableKitInfoPage", true, true);


        //if (!GameData.consumableInfos[ConsumableId].IsEnabledByVipStatus)
        //{
        //    HangarController.Instance.NavigateToVipShop(showMessageBox: true, negativeCallback: () =>
        //    {
        //        GUIPager.SetActivePage("ConsumablesPage");
        //    });
        //    return;
        //}

        //ConsumablesPage.Instance.BuyConsumable(ConsumableId,
        //    beforeRequest: () => { btnBuy.Activated = false; },
        //    afterResult: () => { btnBuy.Activated = true; UpdateComponents(); });
    }

    public void Initialize(object[] parameters)
    {
        ConsumableKitId = (int)parameters[0];

        Messenger.Subscribe(EventId.DiscountStateChanged, OnDiscountStateChanged);
        Messenger.Subscribe(EventId.OnLanguageChange, OnLanguageChange);

        btnConsumableKitInfoUIItem.OnClick += OnBtnConsumableKitInfoClickHandler;
        //uiItem.OnClick += OnItemClickHandler;

        UpdateComponents();
    }

    private void UpdateComponents()
    {
        //lblDescription.text = string.Format("id = {0}, sprite = {1}", ConsumableKitId, GameData.consumableInfos[ConsumableKitId].icon);
        //setPriceScript.Price = GameData.consumableInfos[ConsumableKitId].price.ToPrice();
        //HelpTools.SetSpriteToAllSpritesInCollection(sprite, GameData.consumableInfos[ConsumableKitId].icon);

        var selectedBg = (ConsumableKitId - 1) % kitsBackgrounds.Length;

        if (sprButtonUp != null)
        {
            sprButtonUp.color = kitsBackgrounds[selectedBg].color;

            if (!string.IsNullOrEmpty(kitsBackgrounds[selectedBg].buttonSpriteName))
                sprButtonUp.SetSprite(kitsBackgrounds[selectedBg].buttonSpriteName);
        }

        if (sprHeader != null)
        {
            sprHeader.color = kitsBackgrounds[selectedBg].color;

            if (!string.IsNullOrEmpty(kitsBackgrounds[selectedBg].headerSpriteName))
                sprHeader.SetSprite(kitsBackgrounds[selectedBg].headerSpriteName);

            sprHeader.anchor = kitsBackgrounds[selectedBg].headerSpriteAnchor;
            sprHeader.scale = kitsBackgrounds[selectedBg].headerSpriteScale;
        }

        if (!string.IsNullOrEmpty(Data.localizationKey))
        {
            lblDescription.text = Localizer.GetText(Data.localizationKey);
        }
        else
        {
            Debug.LogErrorFormat("Missing 'localizationKey' data field in consumable kit {0}, generating key from ID.", ConsumableKitId);
            var localizationKey = string.Format(localizationFormat, ConsumableKitId);
            lblDescription.text = Localizer.GetText(localizationKey);
        }

        if (!string.IsNullOrEmpty(Data.icon))
        {
            sprite.SetSprite(Data.icon);
        }
        else
        {
            Debug.LogErrorFormat("Missing 'icon' data field in consumable kit {0}, generating from ID.", ConsumableKitId);
            var icon = string.Format(iconFormat, ConsumableKitId);

            sprite.SetSprite(icon);
        }

        if (saleSticker != null)
        {
            saleSticker.sprSaleSticker.SetActive(Data.discount != null && Data.discount.IsActive);

            if (saleSticker.sprSaleSticker.activeSelf)
                saleSticker.SetTextWithFormatString(Data.discount.val);
        }

        MiscTools.SetObjectsActivity(Data.isVip, vipObjects);
    }

    public void DestroySelf() {}

    public Vector2 GetSize()
    {
        return new Vector2(sizeBg.dimensions.x * sizeBg.scale.x, sizeBg.dimensions.y * sizeBg.scale.y);
    }

    public string GetUniqId { get { return ConsumableKitId.ToString(); } }

    public tk2dUIItem MainUIItem { get { return btnConsumableKitInfoUIItem; } }

    public Transform MainTransform { get { return transform; } }
}
