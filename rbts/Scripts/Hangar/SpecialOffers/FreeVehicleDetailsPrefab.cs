using System;
using UnityEngine;
using System.Collections;

public class FreeVehicleDetailsPrefab : MonoBehaviour
{

    [SerializeField] private tk2dTextMesh lblGain;
    [SerializeField] private tk2dTextMesh lblFuel;

    void Start()
    {
        Init();
    }

    private void Init()
    {
        if (!HangarController.Instance.IsInitialized)
            return;
        
        var freeCamo = PatternPool.Instance.GetItemById(PatternOffersController.FreeVehicleCamos[GameData.ClearGameFlags(GameData.CurrentGame)]);

        if (freeCamo == null)
        {
            Debug.LogWarning("PatternShop doesn't contain pattern ID " + PatternOffersController.FreeVehicleCamos[GameData.ClearGameFlags(GameData.CurrentGame)]);
            return;
        }

        var gain
            = (GameData.IsGame(Game.BattleOfWarplanes | Game.BattleOfHelicopters | Game.ApocalypticCars)
                ? freeCamo.rocketDamageGain
                : freeCamo.damageGain)
                    * 100;

        lblGain.text = string.Format("+{0}%", gain);
    }
}
