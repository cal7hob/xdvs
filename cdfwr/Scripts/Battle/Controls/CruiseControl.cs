using UnityEngine;

public class CruiseControl : MonoBehaviour
{
    public abstract class CruiseControlState
    {
        public abstract float YAxisControl();
    }

    public class CruiseControlOffState : CruiseControlState
    {
        public override float YAxisControl()
        {
            return XDevs.Input.GetAxis("Move Forward/Backward");
        }
    }

    public class CruiseControlledState : CruiseControlState
    {
        public override float YAxisControl()
        {
            return 1f;
        }
    }

    [SerializeField]
    private tk2dUIToggleButton toggleBtn;
    [SerializeField]
    private GameObject wrapper;

    private VehicleController myVehicle;
    private CruiseControlState cruiseControlOffState = new CruiseControlOffState();
    private CruiseControlState cruiseControlledState = new CruiseControlledState();

    void Awake()
    {
        Dispatcher.Subscribe(EventId.MyTankRespawned, OnMyTankRespawned);
        Dispatcher.Subscribe(EventId.MainTankAppeared, Initialize);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.MyTankRespawned, OnMyTankRespawned);
        Dispatcher.Unsubscribe(EventId.MainTankAppeared, Initialize);
    }

    private void OnMyTankRespawned(EventId id, EventInfo info)
    {
        DisableCruiseControl();
    }

    private void Initialize(EventId id, EventInfo info)
    {
        myVehicle = BattleController.MyVehicle;

        DisableCruiseControl();
        wrapper.SetActive(false);//Нет круиз контроля в COW
        //wrapper.SetActive(BattleGUI.IsTargetPlatformForShowingJoysticks);
        if (ProfileInfo.IsBattleTutorial)
        {
            wrapper.SetActive(false);
        }
    }

    public void OnToggle()
    {
        if (myVehicle.CruiseControlState == cruiseControlOffState)
        {
            myVehicle.SetCruiseControlState(cruiseControlledState);
            toggleBtn.IsOn = true;
        }
        else if (myVehicle.CruiseControlState == cruiseControlledState)
        {
            DisableCruiseControl();
        }
    }

    public void DisableCruiseControl()
    {
        myVehicle.SetCruiseControlState(cruiseControlOffState);
        toggleBtn.IsOn = false;
    }

    //void Update()
    //{
    //    if (XDevs.Input.GetAxis("Move Forward/Backward") < -0.7f)
    //    {
    //        DisableCruiseControl();
    //    }
    //    if (XDevs.Input.GetButtonDown("CruiseControl"))
    //    {
    //        OnToggle();
    //    }
    //}
}
