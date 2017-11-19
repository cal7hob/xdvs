using System;
using System.Collections;
using AssetBundles;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class QualityManager : MonoBehaviour
{
    private const int CHANGES_PER_FRAME = 20;
    private const string MIN_QUALITY_SUFFIX = "_min";
    private const string MAX_QUALITY_SUFFIX = "_max";
    private const string UMAX_QUALITY_SUFFIX = "_Umax";
    private const string INSTANCE_SUFFIX = " (Instance)";

    public static QualityManager Instance { get; private set; }
    public static int CurrentQualityLevel { get; private set; }

    public static string QualitySuffix
    {
        get
        {
            switch (CurrentQualityLevel)
            {
                case 0:
                case 1:
                    return MIN_QUALITY_SUFFIX;
                default:
                    return MAX_QUALITY_SUFFIX;
            }
        }
    }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        CurrentQualityLevel = QualitySettings.GetQualityLevel();

        DontDestroyOnLoad(gameObject);
    }

#if UNITY_EDITOR

    [MenuItem("HelpTools/Change Material Quality in scene/Mobile_default")]
    public static void ChangeMaterialQualityEditor_mobile_default()
    {
        ChangeMaterialsInScene(MaterialQualityLevel.mobile_default);
    }

    [MenuItem("HelpTools/Change Material Quality in scene/Mobile_max")]
    public static void ChangeMaterialQualityEditor_mobile_max()
    {
        ChangeMaterialsInScene(MaterialQualityLevel.mobile_max);
    }

    [MenuItem("HelpTools/Change Material Quality in scene/PC_max")]
    public static void ChangeMaterialQualityEditor_pc_max()
    {
        ChangeMaterialsInScene(MaterialQualityLevel.pc_max);
    }

#endif

    public static void SetQualityLevel(int newLevel)
    {
        if (newLevel == CurrentQualityLevel)
            return;

        QualitySettings.SetQualityLevel(newLevel);

        CurrentQualityLevel = newLevel;

        Dispatcher.Send(EventId.QualityLevelChanged, new EventInfo_I(CurrentQualityLevel));
    }

    public IEnumerator ObjectMaterialsChanging(GameObject obj)
    {
        yield return StartCoroutine(MaterialsSetting(obj, Settings.GraphicsLevel));
        Resources.UnloadUnusedAssets();
    }

    /// <summary>
    /// Замена материалов в объекте, с указанием качества (в обход настроек).
    /// </summary>
    /// <param name="obj">Объект.</param>
    /// <param name="graphicsLevel">Нужное качество.</param>
    /// <param name="immediate">Пытаться ли не пропускать кадры.</param>
    /// <returns>Корутина замены матов.</returns>
    public IEnumerator ObjectMaterialsChanging(GameObject obj, GraphicsLevel graphicsLevel, bool immediate)
    {
        yield return StartCoroutine(MaterialsSetting(obj, graphicsLevel, immediate));
        Resources.UnloadUnusedAssets();
    }

    private static void ChangeMaterialsInScene(MaterialQualityLevel qualityLevel)
    {
        MeshRenderer[] allSceneObjects = FindObjectsOfType<MeshRenderer>();

        foreach (MeshRenderer rend in allSceneObjects)
            ChangeMaterial(rend, qualityLevel);
    }

    private static void ChangeMaterial(MeshRenderer renderer, MaterialQualityLevel qualityLevel)
    {
        if (renderer == null || renderer.sharedMaterial == null)
            return;

        string materialName = renderer.sharedMaterial.name;

        string qualitySuffix = GetMaterialQualitySuffix(qualityLevel);

        if (!CheckMaterialNeedsReplacement(materialName, qualitySuffix))
            return;

        string newMaterialName = GetMaterialClearName(materialName, qualitySuffix);

        Instance.StartCoroutine(Instance.MaterialLoading(renderer, newMaterialName));
    }

    private static bool CheckMaterialNeedsReplacement(string materialName, string qualitySuffix)
    {
        if (materialName.Contains(qualitySuffix))
            return false;

        if (materialName.Contains(MIN_QUALITY_SUFFIX) ||
            materialName.Contains(MAX_QUALITY_SUFFIX) ||
            materialName.Contains(UMAX_QUALITY_SUFFIX))
        {
            return true;
        }

        return false;
    }

    private static string GetMaterialQualitySuffix(MaterialQualityLevel qualityLevel)
    {
        switch (qualityLevel)
        {
            case MaterialQualityLevel.mobile_default:
                return MIN_QUALITY_SUFFIX;
            case MaterialQualityLevel.mobile_max:
            case MaterialQualityLevel.pc_max:
                return MAX_QUALITY_SUFFIX;
            default:
                return MIN_QUALITY_SUFFIX;
        }
    }

    private static string GetMaterialClearName(string materialName, string qualitySuffix)
    {
        materialName = materialName.Replace(INSTANCE_SUFFIX, string.Empty);
        materialName = materialName.Replace(MIN_QUALITY_SUFFIX, string.Empty);
        materialName = materialName.Replace(MAX_QUALITY_SUFFIX, string.Empty);
        materialName = materialName.Replace(UMAX_QUALITY_SUFFIX, string.Empty);

        materialName += qualitySuffix;

        return materialName;
    }

    /// <summary>
    /// Устанавливаем материал на переданный объект, в зависимости от настроек качества.
    /// </summary>
    private IEnumerator MaterialsSetting(GameObject obj, GraphicsLevel graphicsLevel, bool immediate = false)
    {
        if (obj == null)
            yield break;

        MaterialSwitcher[] materialSwitchers = obj.GetComponentsInChildren<MaterialSwitcher>(); // У объекта есть кастомные настройки.

        if (materialSwitchers.Length > 0)
        {
            // Если есть нужные компоненты – используем не завязанное на строках переключение материалов:
            yield return StartCoroutine(MaterialsSwitching(materialSwitchers));
            Settings.RefreshBodykit(obj);

            yield break;
        }

        if (!GameData.IsGame(Game.SpaceJet | Game.BattleOfWarplanes | Game.WingsOfWar | Game.BattleOfHelicopters | Game.Armada | Game.MetalForce))
            yield break;

        MeshRenderer[] meshRenderers = obj.GetComponentsInChildren<MeshRenderer>();

        MaterialQualityLevel quality;

        if (GameData.IsGame(Game.Armada | Game.MetalForce) && SystemInfo.deviceType != DeviceType.Handheld)
            quality = MaterialQualityLevel.pc_max;
        else
            quality = ((int)graphicsLevel > (int)GraphicsLevel.normalQuality) ? MaterialQualityLevel.mobile_max : MaterialQualityLevel.mobile_default;

        for (int i = 0; i < meshRenderers.Length; i++)
        {
            ChangeMaterial(meshRenderers[i], quality);

            if (i % CHANGES_PER_FRAME == 0 && !immediate)
                yield return null;
        }

        Settings.RefreshBodykit(obj);
    }

    private IEnumerator MaterialsSwitching(MaterialSwitcher[] materialSwitchers)
    {
        foreach (MaterialSwitcher materialSwitcher in materialSwitchers)
        {
            materialSwitcher.Switch(Settings.GraphicsLevel);
            yield return null;
        }
    }

    private IEnumerator MaterialLoading(MeshRenderer renderer, string materialName)
    {
        string bundleName = string.Format("{0}/materials", GameManager.CurrentResourcesFolder).ToLower();

        AssetBundleLoadAssetOperation request = AssetBundleManager.LoadAssetAsync(bundleName, materialName, typeof(Material));

        if (request == null)
            yield break;

        yield return StartCoroutine(request);

        if (renderer == null)
            yield break;

        Material material = request.GetAsset<Material>();

        int slashIndex = materialName.LastIndexOf("/", StringComparison.Ordinal) + 1;
        string realMatName = materialName.Substring(slashIndex, materialName.Length - slashIndex);

        if (renderer.sharedMaterial.name == realMatName)
            yield break;

        renderer.sharedMaterial = material;

        //MaterialManager.RegisterMaterial(material);
    }
}
