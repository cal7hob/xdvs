using UnityEngine;

public class ArmadaUpDownButton : tk2dUIBaseItemControl
{
    [Header("Изменение цвета")]
    public GameObject[] objectsToChangeColor;

    public Color upStateSpriteColor = Color.white;
    public Color downStateSpriteColor = Color.white;

    public Color upStateLabelColor = Color.white;
    public Color downStateLabelColor = Color.white;

    private void Start()
    {
        SetColor();
    }

    private void OnEnable()
    {
        if (uiItem)
        {
            uiItem.OnDown += ButtonDown;
            uiItem.OnUp += ButtonUp;
        }
    }

    private void OnDisable()
    {
        if (uiItem)
        {
            uiItem.OnDown -= ButtonDown;
            uiItem.OnUp -= ButtonUp;
        }
    }

    private void ButtonUp()
    {
        SetColor();
    }

    private void ButtonDown()
    {
        SetColor();
    }

    protected void SetColor()
    {
        if (objectsToChangeColor != null)
        {
            for (int i = 0; i < objectsToChangeColor.Length; i++)
            {
                var sprite = objectsToChangeColor[i].GetComponent<tk2dBaseSprite>();

                if (sprite)
                {
                    sprite.color = uiItem.IsPressed ? downStateSpriteColor : upStateSpriteColor;
                }

                var lbl = objectsToChangeColor[i].GetComponent<tk2dTextMesh>();

                if (lbl)
                {
                    lbl.color = uiItem.IsPressed ? downStateLabelColor : upStateLabelColor;
                }
            }
        }
    }
}
