using System;
using System.Collections;
using System.Linq;
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
    public bool useConsumableSpritesWithFrame = true;
    [Header("Форматируем по простому...")]
    public Color friendProgressbarBgColor;
    public Color friendProgressbarFillerColor;
    public Color enemyProgressbarBgColor;
    public Color enemyProgressbarFillerColor;
    [Header("... или используем ConditionHelper")]
    public InterfaceExtensions.ConditionHelper conditionHelper;//Настройка префаба в зависимости от того это индикатор врага или друга

    public const float offset = 2.134f;
    public tk2dTextMesh lblPopupDamageOriginal;
    public tk2dTextMesh[] lblPopupDamage;
    public tk2dTextMesh playerName;
    public tk2dTextMesh lblAward;
    public tk2dTextMesh clanName;
    public GameObject clanNameWrapper;
    public tk2dTextMesh chatMessage;
    public tk2dBaseSprite usedConsumableSprite;
    public float minScaleRatio = 0.5f;
    public float pixelsIndicatorUpToPer1000M = 1f;
    public int popupItemsCount = 20;
    private GameObject ClanNameWrapper { get { return clanNameWrapper ? clanNameWrapper : clanName.gameObject; } }//Убрать свойство когда сделаю для всех проектов

    private BattleChatPanelItemData chatMessageData;

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
    private UsedConsumableData usedConsumableData;

    /* UNITY MESSAGES */
    void Awake()
    {

        Dispatcher.Subscribe(EventId.TankKilled, OnTankKilled, 2);
        Dispatcher.Subscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Dispatcher.Subscribe(EventId.OnLanguageChange, OnLanguageChange);
        if (chatMessageWrapper)
            chatMessageWrapper.SetActive(false);
        if (usedConsumableWrapper)
            usedConsumableWrapper.SetActive(false);
        lblPopupDamage = new tk2dTextMesh[popupItemsCount];
        for (int i = 0; i < popupItemsCount; i++)
        {
            lblPopupDamage[i] = Instantiate(lblPopupDamageOriginal, lblPopupDamageOriginal.transform.parent);
            lblPopupDamage[i].color = new Color(lblPopupDamage[i].color.r, lblPopupDamage[i].color.g, lblPopupDamage[i].color.b, 0);
        }
        lblPopupDamageStartPos = lblPopupDamage[0].transform.localPosition;
        lblPopupDamageStartScale = lblPopupDamage[0].scale.x;
        lblPopupDamageOriginal.gameObject.SetActive(false); // Прячем лейбл.
        foreach (var vr in lblPopupDamage)
        {
            vr.gameObject.SetActive(false);
        }
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
        Dispatcher.Unsubscribe(EventId.TankKilled, OnTankKilled);
        Dispatcher.Unsubscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Dispatcher.Unsubscribe(EventId.OnLanguageChange, OnLanguageChange);
    }

    private void OnLanguageChange(EventId evId, EventInfo ev)
    {
        AlignSprKillerAim();
    }

    void OnDisable()
    {
        foreach (var vr in lblPopupDamage)
        {
            vr.gameObject.SetActive(false);
            vr.color = new Color(vr.color.r, vr.color.g, vr.color.b, 0);
        }
        lblPopupDamageOriginal.gameObject.SetActive(false);
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

        if (!string.IsNullOrEmpty(Vehicle.data.country))
            flag.sprFlag.SetSprite(Vehicle.data.country);

        StartCoroutine(SetSprKillerSignPos());
        flag.ApplyAvatarOption(0, null);
        if (BattleController.MyVehicle != null)
            Colorize();
    }

    void Update()
    {
        if (hidden || !BattleController.MyVehicle)
            return;

        MoveIndicatorToHimPosition();
        #region Выключение итема чата
        if (chatMessageWrapper && chatMessageWrapper.activeSelf && chatMessageData != null && !chatMessageData.IsLive)
            chatMessageWrapper.SetActive(false);
        #endregion
        if (usedConsumableWrapper && usedConsumableWrapper.activeSelf && usedConsumableData != null && !usedConsumableData.IsLive)
            usedConsumableWrapper.SetActive(false);

    }

    void MoveIndicatorToHimPosition()
    {
        if (Camera.main.WorldToScreenPoint(Vehicle.transform.position).z > 0)
        {
            Vector3 vehicleIndicatorPointProjection = Camera.main.WorldToViewportPoint(Vehicle.transform.position + new Vector3(0, offset, 0));
            var distance = Vector3.Distance(BattleController.MyVehicle.transform.position, Vehicle.transform.position);
            var indicatorPosition = camera2d.ViewportToWorldPoint(vehicleIndicatorPointProjection);
            wrapperTankIndicator.position = indicatorPosition + distance * Vector3.up * pixelsIndicatorUpToPer1000M * 0.001f;

            #region Если нужен всплывающий урон в точке удара - закомментировать

            Vector3 popupDamageProjection = Camera.main.WorldToViewportPoint(Vehicle.transform.position);//turretController.CannonEnd.position);
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
    }
    /* PUBLIC SECTION */
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
                {
                    StopAllCoroutines();
                }

                lblPopupDamageOriginal.gameObject.SetActive(false);
                foreach (var vr in lblPopupDamage)
                {
                    vr.gameObject.SetActive(false);
                    vr.color = new Color(vr.color.r, vr.color.g, vr.color.b, 0);
                }
                popupDamageValue = 0;
            }
            else
            {
                MoveIndicatorToHimPosition();
            }
            wrapper.SetActive(!hidden);
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

    public void OnTankKilled(EventId id, EventInfo info)
    {
        var eventInfo = (EventInfo_II)info;

        if (eventInfo == null)
        {
            return;
        }

        int victimId = eventInfo.int1;
        int killerId = eventInfo.int2;

        if (killerId == Vehicle.data.playerId && victimId == BattleController.MyPlayerId)
        {
            sprKillerAim.gameObject.SetActive(true);
            LastKillerId = killerId;
        }
        else if (LastKillerId != killerId && victimId == BattleController.MyPlayerId || (LastKillerId == victimId && killerId == BattleController.MyPlayerId))
        {
            sprKillerAim.gameObject.SetActive(false);
        }

        //if (VehicleController.data.playerId == killerId)
        //    LastVictimId = victimId;
    }

    public void AnimateLblPopupDamage(int damage, Vector3 hitPoint)
    {
        if (!usePopupDamageAnimation || Hidden || !gameObject.activeInHierarchy)
            return;
        var linq = SafeLinq.Min(lblPopupDamage.Select(selector => selector.color.a)); //Можно же проще? И лучше? 

        foreach (var label in lblPopupDamage)
        {
            if (HelpTools.Approximately(label.color.a, linq))
            {
                popupDamageValue = damage;
                label.text = popupDamageValue.ToString();
                if (!label.gameObject.activeSelf)
                {
                    popupDamageLabelAnimationRoutine = AnimateLblPopupDamageCoroutine(damage, hitPoint, label);
                    StartCoroutine(popupDamageLabelAnimationRoutine);
                }
                break;
            }
            //if (HelpTools.Approximately(label.color.a, 0))
            //{
            //    popupDamageValue = damage;
            //    label.text = popupDamageValue.ToString();
            //    if (!label.gameObject.activeSelf)
            //    {
            //        popupDamageLabelAnimationRoutine = AnimateLblPopupDamageCoroutine(damage, hitPoint, label);
            //        StartCoroutine(popupDamageLabelAnimationRoutine);
            //    }
            //    break;
            //}
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

    public int LastKillerId { get; private set; }
    //public int LastVictimId { get; private set; }

    /* PRIVATE SECTION */

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

    private IEnumerator AnimateLblPopupDamageCoroutine(float damage, Vector3 hitPoint, tk2dTextMesh label)
    {
        label.gameObject.SetActive(true);

        float elapsedTime = 0;
        float progress = 0;
        float scale = lblPopupDamageStartScale;

        bool isScalingDirectionUp = true;

        label.transform.localPosition = lblPopupDamageStartPos;
        label.color = new Color(label.color.r, label.color.g, label.color.b, 1);

        while (progress < 1 && label.gameObject.activeInHierarchy)
        {
            #region Если нужен всплывающий урон в точке удара - раскомментировать
            Vector3 popupDamageProjection = Camera.main.WorldToViewportPoint(hitPoint);
            wrapperPopupDamage.position = camera2d.ViewportToWorldPoint(popupDamageProjection);
            #endregion

            label.transform.localPosition
                = new Vector3(
                    x: lblPopupDamageStartPos.x,
                    y: lblPopupDamageStartPos.y + progress * popupDamageAnimationDistance,
                    z: lblPopupDamageStartPos.z);

            label.scale = new Vector3(scale, scale, label.scale.z);

            float alpha = (1 - progress) / (1 - dissolutionMoment);
            label.color = new Color(label.color.r, label.color.g, label.color.b, alpha);

            elapsedTime += Time.deltaTime;

            progress = elapsedTime / popupDamageAnimationTime;

            if (isScalingDirectionUp && progress > popupDamageAnimationScalingExtremum) // Смена направления скейла.
                isScalingDirectionUp = false;

            float maxScale = popupDamageAnimationMaxScale;

            scale
                = isScalingDirectionUp ?
                    Mathf.Lerp(lblPopupDamageStartScale, maxScale, Mathf.Clamp01(progress / popupDamageAnimationScalingExtremum)) :
                    Mathf.Lerp(maxScale, lblPopupDamageStartScale, Mathf.Clamp01((progress - popupDamageAnimationScalingExtremum) / (1f - popupDamageAnimationScalingExtremum)));

            yield return null;
        }
        label.color = new Color(label.color.r, label.color.g, label.color.b, 0);
        label.gameObject.SetActive(false);

        popupDamageValue = 0;
    }

    public void SetupChatMessage(BattleChatPanelItemData data)
    {
        if (data == null)
            return;
        chatMessageData = data;
        chatMessage.text = chatMessageData.Message;
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