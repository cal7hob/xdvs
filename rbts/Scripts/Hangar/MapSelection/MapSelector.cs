using UnityEngine;
using System;

public class MapSelector : MonoBehaviour
{
    public tk2dTextMesh[] mapNameLabels;   
    public GameObject sprIsOn;
    public GameObject locker;
    public ActivatedUpDownButton disablerScript;
    public GameObject mapLevelWrapper;
    public GameObject fuelRequiredWrapper;
	public tk2dBaseSprite[] mapPreviewSprites;
    ///В некоторых проектах (IT,FT,TW) атлас SplashScreens содержит непрозрачные текстуры
    ///поэтому для экономии ОЗУ текстуру рандомной карты (она с прозрачностью) перенесли в атлас ангара, чтобы не включать прозрачность для немаленького атласа SplashScreens
    ///По умолчанию в префабе указан атлас для рандомной карты, а при установке превьюхи устанавливается атлас, указанный в переменной sprCollection
    public tk2dSpriteCollectionData sprCollection;
    public tk2dTextMesh lblMapAvailabilityLevel;
    [Header("dont use localization key lblMapAvailabilityLevel")]
    public bool setTo_lblMapAvailabilityLevel_OnlyMapLevel = false;//dont use localizationKey Localizer.GetText("lblMapAvailabilityLevel", MapLevel)
    public tk2dTextMesh lblFuelRequired;
	[SerializeField] private GameObject playersCountWrapper;
	[SerializeField] private tk2dTextMesh lblPlayersCount;
    [SerializeField] private WaitingIndicatorBase waitingIndicator;

    public tk2dUIItem UiItem { get; set; }
    public tk2dUIToggleButton UiToggleButton { get; set; }
    public GameManager.MapId MapId { get; set; }
    public int MapLevel { get; set; }
    public int FuelRequired { get; set; }
    public bool IsMapEnabledOnTheServer { get; set; }
    public bool IsPlayerLevelEnoughToPlayThisMap { get { return ProfileInfo.Level >= MapLevel; } }
    public bool IsMapAvailableForPlay { get { return IsPlayerLevelEnoughToPlayThisMap && IsMapEnabledOnTheServer; } }

    void Awake()
    {
        Messenger.Subscribe(EventId.OnLanguageChange, SetLocker);
    	playersCountWrapper.SetActive (false);
    }

    void OnDestroy()
    {
        Messenger.Unsubscribe(EventId.OnLanguageChange, SetLocker);
    }

	public void Init ( Vector3 pos, GameManager.MapId mapId,int mapLevel, int fuelRequired, bool isMapEnabled )
	{     
        UiToggleButton = GetComponent<tk2dUIToggleButton>();
        UiItem = GetComponent<tk2dUIItem>();
        UiItem.OnClickUIItem -= OnClick;//Important
        transform.SetParent(MapFramesCreator.Instance.mapsContainer.transform, false);
        transform.localPosition = new Vector3(pos.x, pos.y, transform.localPosition.z);//Dont change Z !!! Blya
        gameObject.name = mapId.ToString();
        MapId = mapId;
        MapLevel = mapLevel;
        FuelRequired = fuelRequired;
        IsMapEnabledOnTheServer = isMapEnabled;
        if (mapNameLabels != null)
            for(int i = 0; i < mapNameLabels.Length; i++)
                if(mapNameLabels[i] != null)
                {
                    mapNameLabels[i].name = mapId.ToString();
                    LabelLocalizationAgent lAgent = mapNameLabels[i].gameObject.GetComponent<LabelLocalizationAgent>();
                    if(lAgent == null)
                        mapNameLabels[i].gameObject.AddComponent<LabelLocalizationAgent>();
                }
        
        if (name != "random_map")
		{
            if(mapPreviewSprites != null)
                for(int i = 0; i < mapPreviewSprites.Length; i++)
                    if(mapPreviewSprites[i] != null)
                        mapPreviewSprites[i].SetSprite(sprCollection, "Map_Selection_" + MapId);
        }

		SetLocker();
	}

    public void SetLocker(EventId id = 0, EventInfo info = null)
    {
        if (!IsMapAvailableForPlay)
        {
            if(locker)
                locker.SetActive(true);
            if (disablerScript)
                disablerScript.Activated = false;
            mapLevelWrapper.SetActive(true);
            fuelRequiredWrapper.SetActive(false);

            //*********Move this item behind the scrollbar's collider************
            BoxCollider scrollableAreaBoxCollider = MapFramesCreator.Instance.scrollableArea.GetComponent<BoxCollider>();
            BoxCollider mapItemBoxCollider = transform.GetComponent<BoxCollider>();
            //Для упрощения рассчетов обнуляем размер коллайдеров
            scrollableAreaBoxCollider.size = new Vector3(scrollableAreaBoxCollider.size.x, scrollableAreaBoxCollider.size.y, 0);
            mapItemBoxCollider.size = new Vector3(mapItemBoxCollider.size.x, mapItemBoxCollider.size.y, 0);
            float newColliderZ = MapFramesCreator.Instance.scrollableArea.transform.position.z +
                scrollableAreaBoxCollider.center.z -
                transform.position.z +
                5;//Сдвигаем коллайдер итема на 5 единиц по Z за коллайдер прокрутки 
            mapItemBoxCollider.center = new Vector3(mapItemBoxCollider.center.x, mapItemBoxCollider.center.y, newColliderZ);
            //*******************************************************************

            lblMapAvailabilityLevel.text = IsMapEnabledOnTheServer ? 
                (setTo_lblMapAvailabilityLevel_OnlyMapLevel ?
                    MapLevel.ToString() : 
                    Localizer.GetText("lblMapAvailabilityLevel", MapLevel)) 
                : Localizer.GetText("lblMapSoon");
        }
        else
        {
            if (disablerScript)
                disablerScript.Activated = true;
            if (locker)
                locker.SetActive(false);
            UiItem.OnClickUIItem += OnClick;
            mapLevelWrapper.SetActive(false);
            fuelRequiredWrapper.SetActive(true);
            lblFuelRequired.text = FuelRequired.ToString();
        }
    }

    public void SetPlayersCount(int count, bool showWaitingIndicator)
    {
        if (!IsMapAvailableForPlay || name == "random_map")
        {
            playersCountWrapper.SetActive(false);
            return;
        }
	    playersCountWrapper.SetActive (true);
		lblPlayersCount.text = showWaitingIndicator ? "" : count.ToString();
        if(waitingIndicator)
            waitingIndicator.SetActive(showWaitingIndicator);
    }

    private void OnClick(tk2dUIItem btn)
    {
        //DT3.LogError("clicked {0}", btn.name);
        MapSelectionScrollbar.Instance.SelectedItemToCenter(btn);
    }
}
