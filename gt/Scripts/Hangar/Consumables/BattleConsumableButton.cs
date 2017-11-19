using UnityEngine;

public class BattleConsumableButton : MonoBehaviour
{
    public enum ButtonState
    {
        Active,
        Reloading,
        Disabled
    }

    [SerializeField]
    private GameObject countWrapper;
    [SerializeField]
    private tk2dTextMesh lblCount;
    [SerializeField]
    private tk2dBaseSprite sprite;//специфичная для итема текстура
    [SerializeField]
    private ActivatedUpDownButton activatedScript;
    [SerializeField]
    private ProgressBar bar;

    private int id = -1;
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
                    break;
                case ButtonState.Reloading:
                    activatedScript.Activated = false;
                    countWrapper.SetActive(true);
                    break;
                case ButtonState.Disabled:
                    activatedScript.Activated = false;
                    bar.gameObject.SetActive(false);
                    countWrapper.SetActive(false);
                    break;
            }
        }
    }

    public bool IsEmpty { get { return id < 0; } }

    public void Init(int _id)
    {
        if (_id < 0)
        {
            activatedScript.Activated = false;
            state = ButtonState.Disabled;
            lblCount.gameObject.SetActive(false);
            sprite.gameObject.SetActive(false);
            bar.gameObject.SetActive(false);
            return;
        }

        id = _id;
        data = BattleController.MyVehicle.GetConsumable(id);
        lblCount.text = data.Amount.ToString();
        sprite.SetSprite(data.consumableInfo.icon);
        bar.Percentage = data.ReloadProgress;
        State = data.ReadyForUse ? ButtonState.Active : ButtonState.Reloading;
    }

    private void OnClick(tk2dUIItem btn)
    {
        if (BattleController.MyVehicle == null || IsEmpty || State != ButtonState.Active)
            return;
        bool result = BattleController.MyVehicle.UseConsumable(id);
        if (result)
        {
            state = ButtonState.Reloading;
            activatedScript.Activated = false;
            lblCount.text = data.Amount.ToString();
        }
    }

    private void Update()
    {
        if (state == ButtonState.Disabled || state == ButtonState.Active || (data.Amount == 0))
            return;

        bar.Percentage = data.ReloadProgress;
        if (data.ReloadProgress >= 1)
        {
            State = data.Amount > 0 ? ButtonState.Active : ButtonState.Disabled;
            if (State == ButtonState.Active)
                bar.Percentage = 0;
        }
    }
}

