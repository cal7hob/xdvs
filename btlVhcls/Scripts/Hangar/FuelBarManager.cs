using System;
using UnityEngine;
using System.Collections.Generic;

public class FuelBarManager : MonoBehaviour
{
    /// <summary>
    /// gameObject for common fuel cans
    /// </summary>
    public GameObject commonFuelCan;

    /// <summary>
    /// vip fuel cans' gameobject
    /// </summary>
    public GameObject vipFuelCan;

    /// <summary>
    /// empty cans' gameObject
    /// </summary>
    public GameObject emptyFuelCan;

    /// <summary>
    /// fuel can height (must be == for all fuel can types)
    /// </summary>
    public float fuelCanHeight = 40;

    /// <summary>
    /// fuel can width (must be == for all fuel can types)
    /// </summary>
    public float fuelCanWidth = 30;

    public Color vipFuelCanEnabledColor = new Color(1f, 1f, 1f, 1f);
    public Color vipFuelCanDisabledColor = new Color(1f, 1f, 1f, 0.3f);


    public static FuelBarManager Instance { get { return _instance; } }
    private static FuelBarManager _instance;
    private tk2dTiledSprite vipFuelCanSprite;
    protected bool isInited = false;


    /// <summary>
    /// total allowed for use fuel cans
    /// </summary>
    public int TotalCanAmount
    {
        get { return _totalCanAmount; }
        set
        {
            if (_totalCanAmount == value) return;
            _totalCanAmount = value;
            UpdateFuelCans();
        }
    }
    protected int _totalCanAmount;


    /// <summary>
    /// amount of filled fuel cans
    /// </summary>
    public int FilledCanAmount
    {
        get { return _filledCanAmount; }
        set
        {
            _filledCanAmount = value;
            UpdateFuelCans();
        }
    }
    private int _filledCanAmount;


    protected virtual void Awake()
    {
        Init();
        UpdateFuelCans();
    }

    protected virtual void Init()
    {
        if (isInited)
            return;
        _instance = this;

        if (commonFuelCan == null || vipFuelCan == null || emptyFuelCan == null)
            throw new NullReferenceException("Some of fuel cans was not set");

        Dispatcher.Subscribe(EventId.FuelUpdated, FuelUpdatedHandler);
        Dispatcher.Subscribe(EventId.MaxFuelUpdated, MaxFuelUpdatedHandler);

        vipFuelCanSprite = vipFuelCan.GetComponent<tk2dTiledSprite>();

        isInited = true;
    }

    private void MaxFuelUpdatedHandler(EventId eventId, EventInfo eventInfo)
    {
        TotalCanAmount = ((EventInfo_I)eventInfo).int1;
    }


    private void FuelUpdatedHandler(EventId eventId, EventInfo eventInfo)
    {
        FilledCanAmount = ((EventInfo_I)eventInfo).int1;
    }


    protected virtual void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.FuelUpdated, FuelUpdatedHandler);
        Dispatcher.Unsubscribe(EventId.MaxFuelUpdated, MaxFuelUpdatedHandler);
        _instance = null;
    }

    public virtual List<tk2dBaseSprite> GetFuelBonusBars()
    {
        return new List<tk2dBaseSprite>() { vipFuelCan.GetComponent<tk2dBaseSprite>() };
    }

    /// <summary>
    /// use it to get know where to place fuel can
    /// </summary>
    /// <param name="canNewAmount"></param>
    /// <returns></returns>
    private float GetNewXSize(int canNewAmount)
    {
        return fuelCanWidth * canNewAmount;
    }


    /// <summary>
    /// update fuel cans visual state (redraw all fuel cans)
    /// </summary>
    protected virtual void UpdateFuelCans()
    {
        if (!isInited)
            Init();
        int filledAmount = (int)ProfileInfo.Fuel;
        _totalCanAmount = ProfileInfo.MaxFuel;

        // update extra/vip fuel cans
        if (!ProfileInfo.IsPlayerVip && !ProfileInfo.vehicleUpgrades.ContainsKey(GameData.EXTRA_FUEL_VEHICLE_ID) && !VipShopPage.IsOnScreen)
        {
            // if user is not Vip - fill Vip cans and do not use them (lock them?)
            vipFuelCan.GetComponent<tk2dTiledSprite>().dimensions =
                new Vector2(GetNewXSize(GameData.EXTRA_FUEL_CAN_AMOUNT), fuelCanHeight);
            // make extra cans transparent
            if(GameData.IsGame(Game.BattleOfHelicopters))//Способ №1 - целиком задавая цвет
                vipFuelCanSprite.color = vipFuelCanDisabledColor;//Способ №1 - целиком задавая цвет
            else
            {
                var oldColor = vipFuelCanSprite.color;
                vipFuelCanSprite.color = new Color(oldColor.r, oldColor.g, oldColor.b, 0.3f);//Способ №2 - задавая только альфу
            }
        }
        else if (ProfileInfo.IsPlayerVip || ProfileInfo.vehicleUpgrades.ContainsKey(GameData.EXTRA_FUEL_VEHICLE_ID))
        {
            var filledVipCans = 0;
            if (filledAmount > GameData.STANDART_FUEL_CAN_AMOUNT)
                filledVipCans = GameData.EXTRA_FUEL_CAN_AMOUNT - (TotalCanAmount - filledAmount);
            // if user is Vip or user has tank with bonus fuel cans
            vipFuelCan.GetComponent<tk2dTiledSprite>().dimensions =
                new Vector2(GetNewXSize(filledVipCans), fuelCanHeight);
            // make extra cans visible
            if (GameData.IsGame(Game.BattleOfHelicopters))
                vipFuelCanSprite.color = vipFuelCanEnabledColor;
            else
            {
                var oldColor = vipFuelCanSprite.color;
                vipFuelCanSprite.color = new Color(oldColor.r, oldColor.g, oldColor.b, 1f);
            }
        }
        var commonFilledCans = filledAmount > GameData.STANDART_FUEL_CAN_AMOUNT
            ? GameData.STANDART_FUEL_CAN_AMOUNT
            : filledAmount;
        // update common fuel cans
        commonFuelCan.GetComponent<tk2dTiledSprite>().dimensions =
            new Vector2(GetNewXSize(commonFilledCans), fuelCanHeight);
        // update empty fuel cans
        emptyFuelCan.GetComponent<tk2dTiledSprite>().dimensions =
            new Vector2(GetNewXSize(TotalCanAmount), fuelCanHeight);
    }
}
