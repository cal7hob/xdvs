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

    [SerializeField] private GameObject[] objectsToHideWhenStatTableAppears;//вместо firstGUIGroup
    [SerializeField] private GameObject[] objectsToHideOnExitToHangar;//вместо secondGUIGroup
    public FireButtonBase[] fireButtons;
    [Header("Двиги джойстиков при переключении управления")]
    public DummyClass[] differentControlModesJoyCoords;

    [SerializeField] private float minGunsightScale;
    [SerializeField] private float maxGunsightScale;
    [SerializeField] private BattleQuestUI battleQuestUi;
    [SerializeField] GameObject[] objectsToActivateOnAwake;
    [SerializeField] GameObject[] objectsToDeactivateOnAwake;

    private tk2dUIItem moveJoystickSpr;
    private GameObject zoomButton;
    private tk2dSprite staticGunsight;
    private tk2dTextMesh lblStatusBar;
    private ProgressBarSectored targetLockedBar; // Захват цели.
    private JoystickManager joystickManager;
    private List<GameObject> allJoysticksGroup;
    private Dictionary<GunShellInfo.ShellType, FireButtonBase> fireButtonsDict;
    private RectGunsight rectGunsight;

    public static BattleGUI Instance { get; private set; }
    public static BattleQuestUI BattleQuestUi { get { return Instance.battleQuestUi; } }
    public static bool IsTargetPlatformForShowingJoysticks
    {
        get {
#if UNITY_EDITOR
            return true;
#else
            return Input.touchSupported && !SocialSettings.IsWebPlatform;
#endif
        }
    }

    public static bool SomeWindowOnScreen
    {
        get { return StatTable.instance.isActiveAndEnabled || BattleSettings.OnScreen; }
    }

    public static Dictionary<GunShellInfo.ShellType, FireButtonBase> FireButtons
    {
        get
        {
            if (Instance.fireButtonsDict != null)
                return Instance.fireButtonsDict;

            Instance.fireButtonsDict = new Dictionary<GunShellInfo.ShellType, FireButtonBase>();

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

    public tk2dSprite StaticGunsight
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
        zoomButton = GUIController.CheckReferentObject(transform, "tk2dCamera/Anchor (LowerLeft)/Controls/Zoom/ZoomWrapper", zoomButton);
        rectGunsight = transform.Find("tk2dCamera/Anchor (MiddleCenter)/GunSight2D").GetComponent<RectGunsight>();
        lblStatusBar = GUIController.CheckReferentObject(transform, "tk2dCamera/Anchor (LowerCenter)/lblStatusBar", lblStatusBar);
        staticGunsight = GUIController.CheckReferentObject(transform, "tk2dCamera/Anchor (LowerLeft)/StaticGunsight2D", staticGunsight, false);

        if (GameData.IsGame(Game.BattleOfWarplanes))
            targetLockedBar = GUIController.CheckReferentObject(transform, "tk2dCamera/Anchor (MiddleCenter)/GunSight2D/TargetLockedBar", targetLockedBar);

        allJoysticksGroup = new List<GameObject> { zoomButton == null ? null : zoomButton };

        foreach (var joystick in joystickManager.Items)
            allJoysticksGroup.Add(joystick.gameObject);

        foreach (var fireButton in FireButtons)
            allJoysticksGroup.Add(fireButton.Value.gameObject);

        Messenger.Subscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Messenger.Subscribe(EventId.OnExitToHangar, OnExitToHangar);
        Messenger.Subscribe(EventId.StatTableVisibilityChange, OnStatTableChangeVisibility);
        Messenger.Subscribe(EventId.SettingsSubmited, OnSettingsSubmitted);
        Messenger.Subscribe(EventId.BeforeReconnecting, OnReconnect);
        Messenger.Subscribe(EventId.BattleEnd, OnBattleEnd);
    }

    void OnDestroy()
    {
        Messenger.Unsubscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Messenger.Unsubscribe(EventId.OnExitToHangar, OnExitToHangar);
        Messenger.Unsubscribe(EventId.StatTableVisibilityChange, OnStatTableChangeVisibility);
        Messenger.Unsubscribe(EventId.SettingsSubmited, OnSettingsSubmitted);
        Messenger.Unsubscribe(EventId.BeforeReconnecting, OnReconnect);
        Messenger.Unsubscribe(EventId.BattleEnd, OnBattleEnd);

        Instance = null;
    }

    void Start()
    {
        //DT3.LogWarning("gunSight2D.GO.SetActive(false); GO = {0}", gunSight2D.GO.name);

        if (rectGunsight == null)
            Debug.LogError("BattleGUI.Start() rectGunsight == null");

        if (rectGunsight != null)
            rectGunsight.Hide();

        lblStatusBar.text = Localizer.GetText("ConnectionTry");

        MiscTools.SetObjectsActivity(objectsToHideWhenStatTableAppears, false);
        MiscTools.SetObjectsActivity(objectsToHideOnExitToHangar, false);


        SwitchControls();
        CheckIfHideJoysticksNeed();

        XdevsSplashScreen.SetActive(false);
        XdevsSplashScreen.SetActiveWaitingIndicator(false);
    }

    public static void HideGunSight()
    {
        if (!Instance || Instance.rectGunsight == null)
            return;

        Instance.rectGunsight.Hide();
    }

    public static void ShowGunSightForBounds(Bounds worldBounds)
    {
        if (!Instance)
            return;

        /*        if (BattleCamera.Instance.IsZoomed)
                {
                    Instance.gunSight2D.SpriteScale = Vector3.one;
                }*/

        Instance.rectGunsight.SetBounds(worldBounds);
    }

    public static void SetStatusText(string text)
    {
        if (!Instance)
            return;

        Instance.lblStatusBar.text = !string.IsNullOrEmpty(text) ? text : "";
        Instance.lblStatusBar.gameObject.SetActive(!string.IsNullOrEmpty(text) && !StatTable.OnScreen);
    }

    private void OnMainTankAppeared(EventId id, EventInfo info)
    {
        MiscTools.SetObjectsActivity(objectsToHideWhenStatTableAppears, true);
        MiscTools.SetObjectsActivity(objectsToHideOnExitToHangar, true);

        Messenger.Send(EventId.BattleGUIIntialized, new EventInfo_SimpleEvent());
    }

    private void OnExitToHangar(EventId id, EventInfo info)
    {
        MiscTools.SetObjectsActivity(objectsToHideOnExitToHangar, false);

        XdevsSplashScreen.SetActive(true,true); // Set MobileSplash texture.
        XdevsSplashScreen.SetActiveWaitingIndicator(true);
    }

    private void OnStatTableChangeVisibility(EventId id, EventInfo info)
    {
        MiscTools.SetObjectsActivity(objectsToHideWhenStatTableAppears, !((EventInfo_B)info).bool1);
        SetStatusText(lblStatusBar.text);
    }

    private void OnSettingsSubmitted(EventId eid, EventInfo ei)
    {
        SwitchControls();
    }

    private void CheckIfHideJoysticksNeed()
    {
        if (IsTargetPlatformForShowingJoysticks)
            return;

        foreach (var obj in allJoysticksGroup)
            obj.transform.position = obj.transform.position + new Vector3(10000, 10000, 10000);
    }

    private void SwitchControls()
    {
        if (!GameData.IsGame(Game.BattleOfHelicopters))
            return;

        joystickManager.Items[1].gameObject.SetActive(!ProfileInfo.isSliderControl);

        int number = ProfileInfo.isSliderControl ? 1 : 0;

        if (differentControlModesJoyCoords != null && differentControlModesJoyCoords.Length > number && differentControlModesJoyCoords[number] != null)
            for (int i = 0; i < differentControlModesJoyCoords[number].list.Count; i++)
                differentControlModesJoyCoords[number].list[i].SetLocalPosition();
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
