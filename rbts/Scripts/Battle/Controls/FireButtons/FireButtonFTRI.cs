using UnityEngine;

public class FireButtonFTRI : FireButtonBase
{
    [Header("Настройки для FTRI")]

    [Header("Ссылки")]
    public tk2dSprite sprCenterFireButton;
    public tk2dSprite bg;
    [Header("Цвета")]
    public Color normalColor;
    public Color reloadingColor;

    protected override void FireButton_Down() { }

    protected override void FireButton_Up() { }

    private void Update()
    {
        if (!BattleController.MyVehicle)
            return;

        if (!HelpTools.Approximately(BattleController.MyVehicle.WeaponReloadingProgress, 1, 0.0001f))
        {
            var inverted = BattleController.MyVehicle.VehicleType == VehicleInfo.VehicleType.Robot;

            var color = inverted ? Color.Lerp(normalColor, reloadingColor, BattleController.MyVehicle.WeaponReloadingProgress)
                : Color.Lerp(reloadingColor, normalColor, BattleController.MyVehicle.WeaponReloadingProgress);

            SetColor(color);
        }
    }

    private void SetColor(Color color)
    {
        sprFireButton.color = color;
        sprCenterFireButton.color = color;
    }
}
