using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class Module_GunSightMF : InterfaceModuleBase, IGunSight
{
    public enum States
    {
        Static,
        Target,
    }

    [SerializeField] GameObject gunsightWrapper;
    [SerializeField] private tk2dBaseSprite leftBrace;
    [SerializeField] private tk2dBaseSprite rightBrace;
    [SerializeField] private tk2dBaseSprite sprCrosshair;
    [SerializeField] float additiveX = 40;

    [SerializeField] GameObject targetLockGunsightWrapper;
    [SerializeField] private tk2dSlicedSprite targetLockGunsight;
    [SerializeField] private Vector2 minTargetLockGunsightSize;

    private float initialPos = 0;
    private float maxPos = 0;
    private float curPos = 0;
    private VehicleController lockedTarget = null;
    private List<Vector3> targetBoundPoints = new List<Vector3>();

    private States state = States.Static;
    private States State
    {
        get { return state; }
        set
        {
            state = value;
            leftBrace.SetSprite("gunsight_frame_" + state.ToString().ToLower());
            rightBrace.SetSprite("gunsight_frame_" + state.ToString().ToLower());
            sprCrosshair.SetSprite("gunsight_crosshair_" + state.ToString().ToLower());
        }
    }

    private bool wasGunSightEnabledForTutorialLesson = false;

    protected override void Awake()
    {
        base.Awake();
        Dispatcher.Subscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Dispatcher.Subscribe(EventId.BattleEnd, OnBattleEnd);
        Dispatcher.Subscribe(EventId.TargetAimed, OnTargetAimed);
        Dispatcher.Subscribe(EventId.TargetLockChanged, OnTargetLockChanged);
        initialPos = Mathf.Abs(leftBrace.transform.localPosition.x);
        maxPos = initialPos + additiveX;
        State = States.Static;//hide target lines 
        targetLockGunsight.gameObject.SetActive(false);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Dispatcher.Unsubscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Dispatcher.Unsubscribe(EventId.BattleEnd, OnBattleEnd);
        Dispatcher.Unsubscribe(EventId.TargetAimed, OnTargetAimed);
        Dispatcher.Unsubscribe(EventId.TargetLockChanged, OnTargetLockChanged);
    }

    private void OnMainTankAppeared(EventId id, EventInfo info)
    {
        if(ProfileInfo.IsBattleTutorialCompleted)
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
        if(State == States.Static)
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

    /// <summary>
    /// Такого метода почему то нет в вехикл контроллерах
    /// </summary>
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

    private void Update()
    {
        if (!BattleController.MyVehicle)
            return;

        if(!HelpTools.Approximately(BattleController.MyVehicle.WeaponReloadingProgress, 1, 0.0001f))
        {
            curPos = Mathf.Lerp(maxPos, initialPos, BattleController.MyVehicle.WeaponReloadingProgress);
            leftBrace.transform.localPosition = new Vector3(-curPos, leftBrace.transform.localPosition.y, leftBrace.transform.localPosition.z);
            rightBrace.transform.localPosition = new Vector3(curPos, leftBrace.transform.localPosition.y, leftBrace.transform.localPosition.z);
        }

        //More correct is to rise event when fire lesson appears
        if (!ProfileInfo.IsBattleTutorialCompleted && !wasGunSightEnabledForTutorialLesson && BattleController.MyVehicle.PrimaryFireIsOn)
        {
            wasGunSightEnabledForTutorialLesson = true;
            Show();
        }

        if(lockedTarget)
        {
            targetBoundPoints.Clear();
            Bounds b = lockedTarget.GetEntireAimBounds();
            targetBoundPoints.Add(GameData.CurSceneGuiCamera.ViewportToWorldPoint(Camera.main.WorldToViewportPoint(b.center + new Vector3(-b.extents.x, b.extents.y, -b.extents.z))));
            targetBoundPoints.Add(GameData.CurSceneGuiCamera.ViewportToWorldPoint(Camera.main.WorldToViewportPoint(b.center + new Vector3(-b.extents.x, -b.extents.y, -b.extents.z))));
            targetBoundPoints.Add(GameData.CurSceneGuiCamera.ViewportToWorldPoint(Camera.main.WorldToViewportPoint(b.center + new Vector3(b.extents.x, b.extents.y, -b.extents.z))));
            targetBoundPoints.Add(GameData.CurSceneGuiCamera.ViewportToWorldPoint(Camera.main.WorldToViewportPoint(b.center + new Vector3(b.extents.x, -b.extents.y, -b.extents.z))));
            targetBoundPoints.Add(GameData.CurSceneGuiCamera.ViewportToWorldPoint(Camera.main.WorldToViewportPoint(b.center + new Vector3(-b.extents.x, b.extents.y, b.extents.z))));
            targetBoundPoints.Add(GameData.CurSceneGuiCamera.ViewportToWorldPoint(Camera.main.WorldToViewportPoint(b.center + new Vector3(-b.extents.x, -b.extents.y, b.extents.z))));
            targetBoundPoints.Add(GameData.CurSceneGuiCamera.ViewportToWorldPoint(Camera.main.WorldToViewportPoint(b.center + new Vector3(b.extents.x, b.extents.y, b.extents.z))));
            targetBoundPoints.Add(GameData.CurSceneGuiCamera.ViewportToWorldPoint(Camera.main.WorldToViewportPoint(b.center + new Vector3(b.extents.x, -b.extents.y, b.extents.z))));

            float minX = targetBoundPoints.Select(mx => mx.x).Min();
            float minY = targetBoundPoints.Select(my => my.y).Min();
            float maxX = targetBoundPoints.Select(mx => mx.x).Max();
            float maxY = targetBoundPoints.Select(my => my.y).Max();

            float x = (maxX - minX) / targetLockGunsight.scale.x;
            x = Mathf.Clamp(x, minTargetLockGunsightSize.x, x);
            float y = (maxY - minY) / targetLockGunsight.scale.y;
            y = Mathf.Clamp(y, minTargetLockGunsightSize.y, y);

            targetLockGunsight.dimensions = new Vector2(x, y);

            Vector3 center = GameData.CurSceneGuiCamera.ViewportToWorldPoint(Camera.main.WorldToViewportPoint(b.center));
            targetLockGunsightWrapper.transform.position = new Vector3(center.x, center.y, targetLockGunsight.transform.position.z);
        }
    }

    private void OnTargetLockChanged(EventId id, EventInfo ei)
    {
        EventInfo_IB info = (EventInfo_IB)ei;
        lockedTarget = info.bool1 && BattleController.allVehicles.ContainsKey(info.int1) ? BattleController.allVehicles[info.int1] : null;
        targetLockGunsight.gameObject.SetActive(lockedTarget != null);
    }

    // Not Used
    public IProgressBar TargetLockedProgressBar { get { return null; } }
}

