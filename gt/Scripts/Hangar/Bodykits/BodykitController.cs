using UnityEngine;
using System.Collections.Generic;

public class BodykitController : MonoBehaviour
{
    public bool enableShadowPlane;

    /// <summary>
    /// Ссылки на шейдеры, подходящие для нанесения камуфляжа (т.е. те, у которых есть проперти RGB-маски).
    /// </summary>
    public Shader[] patternShaders;

    [Header("Отладка бодикитов")]
    public int debugCamoId;
    public int debugStickerId;

    private const int SHADOW_PLANE_MAX_QUALITY_LEVEL = 2;
    private const string MATERIAL_INSTANCE_POSTFIX = " (Instance)";

    private Pattern currentCamo;
    private GameObject shadowPlane;

    /// <summary>
    /// Дефолтные материалы.
    /// Это должны быть shared материалы, содержащие проперти текстуры камуфляжа, в которую ещё ничего не было записано.
    /// </summary>
    private Dictionary<Shader, List<Material>> defaultMaterials;

    /// <summary>
    /// Присвоение VehicleInfo, поиск дефолтных материалов и активация ShadowPlane.
    /// </summary>
    public void Init()
    {
        RefreshDefaultMaterials();
        SetShadowPlane();
    }

    /// <summary>
    /// Обновление дефолтных материалов (ищутся материалы shared с подходящим для нанесения камуфляжа шейдером).
    /// </summary>
    public void RefreshDefaultMaterials()
    {
        defaultMaterials = defaultMaterials ?? new Dictionary<Shader, List<Material>>();

        foreach (Shader patternShader in patternShaders)
            foreach (Material material in CollectPaintableMaterials(targetShader: patternShader, searchInShared: true))
                if (!defaultMaterials.ContainsKey(patternShader))
                    defaultMaterials.Add(patternShader, new List<Material> { material });
                else
                    defaultMaterials[patternShader].Add(material);

        if (defaultMaterials.Count == 0)
            Debug.LogWarning("Check BodykitController.patternShader and actual vehicle material shader for matching!");
    }

    /// <summary>
    /// Обновление текущих материалов с учётом установленного камуфляжа.
    /// </summary>
    public void RefreshCurrentMaterials()
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
        currentCamo = camo;

        if (currentCamo == null)
        {
            ResetCamouflageTexture();
            return;
        }

        foreach (Shader patternShader in patternShaders)
        {
            List<Material> paintableMaterials = CollectPaintableMaterials(targetShader: patternShader, searchInShared: false);

            foreach (Material paintableMaterial in paintableMaterials)
            {
                // Осторожно! Здесь для разных шейдеров используются одни и те же ключи пропертей маски и цветов.
                paintableMaterial.SetTexture(currentCamo.maskPropertyKey, currentCamo.TextureMask);
                paintableMaterial.SetTextureScale(currentCamo.maskPropertyKey, currentCamo.GetScale(tankId));

                foreach (var propertyKeyColorPair in currentCamo.PropertyKeysToColors)
                    paintableMaterial.SetColor(propertyKeyColorPair.Key, propertyKeyColorPair.Value);
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
        {
            stickerKit.TryActivate(decal);
        }
    }

    /// <summary>
    /// Сброс текстуры камуфляжа (копируются проперти собранных дефолтных материалов).
    /// </summary>
    public void ResetCamouflageTexture()
    {
        if (defaultMaterials == null)
            RefreshDefaultMaterials();

        if (defaultMaterials == null)
        {
            Debug.LogError("Default materials not found!");
            return;
        }

        currentCamo = null;

        foreach (Shader patternShader in patternShaders)
        {
            List<Material> paintableMaterials = CollectPaintableMaterials(targetShader: patternShader, searchInShared: false);

            foreach (Material paintableMaterial in paintableMaterials)
            {
                foreach (Material defaultMaterial in defaultMaterials[patternShader])
                {
                    if (paintableMaterial.name != defaultMaterial.name + MATERIAL_INSTANCE_POSTFIX)
                        continue;

                    paintableMaterial.CopyPropertiesFromMaterial(defaultMaterial);

                    break;
                }
            }
        }
    }

    /// <summary>
    /// Сброс наклейки.
    /// </summary>
    public void ResetDecal()
    {
        DrawDecal(null);
    }

    /// <summary>
    /// Сбор материалов, подходящих для нанесения камуфляжа.
    /// </summary>
    /// <param name="targetShader">Шейдер, который будем искать (со свойством текстуры камуфляжа).</param>
    /// <param name="searchInShared">Ищем ли в shared материалах?</param>
    /// <returns>Материалы, которым можно задать текстуру камуфляжа.</returns>
    private List<Material> CollectPaintableMaterials(Shader targetShader, bool searchInShared)
    {
        List<Material> collectedMaterials = new List<Material>();

        Renderer[] renderers = GetComponentsInChildren<Renderer>(includeInactive: true);

        foreach (Renderer childRenderer in renderers)
        {
            if (childRenderer == null)
                continue;

            if (childRenderer.CompareTag("IgnoreMaterial"))
                continue;

            Material[] childMaterials = searchInShared ? childRenderer.sharedMaterials : childRenderer.materials;

            foreach (Material childMaterial in childMaterials)
            {
                if (childMaterial == null)
                    continue;

                if (childMaterial.shader.name.Equals(targetShader.name))
                    collectedMaterials.Add(childMaterial);
            }
        }

        return collectedMaterials;
    }
}
