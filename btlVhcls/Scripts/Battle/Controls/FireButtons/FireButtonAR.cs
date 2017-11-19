using UnityEngine;

public class FireButtonAR : FireButtonBase
{
    [Header("Настройки для AR")]

    [Header("Ссылки")]
    [SerializeField] private tk2dSprite sprIcon;
    [SerializeField] private tk2dSprite sprGlow;

    [Header("Цвета")]
    [SerializeField] private Color sprIconNormalColor;
    [SerializeField] private Color sprIconReloadingColor;

    protected override void FireButton_Down()
    {
        SetColor();
    }

    protected override void FireButton_Up()
    {
        SetColor();
    }

    protected override void OnReloaded(EventId id, EventInfo ei)
    {
        base.OnReloaded(id, ei);
        SetColor();
    }

    private void SetColor()
    {
        sprGlow.gameObject.SetActive(fireButton.IsPressed);
        sprIcon.color = IsReloading ? sprIconReloadingColor : sprIconNormalColor;
    }
}

