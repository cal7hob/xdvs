using System;
using UnityEngine;

public class DeltaProgressBar : ProgressBar
{
    public const int ARROW_UP_CODE = 0x25B2;
    public const int ARROW_DOWN_CODE = 0x25BC;
    [Header("------Delta Progress Bar Vars-----")]
    [Tooltip("Чем меньше параметр, тем лучше")]
    [SerializeField]
    private bool inverted;
    [SerializeField]
    private tk2dSlicedSprite deltaSpritePos;
    [SerializeField]
    private tk2dSlicedSprite deltaSpriteNeg;
    [SerializeField]
    private float minDeltaSpriteLength = 0;//Обычно это сумма левого и правого бордеров слайсед спрайта
    [SerializeField]
    private float deltaSpriteLeftBorder = 0;//Без учета скейла(как бордер слайсед спрайта). Сколько пикселей от края до начала неградиентной части
    //[SerializeField]private float deltaSpriteRightBorder = 0;//Без учета скейла(как бордер слайсед спрайта).Сколько пикселей от правого края до начала неградиентной части

    //[SerializeField]private float deltaExtraOffset = 0;//Уже умноженное на скейл.Переменная для точной настройки положения дельта спрайта. Но если дельта спрайт такой же как филлер спрайт - не используется
    [SerializeField]
    private bool moveDeltaLabelForTheValueLabel = true;
    [SerializeField]
    private float spaceBetweenValueAndDeltaLabels = 15;//Если установлена переменная moveDeltaLabelForTheValueLabel, то располагаем deltaLabel c учетом ширины текста valueLabel и spaceBetweenValueAndDeltaLabels
    [SerializeField]
    private bool showDeltaSign = false;
    [SerializeField]
    private bool addArrowSymbolToDeltaLabel = true;

    //Для новой системы, в которой в шрифт не добавляются символы 0x25B2. Вместо этого добавляется спрайт.
    [SerializeField]
    private HorizontalLayout valueHorizontalLayout;
    [SerializeField]
    private GameObject arrowSpritePos;
    [SerializeField]
    private GameObject arrowSpriteNeg;

    [SerializeField]
    private tk2dTextMesh titleLabel;
    [SerializeField]
    private tk2dTextMesh valueLabel;
    [SerializeField]
    private tk2dTextMesh deltaLabel;
    [SerializeField]
    private int decimalDigits;
    [Header("Цвет дельта лейблов")]
    [SerializeField]
    private Color positiveColor;
    [SerializeField]
    private Color negativeColor;



    protected float max;
    protected float primaryValue;
    protected float secondaryValue;
    protected string valueSuffix;

    protected bool realDelta;

    public float Max
    {
        get { return max; }
        set
        {
            max = value;
            primaryValue = Mathf.Clamp(primaryValue, 0, max);
            secondaryValue = Mathf.Clamp(secondaryValue, 0, max);
            Repaint();
        }
    }

    public float PrimaryValue
    {
        get { return primaryValue; }
        set
        {
            primaryValue = value;
            Repaint(); 
        }
    }

    public float SecondaryValue
    {
        get { return secondaryValue; }
        set
        {
            secondaryValue = value;
            Repaint();
        }
    }

    public bool Inverted
    {
        get { return inverted; }
        set
        {
            inverted = value;
            Repaint();
        }
    }

    public string Title
    {
        get { return titleLabel.text; }
        set { titleLabel.text = value; }
    }

    public void Repaint()
    {
        // Check if delta is not zero
        float _primaryValue = Mathf.Clamp(primaryValue, 0, max);
        float _secondaryValue = Mathf.Clamp(secondaryValue, 0, max);

        Percentage = Mathf.Approximately(max, 0) ? 0 : _primaryValue / max;
        valueLabel.text = string.Format("{0}{1}", Math.Round(secondaryValue, decimalDigits), valueSuffix);
        float delta = _secondaryValue - _primaryValue;
        bool positiveChange = delta > 0 != inverted;
        realDelta = !HelpTools.Approximately(primaryValue, secondaryValue);
        deltaLabel.gameObject.SetActive(realDelta);

        if (!realDelta)
        {
            deltaSpriteNeg.gameObject.SetActive(false);
            deltaSpritePos.gameObject.SetActive(false);
            if (arrowSpritePos != null)
                arrowSpritePos.SetActive(false);
            if (arrowSpriteNeg != null)
                arrowSpriteNeg.SetActive(false);
            return;
        }

        /************ Настраиваем delta спрайт  ************/
        tk2dSlicedSprite enabledSprite =  positiveChange ? deltaSpritePos : deltaSpriteNeg;
        tk2dSlicedSprite disabledSprite = positiveChange ? deltaSpriteNeg : deltaSpritePos;

        disabledSprite.gameObject.SetActive(false);
        enabledSprite.gameObject.SetActive(true);

        if (arrowSpritePos != null)
            arrowSpritePos.SetActive(deltaSpritePos.gameObject.activeSelf);
        if (arrowSpriteNeg != null)
            arrowSpriteNeg.SetActive(deltaSpriteNeg.gameObject.activeSelf);

        float part = Mathf.Abs(delta / max);//Часть от всей шкалы
        float enabledSpriteLengthReal = part * clearSize;//Часть от всей шкалы в пикселях
        float edge = FillerEdge + enabledSpriteLengthReal * Mathf.Sign(delta);

        enabledSprite.transform.localPosition = new Vector3(
            Mathf.Clamp(edge, FillerEdge, fullSize + usedFillerSprite.transform.localPosition.x),// -0.5f из-за бага юнити/тулкита при котором 2 одинаковых спрайта с левым и правым анкором налазят друг на друга на пол пикселя.
            enabledSprite.transform.localPosition.y,
            enabledSprite.transform.localPosition.z);

        float deltaSize = 0;
        if (delta > 0)
            deltaSize = Mathf.CeilToInt(enabledSpriteLengthReal / enabledSprite.scale.x + deltaSpriteLeftBorder + rightBorder);//dimmension не должен быть дробным, иначе спрайт отодвигается в сторону анкора (если MiddleRight - отклоняется в право, и образуется щель между филлером и дельта спрайтом)
        else
            deltaSize = Mathf.CeilToInt(enabledSpriteLengthReal / enabledSprite.scale.x + rightGradientBorder + leftGradientBorder);
        if (deltaSize < minDeltaSpriteLength)
            deltaSize = minDeltaSpriteLength;
        enabledSprite.dimensions = new Vector2(deltaSize, enabledSprite.dimensions.y);

        /************ Настраиваем deltaLabel  ************/

        deltaLabel.color = positiveChange ? positiveColor : negativeColor;

        deltaLabel.text = string.Format( "{1}  {2}{0:0.#}",
                Math.Round(Mathf.Abs(delta), decimalDigits),
                addArrowSymbolToDeltaLabel ? Char.ConvertFromUtf32(delta > 0 ? ARROW_UP_CODE : ARROW_DOWN_CODE) : "",
                showDeltaSign ? (delta >= 0 ? "+" : "-") : "");

        if (valueHorizontalLayout != null)//Если используем HorizontalLayout для выравнивания текста дельта лейблов и спрайта - переменная moveDeltaLabelForTheValueLabel не используется.
        {
            deltaLabel.Commit();
            valueLabel.Commit();
            valueHorizontalLayout.Align();
        }
        else 
        {
            if (moveDeltaLabelForTheValueLabel)
            {
                deltaLabel.transform.localPosition = new Vector3(
                    valueLabel.transform.localPosition.x - (valueLabel.GetEstimatedMeshBoundsForString(valueLabel.text).size.x + spaceBetweenValueAndDeltaLabels),
                    deltaLabel.transform.localPosition.y,
                    deltaLabel.transform.localPosition.z);
            }
        }

        
    }
}
