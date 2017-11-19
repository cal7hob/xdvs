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

        Messenger.Send(EventId.QualityLevelChanged, new EventInfo_I(CurrentQualityLevel));
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
            foreach (MaterialSwitcher materialSwitcher in materialSwitchers)
            {
                materialSwitcher.Switch(Settings.GraphicsLevel);
                yield return null;
            }
            Settings.RefreshBodykit(obj);
        }
    }

    public IEnumerator ChangeObjectMaterials(GameObject obj)
    {
        yield return StartCoroutine(SetMaterial(obj));
        Resources.UnloadUnusedAssets();
    }

    public static string QualitySuffix(bool forMainVeh)
    {
        switch (CurrentQualityLevel)
        {
            case 0:
                return "min";
            case 1:
            case 2:
                return forMainVeh ? "max" : "min";
            case 3:
                return "max";
            default:
                return "max";
        }
    }
}
