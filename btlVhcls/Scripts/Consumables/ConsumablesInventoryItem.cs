using UnityEngine;

public class ConsumablesInventoryItem : MonoBehaviour, IInventoryItem
{
    [SerializeField] private tk2dSlicedSprite sizeBg;//для определения размера итема
    [SerializeField] private GameObject countWrapper;
    [SerializeField] private tk2dTextMesh lblCount;
    [SerializeField] private tk2dBaseSprite sprite;//специфичная для итема текстура
    [SerializeField] private string emptyCellSpriteName;
    [SerializeField] private tk2dUIItem uiitem;

    private int slotIndex;
    private int contentId = -1;
    private InventoryBase parentInventoryPanel = null;

    public int ContentId
    {
        get { return contentId; }
        set
        {
            contentId = value;
            UpdateElements();
        }
    }

    public bool IsEmpty { get { return contentId < 0; } }
    public string GetUniqId { get { return slotIndex.ToString(); } }
    public tk2dUIItem MainUIItem { get { return uiitem; } }

    private void Awake()
    {
        //Dispatcher.Subscribe(EventId.ConsumableInventoryStateChanged, OnConsumableInventoryStateChanged);
        Dispatcher.Subscribe(EventId.ProfileInfoLoadedFromServer, OnProfileInfoLoadedFromServer);
    }

    private void OnDestroy()
    {
        //Dispatcher.Unsubscribe(EventId.ConsumableInventoryStateChanged, OnConsumableInventoryStateChanged);
        Dispatcher.Unsubscribe(EventId.ProfileInfoLoadedFromServer, OnProfileInfoLoadedFromServer);
    }

    public void Initialize(object[] parameters)
    {
        slotIndex = (int)((object[])(parameters[0]))[0];
        ContentId = (int)((object[])(parameters[0]))[1];
        ParamDict additionalParams = (ParamDict)parameters[1];
        parentInventoryPanel = (InventoryBase)additionalParams.GetValue("inventoryPanel");

        gameObject.name = gameObject.name.Replace("(Clone)", "");
        gameObject.name += "_" + slotIndex;
    }

    //Устанавливается сейчас напрямую через саму панельку. Так что пока не надо.
    //private void OnConsumableInventoryStateChanged(EventId evId, EventInfo ev)
    //{
    //    EventInfo_U eventInfo = (EventInfo_U)ev;
    //    int consId = (int)eventInfo[0];
    //    //bool state = (bool)eventInfo[1];
    //    int slot = (int)eventInfo[2];

    //    if (slot != slotIndex)
    //        return;

    //    UpdateElements();
    //}

    public void UpdateElements()
    {
        sprite.SetSprite(IsEmpty ? emptyCellSpriteName : (GameData.consumableInfos[ContentId].icon + GameData.CONSUMABLES_SPRITE_FRAMED_VERSION_SUFFIX));
        if(countWrapper)
            countWrapper.SetActive(!IsEmpty);
        if(lblCount)
            lblCount.text = !IsEmpty ?
                Mathf.Clamp(ProfileInfo.consumableInventory[ContentId].count, 0, GameData.consumableInfos[ContentId].maxInBattle).ToString() :
                "";
    }

    private void OnProfileInfoLoadedFromServer(EventId evId, EventInfo ev)
    {
        UpdateElements();
    }

    private void OnItemClick(tk2dUIItem btn)
    {
        if(!IsEmpty)
            Dispatcher.Send(EventId.ChangeConsumableInventoryState, new EventInfo_U(ContentId, false, slotIndex));
        ConsumablesPage.Instance.SetTab(parentInventoryPanel is SuperWeaponsInventoryPanel ? ConsumablesPage.Tab.SuperWeapons : ConsumablesPage.Tab.Consumables);
    }

    public void DesrtoySelf()
    {

    }

    public Vector2 GetSize()
    {
        return new Vector2(sizeBg.dimensions.x * sizeBg.scale.x, sizeBg.dimensions.y * sizeBg.scale.y);
    }

    public Transform MainTransform { get { return transform; } }
}
