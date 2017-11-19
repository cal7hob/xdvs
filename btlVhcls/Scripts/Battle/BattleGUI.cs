using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;



public class BattleGUI : MonoBehaviour
{
    [Serializable]
    public class GameObjectParams
    {
        public GameObject gameObject;
        public Vector3 position;

        public void SetLocalPosition()
        {
            if (gameObject)
                gameObject.transform.localPosition = position;
        }
    }

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
    private tk2dUIItem zoomButton;
    private tk2dSprite staticGunsight;
    private SpriteGroup gunSight2D;
    private tk2dTextMesh lblStatusBar;
    private JoystickManager joystickManager;
    private List<GameObject> allJoysticksGroup;
    private Dictionary<GunShellInfo.ShellType, FireButtonBase> fireButtonsDict;

    public IGunSight iGunSight;//gunSight realized via Module_ script
    public ThrottleLevel ThrottleLevel { get; private set; }
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

    public static bool IsWindowOnScreen
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

    void Awake()
    {
        Instance = this;

        MiscTools.SetObjectsActivity(objectsToActivateOnAwake, true);
        MiscTools.SetObjectsActivity(objectsToDeactivateOnAwake, false);

        Tk2dGuiCamera = transform.Find("tk2dCamera").GetComponent<tk2dCamera>();
        GuiCamera = Tk2dGuiCamera.GetComponent<Camera>();

        iGunSight = GUIController.CheckReferentObject(transform, "tk2dCamera/GunSight", iGunSight);
        joystickManager = GUIController.CheckReferentObject(transform, "tk2dCamera/Anchor (LowerLeft)/Controls", joystickManager);
        zoomButton = GUIController.CheckReferentObject(transform, "tk2dCamera/Anchor (LowerLeft)/Controls/Zoom/sprZoom", zoomButton);
        if(!GameData.IsGame(Game.MetalForce | Game.BattleOfWarplanes | Game.WingsOfWar | Game.IronTanks | Game.Armada | Game.FutureTanks))//Для других проектов тоже нужно вынести прицел в отдельный скрипт
            gunSight2D = new SpriteGroup(transform.Find("tk2dCamera/Anchor (MiddleCenter)/GunSight2D"));
        lblStatusBar = GUIController.CheckReferentObject(transform, "tk2dCamera/Anchor (LowerCenter)/lblStatusBar", lblStatusBar);
        if (!GameData.IsGame(Game.MetalForce | Game.BattleOfWarplanes | Game.WingsOfWar | Game.IronTanks | Game.Armada | Game.FutureTanks))
            staticGunsight = GUIController.CheckReferentObject(transform, "tk2dCamera/Anchor (LowerLeft)/StaticGunsight2D", staticGunsight, false);

        if (GameData.IsGame(Game.SpaceJet | Game.BattleOfWarplanes | Game.WingsOfWar | Game.BattleOfHelicopters))
            ThrottleLevel = GUIController.CheckReferentObject(transform, "tk2dCamera/Anchor (LowerLeft)/Controls/Throttle", ThrottleLevel);

        allJoysticksGroup = new List<GameObject> { zoomButton == null ? null : zoomButton.gameObject };

        foreach (var joystick in joystickManager.joysticks)
            allJoysticksGroup.Add(joystick.gameObject);

        foreach (var fireButton in FireButtons)
            allJoysticksGroup.Add(fireButton.Value.gameObject);

        Dispatcher.Subscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Dispatcher.Subscribe(EventId.OnExitToHangar, OnExitToHangar);
        Dispatcher.Subscribe(EventId.OnStatTableChangeVisibility, OnStatTableChangeVisibility);
        Dispatcher.Subscribe(EventId.SettingsSubmited, OnSettingsSubmitted);
        Dispatcher.Subscribe(EventId.BeforeReconnecting, OnReconnect);
        Dispatcher.Subscribe(EventId.BattleEnd, OnBattleEnd);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Dispatcher.Unsubscribe(EventId.OnExitToHangar, OnExitToHangar);
        Dispatcher.Unsubscribe(EventId.OnStatTableChangeVisibility, OnStatTableChangeVisibility);
        Dispatcher.Unsubscribe(EventId.SettingsSubmited, OnSettingsSubmitted);
        Dispatcher.Unsubscribe(EventId.BeforeReconnecting, OnReconnect);
        Dispatcher.Unsubscribe(EventId.BattleEnd, OnBattleEnd);

        Instance = null;
    }

    void Start()
    {
        HideGunSight();

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
        if (Instance == null)
            return;
        if (Instance.iGunSight != null)//new system
            Instance.iGunSight.HideTargetGunSight();
        else//old system
        {
            Instance.gunSight2D.GO.transform.localPosition = Vector3.down * 10000;
        }
    }

    public static void ShowGunSightForWorld(Vector3 position, float distance)
    {
        if (!Instance)
            return;

        if(Instance.iGunSight != null)
        {
            Instance.iGunSight.ShowTargetGunSight(position, distance);
        }
        else
        {
            if (GameData.IsGame(Game.FutureTanks | Game.ToonWars | Game.SpaceJet | Game.BattleOfHelicopters))//gamesToScaleGunsight
                Instance.gunSight2D.SpriteScale = BattleCamera.Instance.IsZoomed ?
                    Vector3.one :
                    Instance.gunSight2D.SpriteScale = Vector3.one * Mathf.Clamp(25 / distance, Instance.minGunsightScale, Instance.maxGunsightScale);

            position = Camera.main.WorldToViewportPoint(position);
            position.z = 1;
            position = Instance.GuiCamera.ViewportToWorldPoint(position);
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

        joystickManager.joysticks[1].gameObject.SetActive(!ProfileInfo.isSliderControl);

        ThrottleLevel.gameObject.SetActive(ProfileInfo.isSliderControl);
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
