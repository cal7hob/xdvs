using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XDevs;

public class ConsumableKitItem : MonoBehaviour, IItem
{
    [SerializeField] private tk2dSlicedSprite sizeBg;//для определения размера итема
    [SerializeField] private tk2dTextMesh lblDescription;
    [SerializeField] private tk2dBaseSprite sprite;//специфичная для итема текстура
    [SerializeField] private SaleSticker saleSticker;
    [SerializeField] private GameObject[] vipObjects;

    public int ConsumableKitId { get; private set; }
    public ConsumableKitInfo Data { get { return GameData.consumableKitInfos[ConsumableKitId]; } }

    private void Awake()
    {
        Dispatcher.Subscribe(EventId.DiscountStateChanged, OnDiscountStateChanged);
        Dispatcher.Subscribe(EventId.OnLanguageChange, OnLanguageChange);
        UpdateElements();
    }

    private void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.DiscountStateChanged, OnDiscountStateChanged);
        Dispatcher.Unsubscribe(EventId.OnLanguageChange, OnLanguageChange);
    }

    private void OnClick(tk2dUIItem btn)
    {
        switch(btn.name)
        {
            case "btnConsumableKitInfo":
                ConsumableKitInfoPage.kitId = ConsumableKitId;//Передачу параметров надо бы делать черезу GuiPager.
                GUIPager.SetActivePage("ConsumableKitInfoPage");
                break;
        }
    }

    public void Initialize(object[] parameters)
    {
        ConsumableKitId = (int)parameters[0];

        UpdateElements();
    }

    public void UpdateElements()
    {
        lblDescription.text = Localizer.GetText(Data.localizationKey);
        if(!string.IsNullOrEmpty(Data.icon))
            sprite.SetSprite(Data.icon);
        saleSticker.sprSaleSticker.SetActive(Data.discount != null && Data.discount.IsActive);
        if (saleSticker.sprSaleSticker.activeSelf)
            saleSticker.SetTextWithFormatString(Data.discount.val);
        MiscTools.SetObjectsActivity(Data.isVip, vipObjects);
    }

    public void DesrtoySelf()
    {
    }

    private void OnDiscountStateChanged(EventId id, EventInfo info)
    {
        EventInfo_U evInfo = (EventInfo_U)info;
        EntityTypes entityType = (EntityTypes)evInfo[0];
        int entityId = (int)evInfo[1];
        //bool discountState = (bool)evInfo[2];

        if(entityType == EntityTypes.consumableKit && entityId == ConsumableKitId)
            UpdateElements();
    }

    private void OnLanguageChange(EventId id, EventInfo info)
    {
        UpdateElements();
    }

    public Vector2 GetSize()
    {
        return new Vector2(sizeBg.dimensions.x * sizeBg.scale.x, sizeBg.dimensions.y * sizeBg.scale.y);
    }

    public string GetUniqId { get { return ConsumableKitId.ToString(); } }

    public tk2dUIItem MainUIItem { get { return null; } }

    public Transform MainTransform { get { return transform; } }
}
