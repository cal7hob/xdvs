using UnityEngine;
using System.Collections.Generic;

public class BodykitController : MonoBehaviour
{
    private const int SHADOW_PLANE_MAX_QUALITY_LEVEL = 5;
    private const string MATERIAL_INSTANCE_POSTFIX = " (Instance)";
    
    public bool enableShadowPlane;

    /// <summary>
    /// Ссылки на шейдеры, подходящие для нанесения камуфляжа (т.е. те, у которых есть проперти RGB-маски).
    /// </summary>
    public Shader[] patternShaders;

    [Header("Отладка бодикитов")]
    public int debugCamoId;
    public int debugStickerId;
    
    private Dictionary<Material, Material> matCorrespondence; //key - sharedMaterial, value - material
    private List<Material> paintableMaterials;

    private bool inited;
    private Pattern currentCamo;
    private GameObject shadowPlane;

    /// <summary>
    /// Присвоение VehicleInfo, поиск дефолтных материалов и активация ShadowPlane.
    /// </summary>
    public void Init()
    {
        CollectPaintableMaterials();
        SetShadowPlane();
        inited = true;
    }

    void OnDestroy()
    {
        if (matCorrespondence == null)
            return;

        foreach (Material mat in matCorrespondence.Values)
        {
            Destroy(mat);
        }
    }

    /// <summary>
    /// Обновление текущих материалов с учётом установленного камуфляжа.
    /// </summary>
    public void RefreshCamouflage()
    {
        DrawCamouflage(currentCamo, 0);
    }

    /// <summary>
    /// Присвоение и активация ShadowPlane, если нужно.
    /// Кстати, надо будет перенести этот метод в другой класс.
    /// </summary>
    public void SetShadowPlane() 
    {
        if (!enableShadowPlane || shadowPlane != null)
            return;

        ShadowPlane shadow = GetComponentInChildren<ShadowPlane>();

        if (shadow == null)
        {
            Debug.LogWarning("Shadow plane not found.");
            return;
        }

        shadowPlane = shadow.gameObject;

        shadowPlane.SetActive(
            (GameData.IsHangarScene && !ProfileInfo.IsPlayerVip && HangarController.Instance.forceShadowPlaneShow) ||
            QualitySettings.GetQualityLevel() <= SHADOW_PLANE_MAX_QUALITY_LEVEL);
    }

    /// <summary>
    /// Нанесение камуфляжа (ищутся материалы с подходящим шейдером, задаются необходимые проперти).
    /// </summary>
    /// <param name="camo">Объект камуфляжа.</param>
    /// <param name="tankId">ID танка.</param>
    public void DrawCamouflage(Pattern camo, int tankId)
    {
        if (!inited)
        {
            Init();
        }

        currentCamo = camo;

        if (currentCamo == null)
        {
            ResetCamouflage();
            return;
        }

        SetMaterialInstances();

        foreach (Material paintableMaterial in paintableMaterials)
        {
            // Осторожно! Здесь для разных шейдеров используются одни и те же ключи пропертей маски и цветов.
            paintableMaterial.SetTexture(currentCamo.maskPropertyKey, currentCamo.TextureMask);
            paintableMaterial.SetTextureScale(currentCamo.maskPropertyKey, currentCamo.GetScale(tankId));

            var propertyKeysToColors
                = currentCamo.PropertyKeysToColors;

            foreach (var propertyKeyColorPair in propertyKeysToColors)
                paintableMaterial.SetColor(propertyKeyColorPair.Key, propertyKeyColorPair.Value);
        }
    }

    private void SetMaterialInstances()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var rend in renderers)
        {
            Material mat;
            if (!matCorrespondence.TryGetValue(rend.sharedMaterial, out mat))
                continue;

            rend.material = mat;
        }
    }

    /// <summary>
    /// Сброс текстуры камуфляжа (копируются проперти собранных дефолтных материалов).
    /// </summary>
    public void ResetCamouflage()
    {
        if (!inited)
        {
            Init();
        }

        currentCamo = null;
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var rend in renderers)
        {
            foreach (var matCor in matCorrespondence)
            {
                if (matCor.Value == rend.sharedMaterial)
                    rend.material = matCor.Key;
            }
        }
    }

    /// <summary>
    /// Активация комплекта наклеек.
    /// </summary>
    /// <param name="decal">Объект наклейки.</param>
    public void DrawDecal(Decal decal)
    {
        foreach (StickerKit stickerKit in GetComponentsInChildren<StickerKit>(true))
            stickerKit.TryActivate(decal);
    }

    /// <summary>
    /// Сброс наклейки.
    /// </summary>
    public void ResetDecal()
    {
        DrawDecal(null);
    }

    private void CollectPaintableMaterials()
    {
        matCorrespondence = new Dictionary<Material, Material>();
        paintableMaterials = new List<Material>();

        foreach (var shader in patternShaders)
        {
            CollectPaintableMaterials(shader, paintableMaterials);
        }
    }
    
    /// <summary>
    /// Сбор материалов, подходящих для нанесения камуфляжа.
    /// </summary>
    /// <param name="targetShader">Шейдер, который будем искать (со свойством текстуры камуфляжа).</param>
    /// <param name="outputList">Список, в который будет добавден перечень найденных (полученнных) материалов</param>
    private void CollectPaintableMaterials(Shader targetShader, List<Material> outputList)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(includeInactive: true);

        foreach (Renderer childRenderer in renderers)
        {
            if (childRenderer == null || childRenderer.CompareTag("IgnoreMaterial"))
                continue;

            Material sharedMaterial = childRenderer.sharedMaterial;
            if (sharedMaterial.shader.name != targetShader.name)
                continue;

            Material foundMaterial;
            if (!matCorrespondence.TryGetValue(sharedMaterial, out foundMaterial))
            {
                Material newMaterial = new Material(childRenderer.sharedMaterial);
                newMaterial.name = newMaterial.name + " (NewInstance)";
                matCorrespondence.Add(sharedMaterial, newMaterial);
                outputList.Add(newMaterial);
            }
            else
            {
                childRenderer.material = foundMaterial;
            }
        }
    }
}
