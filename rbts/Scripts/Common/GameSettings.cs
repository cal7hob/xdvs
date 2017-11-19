using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu]
[Serializable]
public class GameSettings : ScriptableObject, ISerializationCallbackReceiver
{
    [Header("Стрельба очередями")]
    [Tooltip("Нагрев за выстрел")]
    [SerializeField] private float heating = 0.04f;
    [SerializeField] private float coldFireRatio = 2f;

    [Header("Обводка")]
    [SerializeField] private float outlineWidth = 0.04f;
    [SerializeField] private float zoomOutlineWidth = 0.01f;

    [Header("Спец. эффекты на технике")]
    [SerializeField] private FXInfo stunFX;

    [SerializeField] private ShieldFX.ShieldFXInfo lowShieldInfo;
    [SerializeField] private ShieldFX.ShieldFXInfo medShieldInfo;
    [SerializeField] private ShieldFX.ShieldFXInfo highShieldInfo;

    [SerializeField] private ResourceLink shieldEnabledSound;
    [SerializeField] private ResourceLink shieldDisabledSound;
    [SerializeField] private ResourceLink blindingRaySound;
    [SerializeField] private ResourceLink blindingSound;
    [SerializeField] private ResourceLink stunRaySound;
    [SerializeField] private ResourceLink stunSound;


#if UNITY_STANDALONE_OSX
    [HideInInspector] public bool localTestBuild;
#endif

    [HideInInspector][SerializeField] private int[] shellIds;
    [HideInInspector][SerializeField] private FXInfo[] shellFxInfos;

    private Dictionary<int, FXInfo> shellInfos;
    
    private static GameSettings instance;
    public static GameSettings Instance
    {
        get
        {
            if (instance == null)
                Init();

            return instance;
        }
    }

    public void OnBeforeSerialize()
    {
/*        if (shellInfos == null || shellInfos.Count == 0)
            return;

        shellIds = shellInfos.Keys.ToArray();
        shellFxInfos = shellInfos.Values.ToArray();*/
    }

    public void OnAfterDeserialize()
    {
        shellInfos = new Dictionary<int, FXInfo>();
        for (int i = 0; i < shellIds.Length; i++)
        {
            shellInfos.Add(shellIds[i], shellFxInfos[i]);
        }
    }

    public static void Init()
    {
        string settingsResPath = string.Format("{0}/GameSettings", GameManager.CurrentResourcesFolder);
        instance = Resources.Load<GameSettings>(settingsResPath);
    }

    public static void Dispose()
    {
        instance = null;
    }

    public float ColdFireRatio
    {
        get { return coldFireRatio; }
    }

    public float OutlineWidth
    {
        get { return outlineWidth; }
    }

    public FXInfo StunFX { get { return stunFX; } }

    public ShieldFX.ShieldFXInfo LowShieldInfo { get { return lowShieldInfo; } }
    public ShieldFX.ShieldFXInfo MedShieldInfo { get { return medShieldInfo; } }
    public ShieldFX.ShieldFXInfo HighShieldInfo { get { return highShieldInfo; } }

    public ResourceLink ShieldEnabledSound { get { return shieldEnabledSound; } }
    public ResourceLink ShieldDisabledSound { get { return shieldDisabledSound; } }
    public ResourceLink BlindingRaySound{ get { return blindingRaySound; } }
    public ResourceLink BlindingSound { get { return blindingSound; } }
    public ResourceLink StunRaySound {get { return stunRaySound; } }
    public ResourceLink StunSound {get { return stunSound; } }
    

    /*    public float Cooling
        {
            get { return cooling; }
        }*/

    public float Heating
    {
        get { return heating; }
    }

    public float ZoomOutlineWidth
    {
        get { return zoomOutlineWidth; }
    }

    public FXInfo GetShellInfo(int shellId)
    {
        return shellInfos[shellId];
    }

    public void ClearResourceLinks()
    {
        shieldEnabledSound.Dispose();
        shieldDisabledSound.Dispose();
        blindingRaySound.Dispose();
        blindingSound.Dispose();
        stunRaySound.Dispose();
        stunSound.Dispose();
    }
}
