using UnityEngine;
using System.Collections.Generic;
using XD;

public class BodykitController : MonoBehaviour, IPatternController
{
    public bool                                 enableShadowPlane = false;

    /// <summary>
    /// Ссылки на шейдеры, подходящие для нанесения камуфляжа (т.е. те, у которых есть проперти RGB-маски).
    /// </summary>
    public Shader[]                             patternShaders = null;

    [SerializeField]
    private List<Renderer>                      renderers = null;
    [SerializeField]
    private MaterialsContainer                  container = null;

    private const int                           SHADOW_PLANE_MAX_QUALITY_LEVEL = 2;
    private const string                        MATERIAL_INSTANCE_POSTFIX = " (Instance)";
    
    private ICamouflage                         currentCamo = null;
    private GameObject                          shadowPlane = null;

    /// <summary>
    /// Дефолтные материалы.
    /// Это должны быть shared материалы, содержащие проперти текстуры камуфляжа, в которую ещё ничего не было записано.
    /// </summary>
    private Dictionary<Shader, List<Material>>  defaultMaterials = null;

    private List<Material>                      instanceMaterials = null;
    private List<Material>                      sharedMaterials = null;

    private MaterialsContainer Container
    {
        get
        {
            if (container == null)
            {
                container = GetComponent<MaterialsContainer>();

                if (container == null)
                {
                    container = gameObject.AddComponent<MaterialsContainer>();
                }
            }

            return container;
        }
    }

    public void InitRenderers()
    {
        if (renderers == null || renderers.Count == 0)
        {
            renderers = Container.Renderers;
        }
    }

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
        {
            foreach (Material material in CollectPaintableMaterials(patternShader, true))
            {
                List<Material> materials = null;
                if (!defaultMaterials.TryGetValue(patternShader, out materials))
                {
                    materials = new List<Material> { material };
                    defaultMaterials[patternShader] = materials;
                }
                else
                {
                    materials.Add(material);
                }
            }
        }            
            
        //if (defaultMaterials.Count == 0)
        //{
        //    Debug.LogWarning("Check BodykitController.patternShader and actual vehicle material shader for matching!");
        //}
    }

    /// <summary>
    /// Обновление текущих материалов с учётом установленного камуфляжа.
    /// </summary>
    public void RefreshCurrentMaterials()
    {
        //DrawCamouflage(currentCamo, currentCamo != null ? currentCamo.CurrentType : FXLocation.Grass);
    }

    /// <summary>
    /// Присвоение и активация ShadowPlane, если нужно.
    /// Кстати, надо будет перенести этот метод в другой класс.
    /// </summary>
    public void SetShadowPlane() 
    {
        if (!enableShadowPlane || shadowPlane != null)
        {
            return;
        }

        ShadowPlane shadow = GetComponentInChildren<ShadowPlane>();

        if (shadow == null)
        {
            //Debug.LogWarning("Shadow plane not found.");
            return;
        }

        shadowPlane = shadow.gameObject;

        shadowPlane.SetActive(
            (!StaticContainer.SceneManager.InBattle && !ProfileInfo.IsPlayerVip && HangarController.Instance.forceShadowPlaneShow) ||
            QualitySettings.GetQualityLevel() <= SHADOW_PLANE_MAX_QUALITY_LEVEL);
    }

    /// <summary>
    /// Нанесение камуфляжа (ищутся материалы с подходящим шейдером, задаются необходимые проперти).
    /// </summary>
    /// <param name="camo">Объект камуфляжа.</param>
    public void DrawCamouflage(ICamouflage camo, FXLocation camoType = FXLocation.Grass)
    {
        currentCamo = camo;

        if (currentCamo == null)
        {
            ResetCamouflageTexture();
            return;
        }

        //Debug.LogError("currentCamo: " + currentCamo.ID + ", type: " + camoType + ", " + name, gameObject);

        foreach (Shader patternShader in patternShaders)
        {
            List<Material> paintableMaterials = CollectPaintableMaterials(patternShader, false);

            foreach (Material paintableMaterial in paintableMaterials)
            {
                // Осторожно! Здесь для разных шейдеров используются одни и те же ключи пропертей маски и цветов.
                paintableMaterial.SetTexture(currentCamo.MaskPropertyKey, currentCamo.TextureMask);
                paintableMaterial.SetTextureScale(currentCamo.MaskPropertyKey, currentCamo.Tiling);

                Dictionary<string, Color> propertyKeysToColors = currentCamo.GetColorSet(camoType);

                foreach (KeyValuePair<string, Color> propertyKeyColorPair in propertyKeysToColors)
                {
                    paintableMaterial.SetColor(propertyKeyColorPair.Key, propertyKeyColorPair.Value);
                }
            }
        }
    }

    /// <summary>
    /// Активация комплекта наклеек.
    /// </summary>
    /// <param name="decal">Объект наклейки.</param>
    public void DrawDecal(IDecal decal)
    {
        foreach (StickerKit stickerKit in GetComponentsInChildren<StickerKit>(true))
        {
            stickerKit.gameObject.SetActive(decal != null && decal.ID == stickerKit.id);
        }
    }

    /// <summary>
    /// Сброс текстуры камуфляжа (копируются проперти собранных дефолтных материалов).
    /// </summary>
    public void ResetCamouflageTexture()
    {
        if (defaultMaterials == null)
        {
            ColoredDebug.Log(name + " [null defaultMaterials!]", this, "orange");
            return;
        }     

        currentCamo = null;

        foreach (Shader patternShader in patternShaders)
        {
            if (patternShader == null)
            {
                ColoredDebug.Log(name + " [null shader!]", this, "orange");
                continue;
            }
            List<Material> paintableMaterials = CollectPaintableMaterials(patternShader, false);

            //List<Material> paintableMaterials = Container.GetMaterials(patternShader);

            foreach (Material paintableMaterial in paintableMaterials)
            {
                if (defaultMaterials[patternShader] == null)
                {
                    ColoredDebug.Log(name + " [null materials by shader] " + patternShader.name, this, "orange");
                    continue;
                }

                foreach (Material defaultMaterial in defaultMaterials[patternShader])
                {
                    if (paintableMaterial.name != defaultMaterial.name + MATERIAL_INSTANCE_POSTFIX)
                    {
                        continue;
                    }

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
        if (defaultMaterials == null)
        {
            Init();
        }

        InitRenderers();

        if (searchInShared)
        {
            if (sharedMaterials == null)
            {
                sharedMaterials = GetMaterials(searchInShared);
            }
        }
        else
        {
            if (instanceMaterials == null)
            {
                instanceMaterials = GetMaterials(searchInShared);
            }
        }

        List<Material> collectedMaterials = new List<Material>();
        List<Material> childMaterials = searchInShared ? sharedMaterials : instanceMaterials;
        //Debug.LogWarningFormat(this, "'{0}' contains '{1}' renderers and '{2}' materials!", name, renderers.Length, childMaterials.Count);

        foreach (Material childMaterial in childMaterials)
        {
            if (childMaterial == null)
            {
                continue;
            }

            if (childMaterial.shader == targetShader)
            {
                collectedMaterials.Add(childMaterial);
            }
        }

        return collectedMaterials;
    }

    private List<Material> GetMaterials(bool searchInShared)
    {
        List<Material> collectedMaterials = new List<Material>();

        foreach (Renderer childRenderer in renderers)
        {
            if (childRenderer == null)
            {
                continue;
            }

            collectedMaterials.Add(searchInShared ? childRenderer.sharedMaterial : childRenderer.material);
        }

        return collectedMaterials;
    }
}
