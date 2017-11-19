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
    [SerializeField] private List<tk2dBaseSprite> consumablesSprites = new List<tk2dBaseSprite>();
    [SerializeField] private List<tk2dTextMesh>   consumablesTexts = new List<tk2dTextMesh>();

    public static List<tk2dBaseSprite> MoveJoystickSprites { get { return instance.moveJoystickSprites; } }
    public static List<tk2dBaseSprite> UpDownJoystickSprites { get { return instance.upDownJoystickSprites; } }
    public static List<tk2dBaseSprite> PrimaryFireBtnSprites { get { return instance.primaryFireBtnSprites; } }
    public static List<tk2dBaseSprite> SecondaryFireBntSprites { get { return instance.secondaryFireBntSprites; } }
    public static List<tk2dBaseSprite> ThrottleLevelSprites { get { return instance.throttleLevelSprites; } }
    public static List<tk2dBaseSprite> GunSightCircleSprites { get { return instance.gunSightCircleSprites; } }
    public static List<tk2dBaseSprite> ZoomSprites { get { return instance.zoomSprites; } }
    public static List<tk2dBaseSprite> ConsumablesSprites { get { return instance.zoomSprites; } }
    public static List<tk2dTextMesh> ConsumablesTexts { get { return instance.consumablesTexts; } }
    public static List<List<tk2dBaseSprite>> AllBlinkingSprites { get; private set; }
    public static List<List<tk2dTextMesh>> AllTextLines { get; private set; }

    public static GUIControlSpriteGroups instance;

    void Awake()
    {
        instance = this;

        AllBlinkingSprites = new List<List<tk2dBaseSprite>>
        {
            moveJoystickSprites,
            upDownJoystickSprites,
            primaryFireBtnSprites,
            secondaryFireBntSprites,
            throttleLevelSprites,
            gunSightCircleSprites,
            zoomSprites,
            consumablesSprites,
        };

        AllTextLines = new List<List<tk2dTextMesh>>()
        {
            consumablesTexts,
        };

        Dispatcher.Subscribe(EventId.MainTankAppeared, OnMainTankAppeared);
    }

    void Start()
    {
        SetActiveSpriteGroups(gunSightCircleSprites, false);
    }

    void OnDestroy()
    {
        instance = null;

        Dispatcher.Unsubscribe(EventId.MainTankAppeared, OnMainTankAppeared);
    }

    public static void SetActiveSpriteGroups(IEnumerable<List<tk2dBaseSprite>> spriteGroups, bool activate)
    {
        foreach (var sprite in spriteGroups.SelectMany(spritesGroup => spritesGroup))
        {
            if(sprite.gameObject.activeSelf != activate)
            {
                sprite.gameObject.SetActive(activate);
            }
        }
    }

    public static void SetActiveTextGroups(IEnumerable<List<tk2dTextMesh>> textMeshes, bool activate)
    {
        foreach (var textMesh in textMeshes.SelectMany(textsGroup => textsGroup))
        {
            if (textMesh.gameObject.activeSelf != activate)
            {
                textMesh.gameObject.SetActive(activate);
            }
        }
    }

    public static void SetActiveSpriteGroups(List<tk2dBaseSprite> sprites, bool activate)
    {
        foreach (var sprite in sprites)
        {
            if(sprite.gameObject.activeSelf != activate)
            {
                sprite.gameObject.SetActive(activate);
            }
        }
    }

    public static void SetActiveTextGroups(List<tk2dTextMesh> texts, bool activate)
    {
        foreach (var txt in texts)
        {
            if (txt.gameObject.activeSelf != activate)
            {
                txt.gameObject.SetActive(activate);
            }
        }
    }

    private void OnMainTankAppeared(EventId id, EventInfo info)
    {
        if (!ProfileInfo.IsBattleTutorial)
            SetActiveSpriteGroups(gunSightCircleSprites, true);
    }
}
