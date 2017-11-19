using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XD;

public class QualityManager : MonoBehaviour
{
    public static int CurrentQualityLevel
    {
        get;
        private set;
    }

    public static QualityManager Instance
    {
        get;
        private set;
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

    private void Start()
    {

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
        string path = string.Format("{0}/Vehicles{2}/{3}"), XD.StaticContainer.GameManager.CurrentResourcesFolder, );
        
        if (GameData.IsGame(Game.SpaceJet))
            return (QualitySettings.GetQualityLevel() > 1) ? path : path + "min";

        return path;
    }*/

    public static string GetShellResPath(string shellName)
    {
        string path = XD.StaticContainer.GameManager.CurrentResourcesFolder + "/Shells";
        return string.Format("{0}/{1}_{2}", path, shellName, QualitySuffix());
    }

    /// <summary>
    /// Устанавливаем материал на переданный объект, в зависимости от настроек качества.
    /// </summary>
    public IEnumerator SetMaterial(List<Renderer> meshRenderers)
    {        
        MaterialQualityLevel quality = StaticType.Options.Instance<IOptions>().CurrentGraphicsDefaults.MaterialQualityLevel;

        for (var i = 0; i < meshRenderers.Count; i++)
        {
            meshRenderers[i].ChangeMaterialQuality(quality);

            if (i % 20 == 0)
            {
                yield return null;
            }
        }
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

    public IEnumerator ChangeObjectMaterials(List<Renderer> renderers)
    {
        yield return StartCoroutine(SetMaterial(renderers));
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
}
