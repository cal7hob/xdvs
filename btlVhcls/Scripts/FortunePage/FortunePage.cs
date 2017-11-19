using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class FortunePage : HangarPage
{
    //!!!!! Порядок ссылок в tk2dUIToggleButtonGroup должен быть такой же как в этом енаме !!!!!
    public enum Tab
    {
        Roulett = 0,
        Award = 1,
    }

    public enum RoulettState
    {
        Stop,
        Drag,
        Rotate,
        Deceleration,//замедление
    }
    public static FortunePage Instance { get; private set; }
    public static int AttemptsLeft { get { return ProfileInfo.roulettParams["attempts"]; } }
    public static int Sector { get { return ProfileInfo.roulettParams["sector"]; } }
    public static int TimeToNextAttempt { get { return ProfileInfo.roulettParams["timeout"] - (int)GameData.CurrentTimeStamp; } }
    public static bool IsWaitingForNextAttempt { get { return !IsAllAttemptsUsed && TimeToNextAttempt > 0; } }
    public static bool CanSpinTheRoulett { get { return !IsAllAttemptsUsed && TimeToNextAttempt <= 0; } }
    public static int CurAttempt { get { return Mathf.Clamp( GameData.roulettAttemptsCount - AttemptsLeft + 1, 0, GameData.roulettAttemptsCount); } }
    public static bool IsAllAttemptsUsed { get { return AttemptsLeft <= 0; } }
    public static bool IsAwardObtained { get { return Sector == 0; } }


    [SerializeField] private Factory roulettFactory;
    [SerializeField] private tk2dUIToggleButtonGroup tabs;
    [SerializeField] private GameObject[] pages;//порядок как в енаме Tab
    [SerializeField] private tk2dSlicedSprite awardWindowSprite;//спрайт награды
    [SerializeField] private tk2dTextMesh awardWindowText;//количество награды
    [SerializeField] private ActivatedUpDownButton roulettLock;
    [SerializeField] private ActivatedUpDownButton btnTakeAward;
    [SerializeField] private tk2dTextMesh roulettLockText;
    [SerializeField] private tk2dTextMesh roulettLockTime;
    [SerializeField] private tk2dTextMesh lblUseNumber;
    [SerializeField] private tk2dUIItem swipeUIItem;
    [SerializeField] private Transform roulettSprite;
    [SerializeField] private float minSwipeDistance = 50;//Расстояние меньше которого свайп не обрабатывается
    [SerializeField] private float minStartCircularVelocity = 100;//скорость, меньше которой нельзя раскрутить рулетку. градусов в секунду
    [SerializeField] private float maxStartCircularVelocity = 1080;//скорость, больше которой нельзя раскрутить рулетку. градусов в секунду
    [SerializeField] private float stopCircularVelocity = 30;//скорость по достижении которой можно останавливать кручение. градусов в секунду
    [SerializeField] private float minRotationTime = 3;//минимальное время кручения в состоянии Rotate
    [SerializeField] private float maxDeceleration = 360;//Максимальное ускорение при замедлении :-) .На сколько быстро замедляется кручение (градусов / секунда в квадрате)


    private Vector2 startPos;  //Первая позиция касания
    private Vector2 endPos;   //Последняя позиция касания
    private float startTime;
    private float endTime;

    private float accelerationSpeed = 0;
    private float acceleration = 0;//Ускорение (а точнее замедление). Градусов / секунда в квадрате
    private float s = 0;//Расстояние до нужной ячейки (в градусах)
    private float sAccumulated = 0;
    private float sDelta = 0;//Расстояние пройденное за один кадр (в градусах)

    private bool RotatesForAMinTime { get { return (Time.realtimeSinceStartup - endTime) >= minRotationTime; } }//Говорит о том что мы достигли минимального допустимое время вращения (>= minRotationTime)
    private bool CanStartDeceleration { get { return !isWaitingServerAnswer && RotatesForAMinTime; } }
    private float SwipeDistance { get { return (endPos - startPos).magnitude; } }
    private float SwipeTime { get { return endTime - startTime; } }
    private float AnglePerPixel { get { return 360f / Screen.width; } }
    private float ClampedCircularVelocity { get { return Mathf.Approximately( SwipeTime, 0) ? minStartCircularVelocity : Mathf.Clamp(SwipeDistance * AnglePerPixel / SwipeTime, minStartCircularVelocity, maxStartCircularVelocity); } }//градусов / секунду
    
    private RoulettItem TargetItem { get { return (RoulettItem)roulettFactory.GetItemByUniqId(Sector); } }
    private float TargetItemAngle { get { return TargetItem ? TargetItem.CirclePos : 0; } }
    private bool IsNeededItemChoosed { get { return HelpTools.Approximately(TargetItem.MainTransform.rotation.eulerAngles.z, 0, 3); } }
    private float CurRoulettRotationProgress { get { return 360f - roulettSprite.localRotation.eulerAngles.z; } }//градус поворота по часовой стрелке 0 .. 360 градусов

    private Vector2 awardSpriteInitialSize;
    private bool isWaitingServerAnswer = false;
    private ButtonOpenFortunePage openButton;
    private RoulettState state = RoulettState.Stop;
    public RoulettState State
    {
        get { return state; }
        set
        {
            RoulettState prevState = state;
            state = value;
            //Debug.LogFormat("State = {0}", state);
            switch(state)
            {
                case RoulettState.Stop:
                    if(prevState == RoulettState.Deceleration)
                    {
                        roulettSprite.localRotation = Quaternion.Euler(roulettSprite.localRotation.eulerAngles.x, roulettSprite.localRotation.eulerAngles.y, TargetItem.AngleOffset);//Доворачиваем до середины
                        if (!IsAwardObtained)
                            tabs.SelectedIndex = (int)Tab.Award;
                        UpdateElements();
                    }
                    break;
                case RoulettState.Rotate:
                    SpinTheRoulett();
                    break;
                case RoulettState.Deceleration:
                    sAccumulated = 0;
                    accelerationSpeed = ClampedCircularVelocity;
                    //Определяем расстояние до целевой ячейки (в градусах)
                    s = TargetItemAngle - CurRoulettRotationProgress;
                    if (s < 0)//Если проехали целевой угол
                        s = 360f + s;//т.к. s отрицательный угол здесь, то складываем
                        
                    if (s < 0.0001f)//защита от деления на ноль + Защита от переполнения float (при определении acceleration) - может и не надо, но хуже не будет.
                        s += 360f;

                    if(maxDeceleration <= 0)
                    {
                        Debug.LogErrorFormat("Wrong parameter maxDeceleration! The Value must be > 0. Set value to default!");
                        maxDeceleration = 100f;
                    }
                    //Находим минимальное расстояние которое должна пройти рулетка при замедлении, чтобы не привысить установленный порог maxDeceleration
                    float sMin = Mathf.Abs((Mathf.Pow(stopCircularVelocity, 2) - Mathf.Pow(ClampedCircularVelocity, 2)) / (2f * maxDeceleration));
                    if (s < sMin)//Если определенное расстояние меньше чем минимально допустимое чтоб успеть остановиться с максимальным ускорением = доба
                        s += 360f * Mathf.Ceil((sMin - s) / 360f);
                    acceleration = Mathf.Abs((Mathf.Pow(stopCircularVelocity, 2) - Mathf.Pow(ClampedCircularVelocity, 2)) / (2f * s));
                    //Debug.LogWarningFormat("acceleration = {0}, s = {1}", acceleration, s);
                    break;
            }
        }
    }

    protected override void Create()
    {
        base.Create();
        Instance = this;
        roulettLockTime.text = "";
        roulettSprite.localRotation = Quaternion.identity;
        awardSpriteInitialSize = awardWindowSprite.dimensions;
        roulettLock.gameObject.SetActive(true);//Если случайно выключили в префабе
        HangarController.OnTimerTick += OnTimerTick;
        Dispatcher.Subscribe(EventId.ProfileInfoLoadedFromServer, OnProfileInfoLoadedFromServer);
    }

    protected override void Destroy()
    {
        base.Destroy();
        HangarController.OnTimerTick -= OnTimerTick;
        Dispatcher.Unsubscribe(EventId.ProfileInfoLoadedFromServer, OnProfileInfoLoadedFromServer);
        Instance = null;
    }

    /// <summary>
    /// AfterHangarInit
    /// </summary>
    protected override void Init()
    {
        base.Init();
        //Не инстанируем если еще не были в боевом туторе
        if (!ProfileInfo.IsBattleTutorialCompleted)
            return;

        #region Инстанирование объектов
        if (GameData.roulettItems != null)
            roulettFactory.CreateAll(GameData.roulettItems);
        #endregion
    }

    private void OnClick(tk2dUIItem btn)
    {
        switch(btn.name)
        {
            case "btnOk":
                if(State == RoulettState.Stop)//Выходим только на остановившейся рулетке
                    GUIPager.Back();
                break;
            case "btnTakeRoulettAward": TakeAward(); break;
        }
    }

    protected override void Show()
    {
        base.Show();
        State = RoulettState.Stop;
        UpdateElements();
        //Выбор вкладки в зависимости от того дали нам награду или нет
        tabs.SelectedIndex = IsAwardObtained ? (int)Tab.Roulett : (int)Tab.Award;
    }

    public void SpinTheRoulett()
    {
        if (isWaitingServerAnswer)
            return;

        isWaitingServerAnswer = true;

        var request = Http.Manager.Instance().CreateRequest("/gamble/wheel/roll");
        //Debug.LogWarningFormat("SpinTheRoulettRequest!");
        Http.Manager.StartAsyncRequest(
            request: request,
            successCallback: delegate (Http.Response result)
            {
                if (GameData.IsHangarScene)//На случай если ответ получим уже в бою
                {
                    isWaitingServerAnswer = false;

                    Dispatcher.Send(EventId.FortuneWheelRolled, new EventInfo_I((int)TargetItem.Data.entity.type));

#if UNITY_EDITOR
                    Debug.LogFormat("AWARD: sector {0}, type = {1}, id = {2} text = {3}, sprite = {4}", TargetItem.Data.sector, TargetItem.Data.entity, TargetItem.Data.entity.id, TargetItem.Data.entity.Text, TargetItem.Data.entity.GetSprite(false));
#endif
                }
            },
            failCallback: delegate (Http.Response result)
            {
                if (GameData.IsHangarScene)//На случай если ответ получим уже в бою
                {
                    isWaitingServerAnswer = false;
                    //Debug.LogFormat("Server failCallback");
                    roulettSprite.localRotation = Quaternion.identity;
                    State = RoulettState.Stop;
                }
            });
    }

    public void TakeAward()
    {
        if (isWaitingServerAnswer)
            return;

        isWaitingServerAnswer = true;
        btnTakeAward.Activated = false;
        var request = Http.Manager.Instance().CreateRequest("/gamble/wheel/reward");
        //Debug.LogWarningFormat("TakeAwardRequest!");
        Http.Manager.StartAsyncRequest(
            request: request,
            successCallback: delegate (Http.Response result)
            {
                if (GameData.IsHangarScene)//На случай если ответ получим уже в бою
                {
                    isWaitingServerAnswer = false;
                    //Debug.LogFormat("TakeAward SUCCESS");
                    btnTakeAward.Activated = true;
                    tabs.SelectedIndex = (int)Tab.Roulett;
                }
            },
            failCallback: delegate (Http.Response result)
            {
                if (GameData.IsHangarScene)//На случай если ответ получим уже в бою
                {
                    isWaitingServerAnswer = false;
                    btnTakeAward.Activated = true;
                    //Debug.LogFormat("TakeAward failCallback");
                }
            });
    }

    public void OnTabChanged(tk2dUIToggleButtonGroup buttonGroup)
    {
        for (int i = 0; i < pages.Length; i++)
            pages[i].SetActive(tabs.SelectedIndex == i);

        if(tabs.SelectedIndex == (int)Tab.Award && !IsAwardObtained)
        {
            btnTakeAward.Activated = true;
            awardWindowSprite.SetSprite(AtlasesManager.GetAtlasDataByEntity(TargetItem.Data.entity.type), TargetItem.Data.entity.GetSprite(TargetItem.UseConsumableSpriteWithFrame));
            MiscTools.ResizeSlicedSpriteAccordingToTextureProportions(awardWindowSprite, awardSpriteInitialSize);
            awardWindowText.text = TargetItem.Data.entity.Text;
        }
    }

    private void OnDown(tk2dUIItem btn)
    {
        if (State != RoulettState.Stop || roulettLock.Activated || tabs.SelectedIndex != (int)Tab.Roulett)
            return;
        //Debug.LogErrorFormat("OnDown {0}", btn.Touch.position.ToString());
        startPos = btn.Touch.position;
        startTime = Time.realtimeSinceStartup;
        State = RoulettState.Drag;
    }

    private void OnUp(tk2dUIItem btn)
    {
        if (State != RoulettState.Drag || roulettLock.Activated || tabs.SelectedIndex != (int)Tab.Roulett)
            return;
        //Debug.LogErrorFormat("OnUp {0}", btn.Touch.position.ToString());
        endPos = btn.Touch.position;
        endTime = Time.realtimeSinceStartup;
        
        State = SwipeDistance < minSwipeDistance ? RoulettState.Stop : RoulettState.Rotate;
    }

    private void Update()
    {
        if (State == RoulettState.Drag)//раскручиваем рулетку
        {
            endPos = swipeUIItem.Touch.position;
            RotateRoulett(SwipeDistance * AnglePerPixel);
        }
        else if (State == RoulettState.Rotate)
        {
            if(!CanStartDeceleration)
                RotateRoulett((ClampedCircularVelocity * Time.deltaTime), true);//крутим на максимальной скорости
            else
                State = RoulettState.Deceleration;//начинаем замедление
        }
        else if(State == RoulettState.Deceleration)
        {
           accelerationSpeed -= acceleration * Time.deltaTime;
            if (accelerationSpeed <= stopCircularVelocity)
            {
                State = RoulettState.Stop;
                return;
            }
            sDelta = accelerationSpeed * Time.deltaTime;
            sAccumulated += sDelta;
            if (sAccumulated >= s)
            {
                State = RoulettState.Stop;
                return;
            }

            //Debug.LogFormat("accelerationSpeed = {0}, s delta = {1}, full s = {2}", accelerationSpeed, sDelta, sAccumulated);
            RotateRoulett(sDelta, true);
        }
    }

    private void RotateRoulett(float angle, bool addToCurrentAngle = false)
    {
        roulettSprite.localRotation = Quaternion.Euler(
                roulettSprite.localRotation.eulerAngles.x,
                roulettSprite.localRotation.eulerAngles.y,
                ((addToCurrentAngle ? roulettSprite.localRotation.eulerAngles.z : 0f) - angle));
    }

    private void OnTimerTick(double time)
    {
        UpdateElements();
    }

    private void OnProfileInfoLoadedFromServer(EventId id, EventInfo info)
    {
        UpdateElements();
    }

    private void UpdateElements()
    {
        if (!IsVisible || State != RoulettState.Stop)
            return;
        lblUseNumber.text = !IsAllAttemptsUsed ? string.Format("{0} / {1}", CurAttempt, GameData.roulettAttemptsCount) : "";
        roulettLock.Activated = (IsAllAttemptsUsed || IsWaitingForNextAttempt) && State == RoulettState.Stop;
        if (roulettLock.Activated)
        {
            roulettLockText.text = Localizer.GetText(IsAllAttemptsUsed ? "lblFortunePageAllAttemptsUsed" : "lblFortunePageLockedText");
            roulettLockTime.text = IsAllAttemptsUsed ? "" : Clock.GetTimerString(TimeToNextAttempt);
        }
    }
}
