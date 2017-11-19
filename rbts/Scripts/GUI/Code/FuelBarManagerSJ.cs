using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FuelBarManagerSJ : FuelBarManager
{

    private List<tk2dBaseSprite> fuelBars = new List<tk2dBaseSprite>();
    public float spaceBetweenCans = 0;
    public string emptyCanSpriteName = "fuel_empty";
    public string filledCanSpriteName = "fuel_filled";
    public string disabledCanSpriteName = "fuel_disabled";

    public List<tk2dBaseSprite> ExtraFuelBars { get; private set; } 

    protected override void Awake()
    {
        fuelBars.Add(emptyFuelCan.GetComponent<tk2dBaseSprite>());
        for (int i = 1; i < (GameData.STANDART_FUEL_CAN_AMOUNT + GameData.EXTRA_FUEL_CAN_AMOUNT); i++)
        {
            fuelBars.Add(Instantiate(emptyFuelCan.transform).GetComponent<tk2dBaseSprite>());
            fuelBars[i].name = string.Format("Can_{0:00}",i);
            fuelBars[i].transform.parent = emptyFuelCan.transform.parent;
            fuelBars[i].transform.localPosition = new Vector3(emptyFuelCan.transform.localPosition.x + (float)i* (fuelCanWidth + spaceBetweenCans), emptyFuelCan.transform.localPosition.y, emptyFuelCan.transform.localPosition.z);
        }

        ExtraFuelBars = new List<tk2dBaseSprite>();
        ExtraFuelBars.AddRange(fuelBars.GetRange(fuelBars.Count - 2, 2));

        base.Awake();
    }


    public override List<tk2dBaseSprite> GetFuelBonusBars()
    {
        return ExtraFuelBars;
    }

    /// <summary>
    /// update fuel cans visual state (redraw all fuel cans)
    /// </summary>
    protected override void UpdateFuelCans()
    {
        if (!isInited)
            Init();
        _totalCanAmount = ProfileInfo.MaxFuel;//Хз надо или нет

        for (int i = 0; i < fuelBars.Count; i++)
        {
            if (i >= GameData.STANDART_FUEL_CAN_AMOUNT && !ProfileInfo.IsPlayerVip && !ProfileInfo.vehicleUpgrades.ContainsKey(GameData.EXTRA_FUEL_VEHICLE_ID))//Если это дополнительная ячейка
                fuelBars[i].SetSprite(disabledCanSpriteName);
            else
                fuelBars[i].SetSprite(i < (int)ProfileInfo.Fuel ? filledCanSpriteName : emptyCanSpriteName);
        }
    }
}
