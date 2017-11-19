using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;



public class BattleGUI : MonoBehaviour
{
    /// <summary>
    ////Пришлось сделать такой класс потому что юнити не поддерживает сериализацию двумерных массивов
    /// </summary>
    [Serializable]
    public class DummyClass
    {
        public string description;
        public List<GameObjectParams> list;
    }

    [SerializeField]
    private GameObject[] objectsToHideWhenStatTableAppears;//вместо firstGUIGroup
    [SerializeField]
    private GameObject[] objectsToHideOnExitToHangar;//вместо secondGUIGroup
    public FireButtonBase[] fireButtons;
    [Header("Двиги джойстиков при переключении управления")]
    public DummyClass[] differentControlModesJoyCoords;

    [SerializeField]
    private float minGunsightScale;
    [SerializeField]
    private float maxGunsightScale;
    [SerializeField]
    private BattleQuestUI battleQuestUi;
    [SerializeField]
    GameObject[] objectsToActivateOnAwake;
    [SerializeField]
    GameObject[] objectsToDeactivateOnAwake;
    [SerializeField]
    private GameObject WinTeamWrapper;
    [SerializeField]
    private GameObject LooseTeamWrapper;

    private tk2dUIItem moveJoystickSpr;
    private tk2dUIItem zoomButton;
    private tk2dSlicedSprite staticGunsight;
    private SpriteGroup gunSight2D;
    private tk2dTextMesh lblStatusBar;
    private ProgressBarSectored targetLockedBar; // Захват цели.
    private JoystickManager joystickManager;
    private List<GameObject> allJoysticksGroup;
    private Dictionary<ShellType, FireButtonBase> fireButtonsDict;
    private bool isMouseControlledCamera;

    public static BattleGUI Instance { get; private set; }
    public static BattleQuestUI BattleQuestUi { get { return Instance.battleQuestUi; } }
    public static bool IsTargetPlatformForShowingJoysticks
    {
        get
        {
            //#if UNITY_EDITOR
            //            return true;
            //#endif
            return Input.touchSupported && !SocialSettings.IsWebPlatform;
        }
    }

    // Не используется, закомментил. Илья.
    //public static float GunSightAlpha
    //{
    //    get { return Instance.gunSight2D.SpriteColor.a; }
    //    set
    //    {
    //        Color color = Instance.gunSight2D.SpriteColor;
    //        color.a = value;
    //        Instance.gunSight2D.SpriteColor = color;
    //    }
    //}

    public static bool IsWindowOnScreen
    {
        get { return StatTable.instance.isActiveAndEnabled || BattleSettings.OnScreen; }
    }

    public static Dictionary<ShellType, FireButtonBase> FireButtons
    {
        get
        {
            if (Instance.fireButtonsDict != null)
                return Instance.fireButtonsDict;

            Instance.fireButtonsDict = new Dictionary<ShellType, FireButtonBase>();

            foreach (FireButtonBase fireButton in Instance.fireButtons)
                Instance.fireButtonsDict[fireButton.shellType] = fireButton;

            return Instance.fireButtonsDict;
        }
    }

    public Camera GuiCamera
    {
        get; private set;
    }

    public tk2dCamera Tk2dGuiCamera
    {
        get; private set;
    }

    public tk2dSlicedSprite StaticGunsight
    {
        get { return staticGunsight; }
    }

    public IProgressBar TargetLockedBar
    {
        get { return targetLockedBar; }
    }

    void Awake()
    {
        Instance = this;

        MiscTools.SetObjectsActivity(objectsToActivateOnAwake, true);
        MiscTools.SetObjectsActivity(objectsToDeactivateOnAwake, false);

        Tk2dGuiCamera = transform.Find("tk2dCamera").GetComponent<tk2dCamera>();
        GuiCamera = Tk2dGuiCamera.GetComponent<Camera>();

        joystickManager = GUIController.CheckReferentObject(transform, "tk2dCamera/Anchor (LowerLeft)/Controls", joystickManager);
        zoomButton = GUIController.CheckReferentObject(transform, "tk2dCamera/Anchor (LowerLeft)/Controls/Zoom/sprZoom", zoomButton);
        gunSight2D = new SpriteGroup(transform.Find("tk2dCamera/Anchor (MiddleCenter)/GunSight2D"));
        lblStatusBar = GUIController.CheckReferentObject(transform, "tk2dCamera/Anchor (LowerCenter)/lblStatusBar", lblStatusBar);
        staticGunsight = GUIController.CheckReferentObject(transform, "tk2dCamera/Anchor (LowerLeft)/StaticGunsight2D", staticGunsight, false);

        allJoysticksGroup = new List<GameObject> { zoomButton == null ? null : zoomButton.gameObject };

        foreach (var joystick in joystickManager.Items)
            allJoysticksGroup.Add(joystick.gameObject);

        foreach (var fireButton in FireButtons)
            allJoysticksGroup.Add(fireButton.Value.gameObject);

        Dispatcher.Subscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Dispatcher.Subscribe(EventId.OnExitToHangar, OnExitToHangar);
        Dispatcher.Subscribe(EventId.OnStatTableChangeVisibility, OnStatTableChangeVisibility);
        Dispatcher.Subscribe(EventId.BeforeReconnecting, OnReconnect);
        Dispatcher.Subscribe(EventId.BattleEnd, OnBattleEnd);
        Dispatcher.Subscribe(EventId.TeamWin, WinOrLooseHandler);
        Dispatcher.Subscribe(EventId.MyTankRespawned, DeactivateWinLooseWrappers);
        isMouseControlledCamera = (PlayerPrefs.GetInt("MouseControl", 0) == 1);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Dispatcher.Unsubscribe(EventId.OnExitToHangar, OnExitToHangar);
        Dispatcher.Unsubscribe(EventId.OnStatTableChangeVisibility, OnStatTableChangeVisibility);
        Dispatcher.Unsubscribe(EventId.BeforeReconnecting, OnReconnect);
        Dispatcher.Unsubscribe(EventId.BattleEnd, OnBattleEnd);
        Dispatcher.Unsubscribe(EventId.TeamWin, WinOrLooseHandler);
        Dispatcher.Unsubscribe(EventId.MyTankRespawned, DeactivateWinLooseWrappers);

        Instance = null;
    }

    void Start()
    {
        //DT3.LogWarning("gunSight2D.GO.SetActive(false); GO = {0}", gunSight2D.GO.name);

        gunSight2D.GO.SetActive(false);

        HideGunSight();

        lblStatusBar.text = Localizer.GetText("ConnectionTry");

        MiscTools.SetObjectsActivity(objectsToHideWhenStatTableAppears, false);
        MiscTools.SetObjectsActivity(objectsToHideOnExitToHangar, false);

        CheckIfHideJoysticksNeed();

        XdevsSplashScreen.SetActive(false);
        XdevsSplashScreen.SetActiveWaitingIndicator(false);
    }

    private void WinOrLooseHandler(EventId _id, EventInfo _info)
    {
        if (WinTeamWrapper.activeInHierarchy || LooseTeamWrapper.activeInHierarchy)
        {
            return;
        }
        var info = (EventInfo_I)_info;
        if (info.int1 == BattleController.MyVehicle.TeamId)
        {
            WinTeamWrapper.SetActive(true);
        }
        else
        {
            LooseTeamWrapper.SetActive(true);
        }
    }

    private void DeactivateWinLooseWrappers(EventId _id, EventInfo _info)
    {
        WinTeamWrapper.SetActive(false);
        LooseTeamWrapper.SetActive(false);
    }
    public static void HideGunSight()
    {
        //DT3.LogWarning("HideGunSight()");

        Instance.gunSight2D.GO.transform.localPosition = Vector3.down * 10000;
    }

    public static void ShowGunSightForWorld(Vector3 position, float distance, bool alwaysVisible = false)
    {
        position = Camera.main.WorldToViewportPoint(position);
        position.z = 1;

        if (BattleCamera.Instance.IsZoomed)
        {
            position.y = 0.5f;
        }

        position = Instance.GuiCamera.ViewportToWorldPoint(position);

        if (alwaysVisible)
        {
            Instance.gunSight2D.GO.transform.position = position;
        }
        else
        {
            Instance.gunSight2D.GO.transform.position
                = Vector3.SqrMagnitude(position - Instance.gunSight2D.GO.transform.position) > 40000f
                    ? position
                    : Vector3.Lerp(Instance.gunSight2D.GO.transform.position, position, 0.1f);
        }
    }

    public static void SetStatusText(string text)
    {
        Instance.lblStatusBar.text = !string.IsNullOrEmpty(text) ? text : "";
        Instance.lblStatusBar.gameObject.SetActive(!string.IsNullOrEmpty(text) && !StatTable.OnScreen);
    }

    private void OnMainTankAppeared(EventId id, EventInfo info)
    {
        MiscTools.SetObjectsActivity(objectsToHideWhenStatTableAppears, true);
        MiscTools.SetObjectsActivity(objectsToHideOnExitToHangar, true);

        Dispatcher.Send(EventId.BattleGUIIntialized, new EventInfo_SimpleEvent());
    }

    private void OnExitToHangar(EventId id, EventInfo info)
    {
        MiscTools.SetObjectsActivity(objectsToHideOnExitToHangar, false);

        XdevsSplashScreen.SetActive(true, true); // Set MobileSplash texture.
        XdevsSplashScreen.SetActiveWaitingIndicator(true);
    }

    private void OnStatTableChangeVisibility(EventId id, EventInfo info)
    {
        MiscTools.SetObjectsActivity(objectsToHideWhenStatTableAppears, !((EventInfo_B)info).bool1);
        SetStatusText(lblStatusBar.text);
    }

    private void CheckIfHideJoysticksNeed()
    {
        if (IsTargetPlatformForShowingJoysticks)
            return;

        foreach (var obj in allJoysticksGroup)
            obj.transform.position = obj.transform.position + new Vector3(10000, 10000, 10000);
    }

    private void OnReconnect(EventId id, EventInfo ei)
    {
        HideGunSight();
    }

    private void OnBattleEnd(EventId id, EventInfo ei)
    {
        HideGunSight();  //Гарантированно скрывать прицел при выходе
    }
}
