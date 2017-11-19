using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class Module_GunSightAR : InterfaceModuleBase, IGunSight
{
    public enum States
    {
        Static,
        Target,
    }

    [SerializeField] GameObject gunsightWrapper;


    private States state = States.Static;
    private States State
    {
        get { return state; }
        set
        {
            state = value;
        }
    }

    private bool wasGunSightEnabledForTutorialLesson = false;

    protected override void Awake()
    {
        base.Awake();
        Dispatcher.Subscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Dispatcher.Subscribe(EventId.BattleEnd, OnBattleEnd);
        Dispatcher.Subscribe(EventId.TargetAimed, OnTargetAimed);
        Dispatcher.Subscribe(EventId.TankKilled, OnTankKilled);
        State = States.Static;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Dispatcher.Unsubscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Dispatcher.Unsubscribe(EventId.BattleEnd, OnBattleEnd);
        Dispatcher.Unsubscribe(EventId.TargetAimed, OnTargetAimed);
        Dispatcher.Unsubscribe(EventId.TankKilled, OnTankKilled);
    }

    private void OnMainTankAppeared(EventId id, EventInfo info)
    {
        if (ProfileInfo.IsBattleTutorialCompleted)
            Show();
    }

    private void OnBattleEnd(EventId id, EventInfo info)
    {
        Hide();
    }

    private Vector3 Move(Vector3 position)
    {
        position = Camera.main.WorldToViewportPoint(position);
        position.z = 0;
        position = GameData.CurSceneGuiCamera.ViewportToWorldPoint(position);
        return position;
    }

    public void ShowTargetGunSight(Vector3 position, float distance)
    {
        //Debug.LogError("ShowTargetGunSight");
        gunsightWrapper.SetActive(true);

        position = Move(position);

        gunsightWrapper.transform.position
            = Vector3.SqrMagnitude(position - gunsightWrapper.transform.position) > 40000f
                ? position
                : Vector3.Lerp(gunsightWrapper.transform.position, position, 0.1f);
    }

    public void ShowStaticGunSight(Vector3 position)
    {
        if (State == States.Static)
        {
            //Debug.LogError("ShowStaticGunSight");
            gunsightWrapper.SetActive(true);
            gunsightWrapper.transform.position = Move(position);
        }
    }

    public void HideTargetGunSight()
    {
        gunsightWrapper.SetActive(false);
        //Debug.LogError("HideTargetGunSight()");
    }

    public void HideStaticGunSight()
    {
        //gunsightWrapper.SetActive(false);
    }

    private void OnTargetAimed(EventId id, EventInfo ei)
    {
        EventInfo_IIB info = (EventInfo_IIB)ei;

        if (((EventInfo_IIB)ei).int1 != BattleController.MyPlayerId)
            return;

        State = info.bool1 ? States.Target : States.Static;
    }

    private void OnTankKilled(EventId id, EventInfo ei)
    {
        var info = (EventInfo_III)ei;

        if (info.int1 != BattleController.MyPlayerId)//victim
            return;

        gunsightWrapper.SetActive(false);
    }

    private void Update()
    {
        if (!BattleController.MyVehicle)
            return;

        //More correct is to rise event when fire lesson appears
        if (!ProfileInfo.IsBattleTutorialCompleted && !wasGunSightEnabledForTutorialLesson && BattleController.MyVehicle.PrimaryFireIsOn)
        {
            wasGunSightEnabledForTutorialLesson = true;
            Show();
        }
    }

    // Not Used
    public IProgressBar TargetLockedProgressBar { get { return null; } }
}

