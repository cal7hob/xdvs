using UnityEngine;

public class FireButtonTW : FireButtonBase
{
    [Header("Настройки для TW")]
    public tk2dSprite bg;
    [Header("Цвета")]
    public Color normalColor;           // Цвет нормального ненажатого состояния.
    public Color normalPushedColor;     // Цвет при выстреле пока не отпустишь кнопку.
    public Color reloadingColor;        // Цвет при перезарядке, кнопка не нажата.
    public Color reloadingPushedColor;  // Цвет при перезарядке, кнопка нажата.

    protected override void FireButton_Down()
    {
        SetColor(IsReloading ? reloadingPushedColor : normalPushedColor);
    }

    protected override void FireButton_Up()
    {
        SetColor(IsReloading ? reloadingColor : normalColor);
    }

    protected override void OnReloaded(EventId id, EventInfo ei)
    {
        base.OnReloaded(id, ei);

        if (!fireButton.IsPressed)
            SetColor(normalColor);
    }

    private void SetColor(Color color)
    {
        sprFireButton.color = color;
    }
    public override Rect Coord()
    {
        var joyWorldTopRight = bg.transform.TransformPoint(bg.GetBounds().max);
        var joyScreenTopRight = BattleGUI.Instance.GuiCamera.WorldToScreenPoint(joyWorldTopRight);
        var joyWorldBottomLeft = bg.transform.TransformPoint(bg.GetBounds().min);
        var joyScreenBottomLeft = BattleGUI.Instance.GuiCamera.WorldToScreenPoint(joyWorldBottomLeft);

        var Area = new Rect
        {
            xMin = joyScreenBottomLeft.x,
            yMin = joyScreenBottomLeft.y,
            xMax = joyScreenTopRight.x,
            yMax = joyScreenTopRight.y,
        };
        return Area;
    }
}
