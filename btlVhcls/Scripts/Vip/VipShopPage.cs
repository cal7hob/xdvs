using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class VipShopPage : MonoBehaviour
{
    public tk2dBaseSprite icoVipFuelBar;
    public tk2dBaseSprite icoVipUserLevel;
    public FuelBarManager fuelBarManagerFromTopPanel;
    public tk2dTextMesh lblVipTimer;
    public UniAlignerBase headerAligner;
    public tk2dUIScrollableArea scrollableArea;

    private List<tk2dBaseSprite> blinkingSprites = new List<tk2dBaseSprite>();
    private List<IEnumerator> blinkingRoutines = new List<IEnumerator>();
    private List<Color> initialColors = new List<Color>();
    private bool isInited = false;

    public static bool IsOnScreen { get; private set; }

    void Awake()
    {
        Dispatcher.Subscribe(EventId.PageChanged, OnPageChanged);
        Dispatcher.Subscribe(EventId.VipStatusUpdated, OnVipStatusUpdated);
        Init();
    }

    private void Init()
    {
        if (isInited)
            return;
        //Весь этот гемор с функцией инициализации пришлось сделать из-за того, что мы обращаемся к fuelBarManagerFromTopPanel
        //до его Awake. Это происходит например при дейли бонусе - когда верхняя панель отключена
        List<tk2dBaseSprite> list = fuelBarManagerFromTopPanel.GetFuelBonusBars();
        if (list == null)
            return;

        blinkingSprites.AddRange(fuelBarManagerFromTopPanel.GetFuelBonusBars());
        blinkingSprites.Add(icoVipFuelBar);
        blinkingSprites.Add(icoVipUserLevel);

        //Сохраняем начальное состояние спрайтов
        foreach (var spr in blinkingSprites)
            initialColors.Add(spr.color);

        isInited = true;
    }

    private void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.VipStatusUpdated, OnVipStatusUpdated);
        Dispatcher.Unsubscribe(EventId.PageChanged, OnPageChanged);
    }

    private void OnVipStatusUpdated(EventId eventId, EventInfo eventInfo)
    {
        UpdateHeader();
    }

    private void OnPageChanged(EventId eventId, EventInfo eventInfo)
    {
        if (GUIPager.ActivePageName.Contains("VipAccountShop"))
        {
            Init();
            IsOnScreen = true;
            UpdateHeader();
            if (ProfileInfo.IsPlayerVip)
                return;

            //Создаем корутниы мигания и запускаем их
            blinkingRoutines.Clear();
            foreach (var spr in blinkingSprites)
            {
                IEnumerator routine = MiscTools.BlinkingRoutine(spr, 1);
                blinkingRoutines.Add(routine);
                StartCoroutine(routine);
            }
        }
        else
        {
            IsOnScreen = false;

            if (!isInited || ProfileInfo.IsPlayerVip)
                return;

            //Останавливаем корутины и возвращаем начальное состояние спрайтов
            if (blinkingRoutines.Count > 0)
            {
                foreach (var routine in blinkingRoutines)
                    StopCoroutine(routine);
                for (int i = 0; i < blinkingSprites.Count; i++)
                    blinkingSprites[i].color = initialColors[i];
            }
        }
    }

    private void UpdateHeader()
    {
        if (lblVipTimer)//Сам лейбл устанавливается скриптом VipManager
            lblVipTimer.gameObject.SetActive(ProfileInfo.IsPlayerVip);
        if (headerAligner != null)
            headerAligner.Align();
    }
}
