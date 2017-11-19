using UnityEngine;
using System.Collections;

public class ProgressBarSectored : MonoBehaviour, IProgressBar
{
    public enum BarState
    {
        Disabled,
        InProgress,
        FullProgress
    }

    public BarState State
    {
        get
        {
            return state;
        }
        set
        {
            if (state == value)
                return;

            state = value;

            VisualizeState();
        }
    }

    [SerializeField] private tk2dBaseSprite[] sectors;
    [SerializeField] private tk2dBaseSprite externalCircle;
    [SerializeField] private  float rechargeTime;
    [Range(0.00001f,1)] // Защита от деления на ноль.
    [SerializeField] private float alphaStep;
    [SerializeField] private float percentage;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color targetLockedColor = Color.white;
    [SerializeField] private float targetLockedColorStartAlpha = 1;

    // Для анимации.
    [SerializeField] private float animationSpeed = 3f;
    [SerializeField] private float animationMinAlpha = 0.2f;
    [SerializeField] private float animationMaxAlpha = 1f;

    private const float BLINKING_FREQUENCY = 0.4f;

    private bool isInited = false;
    private int sign = 1;
    private int curStep = 0;
    private float animationCurAlpha = 0;
    private float stepVal = 0;
    private float alpha = 0;
    private BarState state = BarState.Disabled;
    private BarState newState = BarState.Disabled;
    private IEnumerator blinkingRoutine;

    public float Percentage
    {
        get
        {
            return percentage;
        }
        set
        {
            if (!isInited)
                Init();

            if (Mathf.Approximately(percentage, value))
                return;

            percentage = Mathf.Clamp01(value);

            Refill();
            
            //DT3.LogWarning("Percentage = {0}", percentage);
        }
    }

    protected void Awake()
    {
        Dispatcher.Subscribe(EventId.SACLOSAimed, OnSACLOSAimed);
    }

    protected void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.SACLOSAimed, OnSACLOSAimed);
    }

    protected virtual void Start()
    {
        Init();
    }

    protected void FixedUpdate()
    {
        if (!isInited)
            return;

        if (Percentage > 0 && Percentage < 1)
            newState = BarState.InProgress;
        else if (Mathf.Approximately(Percentage, 1))
            newState = BarState.FullProgress;
        else
            newState = BarState.Disabled;

        if (state != newState)
        {
            state = newState;
            VisualizeState();
        }

        // Если цель захвачена - выполняем анимацию готовности выпуска ракеты.
        if (state == BarState.FullProgress)
        {
            animationCurAlpha += Time.fixedDeltaTime * animationSpeed * sign;

            ChangeSectorsAlpha(animationCurAlpha);

            if (sign > 0 && animationCurAlpha >= animationMaxAlpha)
                sign = -1;

            else if (sign < 0 && animationCurAlpha <= animationMinAlpha)
                sign = 1;
        }
    }

    public void OnHideGunSight()
    {
        State = BarState.Disabled;
        Percentage = 0;
    }

    protected virtual void Init()
    {
        if (isInited)
            return;

        percentage = 0;

        stepVal = 1f / (float)(sectors.Length/* + (1f / alphaStep - 1f)*/);

        isInited = true;

        VisualizeState(); // Обязательно после присваивания IsInited = true.

        Refill();
    }

    private void OnSACLOSAimed(EventId id, EventInfo ei)
    {
        EventInfo_IB info = (EventInfo_IB)ei;

        int playerId = info.int1;
        bool aimed = info.bool1;

        VehicleController vehicleController;

        if (!BattleController.allVehicles.TryGetValue(playerId, out vehicleController) || !vehicleController.IsMain)
            return;

        if (blinkingRoutine != null)
        {
            StopCoroutine(blinkingRoutine);

            ChangeSectorsAlpha(0);
            ChangeCircleAlpha(1);
        }

        if (!aimed)
            return;

        blinkingRoutine = Blinking();

        StartCoroutine(blinkingRoutine);
    }

    private IEnumerator Blinking()
    {
        while (true)
        {
            ChangeSectorsAlpha(0);
            ChangeCircleAlpha(0);
            yield return new WaitForSeconds(BLINKING_FREQUENCY);

            ChangeSectorsAlpha(1);
            ChangeCircleAlpha(1);
            yield return new WaitForSeconds(BLINKING_FREQUENCY);
        }
    }

    private void Refill()
    {
        curStep = Mathf.FloorToInt(Percentage / stepVal);

        //for(int i = 0; i < sectors.Length; i++)//Способ с постепенным заполнением альфы
        //{
        //    alpha = alphaStep * (curStep - i);
        //
        //    alpha = Mathf.Clamp01(alpha);
        //
        //    //if (!HelpTools.Approximately( alpha, sectors[i].color.a))
        //        sectors[i].color = new Color(sectors[i].color.r, sectors[i].color.g, sectors[i].color.b, alpha);
        //}

        string s = string.Empty;

        for (int i = 0; i < sectors.Length; i++)
        {
            alpha = curStep > i ? 1 : 0;

            s += alpha + " ";

            sectors[i].color
                = new Color(
                    r:  sectors[i].color.r,
                    g:  sectors[i].color.g, 
                    b:  sectors[i].color.b,
                    a:  alpha);
        }

        //DT3.LogWarning(s);
    }

    private void VisualizeState()
    {
        switch (state)
        {
            case BarState.Disabled:
                externalCircle.SetSprite("sight auto");
                ChangeSectorsColor(normalColor);
                ChangeSectorsAlpha(0);
                Percentage = 0;
                break;
            case BarState.InProgress:
                externalCircle.SetSprite("sight auto");
                ChangeSectorsColor(normalColor);
                break;
            case BarState.FullProgress:
                externalCircle.SetSprite("sight auto red");
                animationCurAlpha = targetLockedColorStartAlpha;
                ChangeSectorsColor(targetLockedColor);
                ChangeSectorsAlpha(targetLockedColorStartAlpha);
                break;
        }

        //DT3.LogWarning("set state {0}", state);
    }

    private void ChangeSectorsAlpha(float alpha)
    {
        for (int i = 0; i < sectors.Length; i++)
            sectors[i].color
                = new Color(
                    r:  sectors[i].color.r,
                    g:  sectors[i].color.g,
                    b:  sectors[i].color.b,
                    a:  alpha);
    }

    private void ChangeSectorsColor(Color color)
    {
        for (int i = 0; i < sectors.Length; i++)
            sectors[i].color = color;
    }

    private void ChangeCircleAlpha(float alpha)
    {
        externalCircle.color
            = new Color(
                r:  externalCircle.color.r,
                g:  externalCircle.color.g,
                b:  externalCircle.color.b,
                a:  alpha);
    }
}
