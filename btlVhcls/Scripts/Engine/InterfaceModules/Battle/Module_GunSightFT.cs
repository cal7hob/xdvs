using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class Module_GunSightFT : InterfaceModuleBase, IGunSight
{
    public enum States
    {
        Standart,
        Zoomed,
    }

    [SerializeField] tk2dBaseSprite targetGunSight;
    [SerializeField] private float minGunsightScale = 0.8f;
    [SerializeField] private float maxGunsightScale = 1f;

    private States state = States.Standart;
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
        //Dispatcher.Subscribe(EventId.TargetAimed, OnTargetAimed);
        Dispatcher.Subscribe(EventId.TankKilled, OnTankKilled);
        State = States.Standart;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Dispatcher.Unsubscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Dispatcher.Unsubscribe(EventId.BattleEnd, OnBattleEnd);
        //Dispatcher.Unsubscribe(EventId.TargetAimed, OnTargetAimed);
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
        targetGunSight.gameObject.SetActive(true);

        //if (State == States.Standart)
        //{
            float koef = Mathf.Clamp(25f / distance, minGunsightScale, maxGunsightScale);
            float koef2 = 2f * koef;
            targetGunSight.scale = new Vector3(koef2, koef2, 1);
        //}

        position = Move(position);

        targetGunSight.gameObject.transform.position
            = Vector3.SqrMagnitude(position - targetGunSight.transform.position) > 40000f
                ? position
                : Vector3.Lerp(targetGunSight.transform.position, position, 0.1f);
    }

    public void ShowStaticGunSight(Vector3 position)
    {
        //if (State == States.Static)
        //{
        //    //Debug.LogError("ShowStaticGunSight");
        //    gunsightWrapper.SetActive(true);
        //    gunsightWrapper.transform.position = Move(position);
        //}
    }

    public void HideTargetGunSight()
    {
        targetGunSight.gameObject.SetActive(false);
        //Debug.LogError("HideTargetGunSight()");
    }

    public void HideStaticGunSight()
    {
        //gunsightWrapper.SetActive(false);
    }

    private void OnTankKilled(EventId id, EventInfo ei)
    {
        var info = (EventInfo_III)ei;

        if (info.int1 != BattleController.MyPlayerId)//victim
            return;

        targetGunSight.gameObject.SetActive(false);
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

