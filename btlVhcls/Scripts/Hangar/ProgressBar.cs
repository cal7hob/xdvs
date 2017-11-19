//https://monosnap.com/file/NVFP8DnpdrvgWY0iQplW7HWLX7hTD2

using UnityEngine;
using System.Collections;

public interface IProgressBar
{
    float Percentage { get; set; }

}


public class ProgressBar : MonoBehaviour, IProgressBar
{
    [Header("------Progress Bar Vars-----")]
    [SerializeField]protected tk2dBaseSprite bg;
    [SerializeField]protected tk2dSlicedSprite fillerSliced;
    [SerializeField]protected tk2dTiledSprite fillerTiled;
    [SerializeField]protected tk2dClippedSprite fillerClipped;
    [SerializeField]private float minFillerLength = 0;
    [Header("fullSize, leftBorder, rightBorder not used for clipped sprites")]
    [SerializeField]protected float fullSize;//Без учета скейла - как dimensions на спрайте
    [SerializeField]protected float leftGradientBorder = 0; //Часть спрайта которая не участвует в определении размера области заполнения (Например градиентная часть бара)...
    [SerializeField]protected float rightGradientBorder = 0;//...БЕЗ УЧЕТА СКЕЙЛА (Нужно умножить на скейл при рассчетах).
    [SerializeField]protected float rightBorder = 0;//см. скрин в начале файла
    [SerializeField]bool isVertical = false;

    [SerializeField]private float percentage;
    /// <summary>
    /// Область которая будет умножаться на percentage
    /// = fullSize - leftBorder - rightBorder
    /// </summary>
    protected float clearSize = 0;//С учетом скейла (определяется в инициализации)
    protected float fullSizeMinusRightBorder = 0;//Максимальная координата для дельта дельта спрайта
    private bool isInited = false;
    protected tk2dBaseSprite usedFillerSprite = null;
    private Renderer fillerRenderer = null;

    public float Percentage
    {
        get { return percentage; }
        set
        {
            if (!isInited)
                Init();
            if (Mathf.Approximately(percentage, value))
                return;
            percentage = Mathf.Clamp01(value);
            Refill();
        }
    }

    protected virtual void Start()
    {
        Init();
    }

    protected virtual void Init()
    {
        if (isInited)
            return;

        if (fillerTiled)
            usedFillerSprite = fillerTiled;
        else if (fillerClipped)
            usedFillerSprite = fillerClipped;
        else if (fillerSliced)
            usedFillerSprite = fillerSliced;

        fillerRenderer = usedFillerSprite.GetComponent<Renderer>();
        percentage = 0;
        fullSize = fullSize * usedFillerSprite.scale.x;
        clearSize = fullSize - usedFillerSprite.scale.x * (leftGradientBorder + rightGradientBorder);
        fullSizeMinusRightBorder = fullSize - usedFillerSprite.scale.x * rightGradientBorder;
        Refill();

        isInited = true;
    }

    protected float FillerWidth { get { return fillerRenderer.bounds.size.x; } }

    protected float FillerEdge { get { return usedFillerSprite.transform.localPosition.x + FillerWidth; } }

    protected float FillerEdgeCorrected { get { return FillerEdge - usedFillerSprite.scale.x * rightGradientBorder; } }

    private void Refill()
    {
        if (HelpTools.Approximately(percentage, 0))
        {
            if (usedFillerSprite.gameObject.activeSelf)
                usedFillerSprite.gameObject.SetActive(false);
        }
        else
        {
            if (!usedFillerSprite.gameObject.activeSelf)
                usedFillerSprite.gameObject.SetActive(true);

            if (fillerClipped)
            {
                if(isVertical)
                    fillerClipped.ClipRect = new Rect(fillerClipped.ClipRect.x, fillerClipped.ClipRect.y, fillerClipped.ClipRect.width, percentage);
                else
                    fillerClipped.ClipRect = new Rect(fillerClipped.ClipRect.x, fillerClipped.ClipRect.y, percentage, fillerClipped.ClipRect.height);
            }
            else//sliced sprite & tiled sprite
            {
                float val = (clearSize * percentage) / usedFillerSprite.scale.x + leftGradientBorder + rightGradientBorder;
                val = Mathf.Clamp(val, minFillerLength, val);
                if(isVertical)
                    fillerSliced.dimensions = new Vector2(fillerSliced.dimensions.x, val);
                else
                    fillerSliced.dimensions = new Vector2(val, fillerSliced.dimensions.y);
            }
        }
    }

    public Color BarColor
    {
        set { usedFillerSprite.color = value; }
    }

    public string BarSprite
    {
        set
        {
            if (!string.IsNullOrEmpty(value))
                usedFillerSprite.SetSprite(value);
        }
    }

    public Color BGColor
    {
        set { bg.color = value; }
    }
}
