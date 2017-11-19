using UnityEngine;
using Rewired;

/* Расходка не может быть поднята. Покупается только в ангаре.*/

public class BattleConsumableButton : MonoBehaviour
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

    public string RewiredName { get { return string.Format("{0}{1}", data.consumableInfo.isSuperWeapon ? "Use Super Weapon" : "Use Consumable ", data.consumableInfo.isSuperWeapon ? "" : (btnIndex + 1).ToString()); } }

    protected CustomController touchController;

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
            switch(state)
            {
                case ButtonState.Active:
                    activatedScript.Activated = true;
                    if(countWrapper)
                        countWrapper.SetActive(true);
                    break;
                case ButtonState.Reloading:
                    activatedScript.Activated = false;
                    if (countWrapper)
                        countWrapper.SetActive(true);
                    break;
                case ButtonState.Disabled:
                    activatedScript.Activated = false;
                    bar.gameObject.SetActive(false);
                    if (countWrapper)
                        countWrapper.SetActive(false);
                    break;
            }
        }
    }

    public bool IsEmpty { get { return id < 0; } }

    private ButtonState ActualState { get { return data.ReadyForUse ? ButtonState.Active : ButtonState.Reloading; } }

    private bool IsStateChanged { get { return ActualState != State; } }

    public void Init(int _id, int _btnIndex)
    {
        if (_id < 0)
        {
            State = ButtonState.Disabled;
            sprite.gameObject.SetActive(false);
            return;
        }

        id = _id;
        btnIndex = _btnIndex;
        touchController = XDevs.Input.TouchController;
        data = BattleController.MyVehicle.GetConsumable(id);
        if(lblCount)
            lblCount.text = data.Amount.ToString();
        sprite.SetSprite(data.consumableInfo.icon);
        bar.Percentage = data.ReloadProgress;
        State = ActualState;
    }

    private void Update()
    {
        if (state == ButtonState.Disabled)
            return;

        if (IsStateChanged)
        {
            if (data.Amount <= 0)
            {
                State = ButtonState.Disabled;
                return;
            }

            if(lblCount)
                lblCount.text = data.Amount.ToString ();
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
        if (data == null || data.consumableInfo == null)
            return;

        if (touchController != null && !IsEmpty && activatedScript.Activated && state == ButtonState.Active)
        {
            //if (RewiredName == "Use Super Weapon")
            //    Debug.LogWarning("Use Super Weapon, uiitem.IsPressed = " + uiitem.IsPressed);
            touchController.SetButtonValue(RewiredName, uiitem.IsPressed);
        }
    }
    #endregion
}
