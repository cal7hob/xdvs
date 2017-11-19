using UnityEngine;

public class FireButtonSJ : FireButtonBase
{
    [Header("Настройки для SJ")]

    [Header("Цвета")]
    public Color normalColor;           // Цвет нормального ненажатого состояния.
    public Color reloadingColor;        // Цвет при перезарядке, кнопка не нажата.
    public Color reloadingPushedColor;  // Цвет при перезарядке, кнопка нажата.

    protected override void FireButton_Down()
    {
        SetColor(reloadingPushedColor);
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
