using UnityEngine;

public class FireButtonIT : FireButtonBase
{
    [Header("Настройки для IT")]

    [Header("Цвета")]
    public Color normalColor;           // Цвет нормального ненажатого состояния.
    public Color normalPushedColor;     // Цвет при выстреле пока не отпустишь кнопку.
    public Color reloadingColor;        // Цвет при перезарядке, кнопка не нажата.
    public Color reloadingPushedColor;  // Цвет при перезарядке, кнопка нажата.
    public tk2dSprite bg;
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
}
