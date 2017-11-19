using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GUIControlSpriteGroups : MonoBehaviour {
     
	[SerializeField] private List<tk2dBaseSprite> moveJoystickSprites = new List<tk2dBaseSprite>();
    [SerializeField] private List<tk2dBaseSprite> upDownJoystickSprites = new List<tk2dBaseSprite>();
    [SerializeField] private List<tk2dBaseSprite> primaryFireBtnSprites = new List<tk2dBaseSprite>();
    [SerializeField] private List<tk2dBaseSprite> secondaryFireBntSprites = new List<tk2dBaseSprite>();
    [SerializeField] private List<tk2dBaseSprite> throttleLevelSprites = new List<tk2dBaseSprite>();
    [SerializeField] private List<tk2dBaseSprite> gunSightCircleSprites = new List<tk2dBaseSprite>();
    [SerializeField] private List<tk2dBaseSprite> zoomSprites = new List<tk2dBaseSprite>();

    public static List<tk2dBaseSprite> MoveJoystickSprites { get { return instance.moveJoystickSprites; } }
    public static List<tk2dBaseSprite> UpDownJoystickSprites { get { return instance.upDownJoystickSprites; } }
    public static List<tk2dBaseSprite> PrimaryFireBtnSprites { get { return instance.primaryFireBtnSprites; } }
    public static List<tk2dBaseSprite> SecondaryFireBntSprites { get { return instance.secondaryFireBntSprites; } }
    public static List<tk2dBaseSprite> ThrottleLevelSprites { get { return instance.throttleLevelSprites; } }
    public static List<tk2dBaseSprite> GunSightCircleSprites { get { return instance.gunSightCircleSprites; } }
    public static List<tk2dBaseSprite> ZoomSprites { get { return instance.zoomSprites; } }
    public static List<List<tk2dBaseSprite>> Everything { get; private set; }

    public static GUIControlSpriteGroups instance;

    void Awake()
    {
        instance = this;

        Everything = new List<List<tk2dBaseSprite>>
        {
            moveJoystickSprites,
            upDownJoystickSprites,
            primaryFireBtnSprites,
            secondaryFireBntSprites,
            throttleLevelSprites,
            gunSightCircleSprites,
            zoomSprites
        };

        Messenger.Subscribe(EventId.MainTankAppeared, OnMainTankAppeared);
    }

    void Start()
    {
        SetActiveSpriteGroups(gunSightCircleSprites, false);
    }

    void OnDestroy()
    {
        instance = null;

        Messenger.Unsubscribe(EventId.MainTankAppeared, OnMainTankAppeared);
    }

    public static void SetActiveSpriteGroups(IEnumerable<List<tk2dBaseSprite>> spriteGroups, bool activate)
    {
        foreach (var sprite in spriteGroups.SelectMany(spriteGroup => spriteGroup))
            sprite.gameObject.SetActive(activate);
    }

    public static void SetActiveSpriteGroups(List<tk2dBaseSprite> sprites, bool activate)
    {
        foreach (var sprite in sprites)
            sprite.gameObject.SetActive(activate);
    }

    private void OnMainTankAppeared(EventId id, EventInfo info)
    {
        if (!ProfileInfo.IsBattleTutorial)
            SetActiveSpriteGroups(gunSightCircleSprites, true);
    }
}
