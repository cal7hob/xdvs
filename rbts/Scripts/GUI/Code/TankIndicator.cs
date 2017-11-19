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
    public IndicatorFlag flag;
    public Avatar avatar;
    public tk2dSlicedSprite sprKillerAim;
    public GameObject wrapper;
    public Transform wrapperTankIndicator;
    public Transform wrapperPopupDamage;
    public GameObject chatMessageWrapper;
    public GameObject usedConsumableWrapper;//Отображение использования расходки
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

    /* UNITY MESSAGES */
    void Awake()
    {
        Messenger.Subscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Messenger.Subscribe(EventId.OnLanguageChange, OnLanguageChange);

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
        Messenger.Unsubscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Messenger.Unsubscribe(EventId.OnLanguageChange, OnLanguageChange);
    }

    private void OnLanguageChange(EventId evId, EventInfo ev)
    {
        AlignSprKillerAim();
    }

    void OnDisable()
    {
        lblPopupDamage.gameObject.SetActive(false);
        popupDamageValue = 0;
    }


    void Start()
    {
        playerName.text = Vehicle.data.playerName;
        playerName.maxChars = Settings.MAX_NAME_LENGTH;

        ClanNameWrapper.SetActive(!string.IsNullOrEmpty(Vehicle.data.clanName));

        awardGroup.SetActive(false);
        if (!string.IsNullOrEmpty(Vehicle.data.clanName))
        {
            clanName.text = Vehicle.data.clanName;
            awardGroup.transform.localPosition += Vector3.up * 50;
        }

        // TODO: отрефакторить вместе с другими использованиями флага
        var flagSpriteName =
            string.IsNullOrEmpty(Vehicle.data.country) || Vehicle.data.country.ToString().ToLowerInvariant() == "unknown"
                ? GameData.UNKNOWN_FLAG_NAME
                : Vehicle.data.country.ToString().ToLowerInvariant();

        flag.sprFlag.SafeSetSprite(flagSpriteName, GameData.UNKNOWN_FLAG_NAME);

        StartCoroutine(SetSprKillerSignPos());
        flag.ApplyAvatarOption(0, null);
        if (BattleController.MyVehicle != null)
            Colorize();
    }

    void Update()
    {
        if (hidden || !BattleController.MyVehicle)
            return;

        if (Camera.main.WorldToScreenPoint(Vehicle.transform.position).z > 0)
        {
            Vector3 vehicleIndicatorPointProjection = Camera.main.WorldToViewportPoint(Vehicle.IndicatorPointPosition);
            var distance = Vector3.Distance(BattleController.MyVehicle.transform.position, Vehicle.transform.position);
            var indicatorPosition = camera2d.ViewportToWorldPoint(vehicleIndicatorPointProjection);
            wrapperTankIndicator.position = indicatorPosition + distance * Vector3.up * pixelsIndicatorUpToPer1000M * 0.001f;

            #region Если нужен всплывающий урон в точке удара - закомментировать

            Vector3 popupDamageProjection = Camera.main.WorldToViewportPoint(Vehicle.AimingPoint.position);
            wrapperPopupDamage.position = camera2d.ViewportToWorldPoint(popupDamageProjection);

            #endregion

            scaleRatio = Mathf.Clamp(vehicleIndicatorPointProjection.z / -180 + 1, minScaleRatio, 1);
            indicatorLocalScale.x = indicatorLocalScale.y = indicatorLocalScale.z = scaleRatio;
            wrapper.transform.localScale = indicatorLocalScale;
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

    /* PUBLIC SECTION */
    public bool Hidden
    {
        get { return hidden; }

        set
        {
            hidden = value;
            Refresh();
        }
    }

    public void Refresh()
    {
        bool visible = !hidden && !TankIndicators.InvisibleMode;

        wrapper.SetActive(visible);

        if (!visible)
        {
            if (popupDamageLabelAnimationRoutine != null)
                StopCoroutine(popupDamageLabelAnimationRoutine);

            lblPopupDamage.gameObject.SetActive(false);

            popupDamageValue = 0;
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

    public bool IsOffenderForMain
    {
        get { return sprKillerAim.gameObject.activeSelf; }
        set { sprKillerAim.gameObject.SetActive(value); }
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
            avatar.DownloadAvatar(0, delegate (Texture2D texture)
            {
                //Debug.LogWarning("TankIndicator avatar callback");
                //Vehicle.data.avatar = texture;
            });
        }
    }


    private IEnumerator SetSprKillerSignPos()
    {
        yield return new WaitForEndOfFrame();
        AlignSprKillerAim();
    }

    /// <summary>
    /// Выровнять спрайт прицела по правому краю логина
    /// </summary>
    private void AlignSprKillerAim()
    {
        var sprKillerAimPos = Vector3.zero;
        sprKillerAimPos.x = playerName.GetComponent<Renderer>().bounds.max.x + 5;
        sprKillerAimPos.y = sprKillerAim.transform.position.y;
        sprKillerAimPos.z = sprKillerAim.transform.position.z;//IMPORTANT
        sprKillerAim.transform.position = sprKillerAimPos;
    }

    private void OnMainTankAppeared(EventId id, EventInfo info)
    {
        Colorize();
    }

    private void Colorize()
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
                    x: lblPopupDamageStartPos.x,
                    y: lblPopupDamageStartPos.y + progress * popupDamageAnimationDistance,
                    z: lblPopupDamageStartPos.z);

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
                        a: popupDamageAnimationMaxScale,
                        b: popupDamageAnimationMaxScaleDamageAffected,
                        t: damage / maxPossibleDamage);
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
        usedConsumableSprite.SetSprite(GameData.consumableInfos[data.consumableId].icon);
        usedConsumableWrapper.SetActive(true);
    }
}

public class UsedConsumableData
{
    public int photonPlayerId;
    public int consumableId;
    public float hideTime;

    public bool IsLive { get { return Time.time < hideTime; } }

    public UsedConsumableData(int _photonPlayerId, int _consumableId)
    {
        photonPlayerId = _photonPlayerId;
        consumableId = _consumableId;
        hideTime = Time.time + GameData.tankIndicatorUsedConsumableShowingTime;
    }
}