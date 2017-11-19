using System;
using System.Collections;
using Tanks.Models;
using UnityEngine;

public class TankIndicator : MonoBehaviour
{
	public GameObject awardGroup;
	public ProgressBar progressBar;
    public tk2dSlicedSprite background;
    public VehicleController Vehicle;
    public tk2dBaseSprite sprFlag;
    public Avatar avatar;
    public tk2dSlicedSprite sprKillerAim;
    public GameObject wrapper;
    public Transform wrapperTankIndicator;
    public Transform wrapperPopupDamage;
    public GameObject chatMessageWrapper;
    public GameObject usedConsumableWrapper;//Отображение использования расходки
    public bool useConsumableSpritesWithFrame = true;
    [Header("Форматируем по простому...")]
    public Color friendProgressbarBgColor;
	public Color friendProgressbarFillerColor;
	public Color enemyProgressbarBgColor;
	public Color enemyProgressbarFillerColor;
    [Header("... или используем ConditionHelper")]
    public InterfaceExtensions.ConditionHelper conditionHelper;//Настройка префаба в зависимости от того это индикатор врага или друга
    public tk2dTextMesh lblPopupDamage;
    public tk2dTextMesh playerName;
    public tk2dTextMesh lblAward;
    public tk2dTextMesh clanName;
    public GameObject clanNameWrapper;
    public tk2dTextMesh chatMessage;
    public tk2dBaseSprite usedConsumableSprite;
    public float minScaleRatio = 0.5f;
    public float pixelsIndicatorUpToPer1000M = 5.0f;

    private GameObject ClanNameWrapper { get { return clanNameWrapper ? clanNameWrapper : clanName.gameObject; } }//Убрать свойство когда сделаю для всех проектов

    private BattleChatPanelItemData chatMessageData;
    private UsedConsumableData usedConsumableData;
    private Vector3 playerNameDefPos;

    /// <summary>
    /// Время анимации.
    /// </summary>
    public float popupDamageAnimationTime = 1f;

    /// <summary>
    /// На сколько пикселей вверх будет уходить лейбл урона.
    /// </summary>
    public float popupDamageAnimationDistance = 75f;

    /// <summary>
    /// Скейл шрифта в точке экстремума.
    /// </summary>
    public float popupDamageAnimationMaxScale = 1.5f;

    /// <summary>
    /// Максимальный скейл шрифта при максимальном дамаге.
    /// </summary>
    public float popupDamageAnimationMaxScaleDamageAffected = 10.0f;

    /// <summary>
    /// Захадкоженный максимальный возможный урон в BOH.
    /// </summary>
    public float maxPossibleDamage = 340.0f;

    /// <summary>
    /// Момент начала растворения (0..1).
    /// </summary>
    public float dissolutionMoment = 0.5f;

    /// <summary>
    /// Момент экстремума (0..1). 0.5 означает что длительность фазы роста скейла равна длительности спада.
    /// </summary>
    public float popupDamageAnimationScalingExtremum = 0.4f;

    private Camera camera2d;
    private Vector3 indicatorLocalScale;
    private float scaleRatio;
	private int award;
    private bool hidden;
    private int popupDamageValue;
	private bool usePopupDamageAnimation = false;
	private Vector3 lblPopupDamageStartPos;
	private float lblPopupDamageStartScale;
    private Transform popupDamageParentBuffer;
    private IEnumerator popupDamageLabelAnimationRoutine;

    public int LastKillerId { get; private set; }
    //public int LastVictimId { get; private set; }

    void Awake()
    {
		Dispatcher.Subscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        //Dispatcher.Subscribe(EventId.OnLanguageChange, OnLanguageChange);//теперь нельзя менять язык в бою
        Dispatcher.Subscribe(EventId.FlagSettingsChanged, OnFlagSettingsChanged);
        //Dispatcher.Subscribe(EventId.AvatarSettingsChanged, ApplyAvatarAndFlagOptions);

        playerNameDefPos = playerName.transform.localPosition;
        if (chatMessageWrapper)
            chatMessageWrapper.SetActive(false);
        if (usedConsumableWrapper)
            usedConsumableWrapper.SetActive(false);

        lblPopupDamageStartPos = lblPopupDamage.transform.localPosition;
		lblPopupDamageStartScale = lblPopupDamage.scale.x;
		lblPopupDamage.gameObject.SetActive(false); // Прячем лейбл.
	    popupDamageValue = 0;
		// Ограничения на параметры.
		if (popupDamageAnimationTime > 0f &&
			popupDamageAnimationDistance > 0f &&
			popupDamageAnimationMaxScale > 0f &&
			lblPopupDamageStartScale > 0f &&
			popupDamageAnimationMaxScale >= lblPopupDamageStartScale &&
			popupDamageAnimationScalingExtremum > 0 && popupDamageAnimationScalingExtremum < 1f)
		{
			usePopupDamageAnimation = true;
		}
		else
			DT.LogError("Wrong Parameters for PopupDamage!");

	    camera2d = BattleGUI.Instance.GuiCamera;
        sprKillerAim.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
		Dispatcher.Unsubscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        //Dispatcher.Unsubscribe(EventId.OnLanguageChange, OnLanguageChange);
        Dispatcher.Unsubscribe(EventId.FlagSettingsChanged, OnFlagSettingsChanged);
        //Dispatcher.Unsubscribe(EventId.AvatarSettingsChanged, ApplyAvatarAndFlagOptions);
    }

    //private void OnLanguageChange(EventId evId, EventInfo ev)
    //{
    //    UpdateElements();
    //}

    void OnDisable()
    {
        lblPopupDamage.gameObject.SetActive(false);
        popupDamageValue = 0;
    }


    void Start()
    {
		playerName.maxChars = Settings.MAX_NAME_LENGTH;

        awardGroup.SetActive(false);

        ClanNameWrapper.SetActive(!string.IsNullOrEmpty(Vehicle.data.clanName));
        if (!string.IsNullOrEmpty(Vehicle.data.clanName))
            clanName.text = Vehicle.data.clanName;
	
        UpdateElements();
    }

    void LateUpdate()
    {
        if (hidden || !BattleController.MyVehicle)
            return;

        if (Camera.main.WorldToScreenPoint(Vehicle.transform.position).z > 0)
        {
            Vector3 vehicleIndicatorPointProjection = Camera.main.WorldToViewportPoint(Vehicle.IndicatorPointPosition);

            scaleRatio = Mathf.Clamp (vehicleIndicatorPointProjection.z / -180 + 1, minScaleRatio, 1);
            indicatorLocalScale.x = indicatorLocalScale.y = indicatorLocalScale.z = scaleRatio;
            wrapper.transform.localScale = indicatorLocalScale;

            var distance = Vector3.Distance(BattleController.MyVehicle.transform.position, Vehicle.transform.position);
            var indicatorPosition = camera2d.ViewportToWorldPoint(vehicleIndicatorPointProjection);
            wrapperTankIndicator.position = indicatorPosition + distance*Vector3.up*pixelsIndicatorUpToPer1000M*0.001f;

            #region Если нужен всплывающий урон в точке удара - закомментировать

            Vector3 popupDamageProjection = Camera.main.WorldToViewportPoint(Vehicle.ShotPoint.position);
            wrapperPopupDamage.position = camera2d.ViewportToWorldPoint(popupDamageProjection);

            #endregion
        }
        else
        {
            wrapperTankIndicator.position = -transform.forward * 10000;
        }

        #region Выключение итема чата
        if (chatMessageWrapper && chatMessageWrapper.activeSelf && chatMessageData != null && !chatMessageData.IsLive)
            chatMessageWrapper.SetActive(false);
        #endregion

        #region Выключение значка расходки
        if (usedConsumableWrapper && usedConsumableWrapper.activeSelf && usedConsumableData != null && !usedConsumableData.IsLive)
            usedConsumableWrapper.SetActive(false);
        #endregion

    }

    public bool Hidden
    {
        get { return hidden; }

        set
        {
            hidden = value;

            wrapper.SetActive(!hidden);

            if (hidden)
            {
                if (popupDamageLabelAnimationRoutine != null)
                    StopCoroutine(popupDamageLabelAnimationRoutine);

                lblPopupDamage.gameObject.SetActive(false);

                popupDamageValue = 0;
            }
        }
    }

	public int Award
	{
		get { return award; }
		set
		{
		    award = value > 0 ? value : 0;
		    RefreshAward();
		}
	}

    public void AnimateLblPopupDamage(int damage, Vector3 hitPoint)
    {
        if (!usePopupDamageAnimation || Hidden || !gameObject.activeInHierarchy)
            return;

        popupDamageValue = damage;
        lblPopupDamage.text = popupDamageValue.ToString();

        if (!lblPopupDamage.gameObject.activeSelf)
        {
            popupDamageLabelAnimationRoutine = AnimateLblPopupDamageCoroutine(damage, hitPoint);
            StartCoroutine(popupDamageLabelAnimationRoutine);
        }
    }

    public void RedrawHealthBar(int newValue)
    {
        progressBar.Percentage = (float)(newValue) / Vehicle.data.maxArmor;
    }

    public void GetAvatar()
    {
        if (Vehicle.data.socialPlatform != SocialPlatform.Undefined
                    && Vehicle.data.socialUID != null)
        {
            avatar.Init(new Player(Vehicle.data.socialPlatform, Vehicle.data.socialUID));
            avatar.DownloadAvatar(0, delegate(Texture2D texture)
            {
                //Debug.LogWarning("TankIndicator avatar callback");
                //Vehicle.data.avatar = texture;
            });
        }
    }

    private void OnMainTankAppeared(EventId id, EventInfo info)
	{
        UpdateElements();
	}

	private void RefreshAward()
	{
		if (award > 0)
		{
			awardGroup.SetActive(true);
			lblAward.text = award.ToString();
		}
		else
			awardGroup.SetActive(false);
	}

	private IEnumerator AnimateLblPopupDamageCoroutine(float damage, Vector3 hitPoint)
	{
        lblPopupDamage.gameObject.SetActive(true);

        float elapsedTime = 0;
		float progress = 0;
		float scale = lblPopupDamageStartScale;

		bool isScalingDirectionUp = true;

        lblPopupDamage.transform.localPosition = lblPopupDamageStartPos;

        while (progress < 1 && lblPopupDamage.gameObject.activeInHierarchy)
		{
            #region Если нужен всплывающий урон в точке удара - раскомментировать
            //Vector3 popupDamageProjection = Camera.main.WorldToViewportPoint(hitPoint);
            //wrapperPopupDamage.position = camera2d.ViewportToWorldPoint(popupDamageProjection);
            #endregion

            lblPopupDamage.transform.localPosition
                = new Vector3(
                    x:  lblPopupDamageStartPos.x,
                    y:  lblPopupDamageStartPos.y + progress * popupDamageAnimationDistance,
                    z:  lblPopupDamageStartPos.z);

            lblPopupDamage.scale = new Vector3(scale, scale, lblPopupDamage.scale.z);

		    float alpha = (1 - progress) / (1 - dissolutionMoment);

            lblPopupDamage.color = new Color(lblPopupDamage.color.r, lblPopupDamage.color.g, lblPopupDamage.color.b, alpha);

			elapsedTime += Time.deltaTime;

			progress = elapsedTime / popupDamageAnimationTime;

			if (isScalingDirectionUp && progress > popupDamageAnimationScalingExtremum) // Смена направления скейла.
				isScalingDirectionUp = false;

		    float maxScale = popupDamageAnimationMaxScale;

		    if (GameData.IsGame(Game.BattleOfHelicopters))
		    {
		        maxScale
		            = Mathf.Lerp(
                        a:  popupDamageAnimationMaxScale,
                        b:  popupDamageAnimationMaxScaleDamageAffected,
                        t:  damage / maxPossibleDamage);
		    }

            scale
                = isScalingDirectionUp ?
				    Mathf.Lerp(lblPopupDamageStartScale, maxScale, Mathf.Clamp01(progress / popupDamageAnimationScalingExtremum)) :
				    Mathf.Lerp(maxScale, lblPopupDamageStartScale, Mathf.Clamp01((progress - popupDamageAnimationScalingExtremum) / (1f - popupDamageAnimationScalingExtremum)));

            yield return null;
		}

        lblPopupDamage.gameObject.SetActive(false);

	    popupDamageValue = 0;
	}

    public void SetupChatMessage(BattleChatPanelItemData data)
    {
        if (data == null)
            return;
        chatMessageData = data;
		chatMessage.text = chatMessageData.ChatMessage;
        chatMessageWrapper.SetActive(true);
    }

    public void SetupUsedConsumable(UsedConsumableData data)
    {
        if (data == null || !GameData.consumableInfos.ContainsKey(data.consumableId))
            return;
        usedConsumableData = data;
        usedConsumableSprite.SetSprite(GameData.consumableInfos[data.consumableId].GetIcon(withFrame: useConsumableSpritesWithFrame));
        usedConsumableWrapper.SetActive(true);
    }

    public void UpdateElements(EventId id = 0, EventInfo info = null)
    {
        //Debug.LogErrorFormat("UpdateElements of indicator {0}, Vehicle.data {1}", Vehicle != null && Vehicle.data != null ? Vehicle.data.playerName.ToString() : "NULL", Vehicle == null || Vehicle.data == null ? "NULL" : "defined");

        if (Vehicle == null || Vehicle.data == null)
            return;

        if (BattleController.MyVehicle != null)//хз..., раз была проверка - оставлю ее
        {
            if (conditionHelper)//Новая система
            {
                conditionHelper.State = Vehicle.IsMainsFriend ? 0 : 1;
                //Debug.LogErrorFormat("Set style {0} for indicator {1}", conditionHelper.StateString, Vehicle.data.playerName);
            }
            else//старая система
            {
                progressBar.BarColor = Vehicle.IsMainsFriend ? friendProgressbarFillerColor : enemyProgressbarFillerColor;
                progressBar.BGColor = Vehicle.IsMainsFriend ? friendProgressbarBgColor : enemyProgressbarBgColor;
            }
        }

        switch (ProfileInfo.AvatarOption)
        {
            case AvatarOption.showNothing:
                sprFlag.gameObject.SetActive(false);
                avatar.Hide();
                break;
            case AvatarOption.showEverything:
                sprFlag.gameObject.SetActive(!Vehicle.data.hideMyFlag);
                avatar.Show();
                GetAvatar();
                break;
            case AvatarOption.showOnlyFlags:
                sprFlag.gameObject.SetActive(!Vehicle.data.hideMyFlag);
                avatar.Hide();
                break;
            case AvatarOption.showOnlyAvatars:
                sprFlag.gameObject.SetActive(false);
                avatar.Show();
                GetAvatar();
                break;

        }

        if (!string.IsNullOrEmpty(Vehicle.data.country))
            sprFlag.SetSprite(Vehicle.data.country);
        playerName.text = Vehicle.data.playerName;
        playerName.transform.localPosition = new Vector3(!sprFlag.gameObject.activeSelf ? sprFlag.transform.localPosition.x : playerNameDefPos.x, playerNameDefPos.y, playerNameDefPos.z);

        //Выровнять спрайт прицела по правому краю логина
        sprKillerAim.transform.localPosition = new Vector3( 
            playerName.GetEstimatedMeshBoundsForString(playerName.text).size.x + 5,
            sprKillerAim.transform.localPosition.y,
            sprKillerAim.transform.localPosition.z);
    }

    private void OnFlagSettingsChanged(EventId id, EventInfo info)
    {
        if (info == null)
        {
            //старые клиенты не отправляют в этом событии аргумент(player id). Поэтому обновляться будет при любом изменении любого игрока. 
            //Медленно, но работать будет. Думаю можно выпилить эту проверку версии так... на 2.80. (Сейчас 2.60)
            UpdateElements();
        }
        else
        {
            EventInfo_I eventInfoInt = (EventInfo_I)info;
            if (eventInfoInt.int1 == Vehicle.data.playerId)
                UpdateElements();
        }
    }

    public bool IsOffenderForMain
    {
        get { return sprKillerAim.gameObject.activeSelf; }
        set { sprKillerAim.gameObject.SetActive(value); }
    }
}

public class UsedConsumableData
{
    public int photonPlayerId;
    public int consumableId;
    public float showingTime = 0;

    public bool IsLive { get { return Time.realtimeSinceStartup < showingTime + GameData.tankIndicatorUsedConsumableShowingTime; } }

    public UsedConsumableData(int _photonPlayerId, int _consumableId, float _showingTime)
    {
        photonPlayerId = _photonPlayerId;
        consumableId = _consumableId;
        showingTime = _showingTime;
    }
}