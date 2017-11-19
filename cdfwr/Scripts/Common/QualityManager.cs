using System;
using System.Collections;
using UnityEngine;

public class QualityManager : MonoBehaviour
{
    public static int CurrentQualityLevel { get; private set; }

    public static QualityManager Instance { get; private set; }

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

    public static void SetQualityLevel(int newLevel)
    {
        if (newLevel == CurrentQualityLevel)
        {
            return;
        }

        QualitySettings.SetQualityLevel(newLevel);

        CurrentQualityLevel = newLevel;

        Dispatcher.Send(EventId.QualityLevelChanged, new EventInfo_I(CurrentQualityLevel));
    }
    
    
    /*    public static string GetVehicleResPath(string vehicleName)
    {
        string path = string.Format("{0}/Vehicles{2}/{3}"), GameManager.CurrentResourcesFolder, );
        
        if (GameData.IsGame(Game.SpaceJet))
            return (QualitySettings.GetQualityLevel() > 1) ? path : path + "min";

        return path;
    }*/
    
    public static string GetShellResPath(string shellName)
    {
        string path = GameManager.CurrentResourcesFolder + "/Shells";
        return string.Format("{0}/{1}_{2}", path, shellName, QualitySuffix());
    }

    /// <summary>
    /// Устанавливаем материал на переданный объект, в зависимости от настроек качества.
    /// </summary>
    public IEnumerator SetMaterial(GameObject obj)
    {
        if (obj == null)
            yield break;

        MaterialSwitcher[] materialSwitchers = obj.GetComponentsInChildren<MaterialSwitcher>();//у объекта есть кастомные настройки

        if (materialSwitchers.Length > 0)
        {
            // Если есть нужные компоненты – используем не завязанное на строках переключение материалов:
            yield return StartCoroutine(SetMaterial(materialSwitchers));
            Settings.RefreshBodykit(obj);

            yield break;
        }

        var meshRenderers = obj.GetComponentsInChildren<MeshRenderer>();

        MaterialQualityLevel quality;

        if (Settings.GraphicsLevel > GraphicsLevel.highQuality)
        {
            quality = MaterialQualityLevel.pc_max;
        }
        else
        {
            quality = Settings.GraphicsLevel > GraphicsLevel.normalQuality ? 
            MaterialQualityLevel.mobile_max: MaterialQualityLevel.mobile_default;
        }

        for (var i = 0; i < meshRenderers.Length; i++)
        {
            ChangeMaterialQuality(meshRenderers[i], quality);
            
            if (i%20 == 0)
            {
                yield return null;
            }
        }

        Settings.RefreshBodykit(obj);
    }

    public IEnumerator SetMaterial(MaterialSwitcher[] materialSwitchers)
    {
        foreach (MaterialSwitcher materialSwitcher in materialSwitchers)
        {
            materialSwitcher.Switch(Settings.GraphicsLevel);
            Debug.Log("Корутина работает!!!");
            yield return null;
        }
    }

    public IEnumerator ChangeObjectMaterials(GameObject obj)
    {
        yield return StartCoroutine(SetMaterial(obj));
        Resources.UnloadUnusedAssets();
    }

    private static string QualitySuffix()
    {
        switch (CurrentQualityLevel)
        {
            case 0:
            case 1:
                return "min";
            default:
                return "max";
        }
    }

#if UNITY_EDITOR

    [UnityEditor.MenuItem("HelpTools/Change Material Quality in scene/Mobile_default")]
    public static void ChangeMaterialQualityEditor_mobile_default()
    {
        ChangeMaterialQuality(MaterialQualityLevel.mobile_default);
    }

    [UnityEditor.MenuItem("HelpTools/Change Material Quality in scene/Mobile_max")]
    public static void ChangeMaterialQualityEditor_mobile_max()
    {
        ChangeMaterialQuality(MaterialQualityLevel.mobile_max);
    }

    [UnityEditor.MenuItem("HelpTools/Change Material Quality in scene/PC_max")]
    public static void ChangeMaterialQualityEditor_pc_max()
    {
        ChangeMaterialQuality(MaterialQualityLevel.pc_max);
    }

#endif

    public static void ChangeMaterialQuality(MaterialQualityLevel qLvl)
    {
        MeshRenderer[] allSceneObjects = FindObjectsOfType<MeshRenderer>();
        foreach (var rend in allSceneObjects)
        {
            ChangeMaterialQuality(rend, qLvl);
        }
        Debug.Log("Ready!");
    }


    public static void ChangeMaterialQuality(MeshRenderer rend_, MaterialQualityLevel qLvl)
    {
        if (rend_ == null || rend_.sharedMaterial == null)
        {
            return;
        }

        string name_ = string.Format("{0}/Materials/{1}", GameManager.CurrentResourcesFolder, rend_.sharedMaterial.name);
        GetMaterialName(qLvl, ref name_);

        Material mat_ = null;

        if (CheckMaterial(rend_, name_, out mat_))
        {
            rend_.sharedMaterial = mat_;
        }
    }

    private static void GetMaterialName(MaterialQualityLevel qLvl, ref string name_)
    {
        name_ = name_.Replace(" (Instance)", string.Empty);

        switch (qLvl)
        {
            case MaterialQualityLevel.mobile_default:
                name_ = name_.Replace("_max", string.Empty);
                name_ = name_.Replace("_Umax", string.Empty);
                break;
            case MaterialQualityLevel.mobile_max:
                name_ = name_.Replace("_Umax", string.Empty);
                name_ += "_max";
                break;
            case MaterialQualityLevel.pc_max:
                name_ = name_.Replace("_max", string.Empty);
                name_ += "_Umax";
                break;
        }
    }

    private static bool CheckMaterial(MeshRenderer meshRenderer, string matName, out Material mat)
    {
        var slashIndex = matName.LastIndexOf("/", StringComparison.Ordinal) + 1;
        var realMatName = matName.Substring(slashIndex, matName.Length - slashIndex);

        if (meshRenderer.sharedMaterial.name == realMatName)
        {
            mat = null;
            return false;
        }

        mat = Resources.Load(matName, typeof(Material)) as Material;

        if (mat == null)
        {
            return false;
        }

        return true;
        //MaterialManager.RegisterMaterial(mat);
    }
}
