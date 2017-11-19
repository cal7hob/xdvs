using Rewired;
using UnityEngine;

/* Расходка не может быть поднята. Покупается только в ангаре.*/

public class BattleConsumableButton : MonoBehaviour, ISwappableControl
{
    public enum ButtonState
    {
        Active,
        Reloading,
        Disabled
    }

    [SerializeField] private GameObject countWrapper;
    [SerializeField] private tk2dTextMesh lblCount;
    [SerializeField] private tk2dBaseSprite sprite;//специфичная для итема текстура
    [SerializeField] private ActivatedUpDownButton activatedScript;
    [SerializeField] private tk2dUIItem uiitem;
    [SerializeField] private ProgressBar bar;

    [SerializeField] private bool swappable;
    [SerializeField] private DeadZoneObjectChangeStateSender deadZoneObjectChangeStateSender;

    public bool Swappable
    {
        get { return swappable; }
    }

    [SerializeField] private Transform wrapperToRotate;
    [SerializeField] private tk2dUILayout layoutToSwapIn;
    [SerializeField] private Transform[] transformsToSwap;

    public string RewiredName { get { return string.Format("{0}{1}", REWIRED_NAME_BASE, btnIndex + 1); } }

    protected CustomController touchController;

    private const string REWIRED_NAME_BASE = "Use Consumable ";
    private int id = -1;
    private int btnIndex = -1;
    private BattleConsumable data;
    private ButtonState state;
    private ButtonState State
    {
        get { return state; }
        set
        {
            state = value;
            switch (state)
            {
                case ButtonState.Active:
                    activatedScript.Activated = true;
                    countWrapper.SetActive(true);
                    bar.Percentage = 0;
                    deadZoneObjectChangeStateSender.State = true;
                    break;
                case ButtonState.Reloading:
                    activatedScript.Activated = false;
                    countWrapper.SetActive(true);
                    break;
                case ButtonState.Disabled:
                    activatedScript.Activated = false;
                    bar.gameObject.SetActive(false);
                    countWrapper.SetActive(false);
                    deadZoneObjectChangeStateSender.State = false;
                    break;
            }
        }
    }

    public bool IsEmpty { get { return id < 0; } }

    private ButtonState ActualState { get { return data.ReadyForUse ? ButtonState.Active : ButtonState.Reloading; } }

    private bool IsStateChanged { get { return ActualState != State; } }

    public void Init(int _id, int _btnIndex)
    {
        id = _id;
        data = BattleController.MyVehicle.GetConsumable(id);

        if (id < 0 || data == null || data.Amount <= 0)
        {
            State = ButtonState.Disabled;
            sprite.gameObject.SetActive(data != null);
            return;
        }

        btnIndex = _btnIndex;
        touchController = XDevs.Input.TouchController;
        lblCount.text = data.Amount.ToString();
        sprite.SetSprite(data.consumableInfo.icon);
        bar.Percentage = data.ReloadProgress;
        State = ActualState;
    }

    private void Update()
    {
        if (state == ButtonState.Disabled)
        {
            if (deadZoneObjectChangeStateSender.State != false)
                deadZoneObjectChangeStateSender.State = false;

            return;
        }

        if (IsStateChanged)
        {
            if (data.Amount <= 0)
            {
                State = ButtonState.Disabled;
                return;
            }

            lblCount.text = data.Amount.ToString();
            State = ActualState;

            if (State == ButtonState.Active)
                bar.Percentage = 0;
        }

        if (State == ButtonState.Reloading)
            bar.Percentage = data.ReloadProgress;
    }

    #region Использование расходки по кнопкам 1-3

    private void OnEnable()
    {
        ReInput.InputSourceUpdateEvent += ReInput_InputSourceUpdateEvent;
    }

    private void OnDisable()
    {
        ReInput.InputSourceUpdateEvent -= ReInput_InputSourceUpdateEvent;
    }

    private void ReInput_InputSourceUpdateEvent()
    {
        if (touchController != null && !IsEmpty && activatedScript.Activated && state == ButtonState.Active)
            touchController.SetButtonValue(RewiredName, uiitem.IsPressed);
    }

    #endregion

    public void Swap()
    {
        if (layoutToSwapIn != null && transformsToSwap != null)
        {
            foreach (var transformToSwap in transformsToSwap)
            {
                // Опорная точка (pivot) У tk2dUILayout всегда в левом верхнем углу

                var layoutToSwapInWidth = (layoutToSwapIn.GetMaxBounds() - layoutToSwapIn.GetMinBounds()).x;
                var deltaCenter = layoutToSwapInWidth / 2 - transformToSwap.localPosition.x;

                // Считаем, что у transformToSwap опорная точка (pivot) в центре
                transformToSwap.localPosition += new Vector3(deltaCenter * 2, 0, 0);
            }
        }

        if (wrapperToRotate == null)
            return;

        wrapperToRotate.RotateAround(wrapperToRotate.position, Vector3.up, 180f);
    }
}
