using UnityEngine;
using System.Collections.Generic;

public class BodykitController : MonoBehaviour
{
    public bool enableShadowPlane;

    [Header("Отладка бодикитов")]
    public int debugCamoId;
    public int debugStickerId;

    private const int SHADOW_PLANE_MAX_QUALITY_LEVEL = 2;
    private const string MATERIAL_INSTANCE_POSTFIX = " (Instance)";

    private readonly Vector2 PATTERN_TEXTURE_SCALE_LOD_BOW = new Vector2(4, 4);
 
    private Pattern currentCamo;
    private GameObject shadowPlane;
    private Transform shield;
    private Transform atgw;
    private Transform ags;
    private Transform machinegun;
    private VehicleController vehicleController;
    private List<Shader> patternShaders;

    /// <summary>
    /// Дефолтные материалы.
    /// Это должны быть shared материалы, содержащие проперти текстуры камуфляжа, в которую ещё ничего не было записано.
    /// </summary>
    private Dictionary<Shader, List<Material>> defaultMaterials;

    private List<Shader> PatternShaders
    {
        get { return patternShaders = patternShaders ?? FindPatternShaders(); }
    }

    public List<StickerKitMF> StickerKitsMF
    {
        get { return StickerKitMF.ParseList(gameObject.transform); }
    }

    private int VehicleIdFromObjectName
    {
        get
        {
            string[] nameParts = name.Split('_');

            string idString = nameParts[0];

            if (GameData.IsGame(Game.SpaceJet))
                idString = nameParts[1];

            return int.Parse(idString);
        }
    }

    private int VehicleId
    {
        get
        {
            if (vehicleController != null)
                return vehicleController.VehicleId;

            return VehicleIdFromObjectName;
        }
    }

    private Transform Shield
    {
        get { return shield = shield ?? transform.FindInHierarchy(string.Format("{0}_armor_shield", GameData.InterfaceShortName)); }
    }

    private Transform ATGW
    {
        get { return atgw = atgw ?? transform.Find("Body/Turret/Add-Weapons/AR_armor_ptur_2"); }
    }

    private Transform AGS
    {
        get { return ags = ags ?? transform.Find("Body/Turret/Add-Weapons/MF_AGS"); }
    }

    private Transform MachineGun
    {
        get { return machinegun = machinegun ?? transform.Find("Body/Turret/Add-Weapons/MF_GUN"); }
    }

    private VehicleInfo VehicleInfo
    {
        get { return VehiclePool.Instance.GetItemById(VehicleId); }
    }

    void Awake()
    {
        vehicleController = GetComponent<VehicleController>();
        Dispatcher.Subscribe(EventId.TankJoinedBattle, OnTankJoinedBattle);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.TankJoinedBattle, OnTankJoinedBattle);
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
        patternShaders = FindPatternShaders();

        defaultMaterials = defaultMaterials ?? new Dictionary<Shader, List<Material>>();

        foreach (Shader patternShader in PatternShaders)
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
            if (!GameData.IsGame(Game.MetalForce))
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
            FixLODsMaterial();
            return;
        }

        foreach (Shader patternShader in PatternShaders)
        {
            List<Material> paintableMaterials = CollectPaintableMaterials(targetShader: patternShader, searchInShared: false);

            foreach (Material paintableMaterial in paintableMaterials)
            {
                // Осторожно! Здесь для разных шейдеров используются одни и те же ключи пропертей маски и цветов.
                paintableMaterial.SetTexture(currentCamo.maskPropertyKey, currentCamo.TextureMask);
                paintableMaterial.SetTextureScale(currentCamo.maskPropertyKey, currentCamo.GetScale(tankId));

                var propertyKeysToColors
                    = GameData.IsGame(Game.BattleOfHelicopters) && BattleController.Instance != null
                        ? currentCamo.PropertyKeysToBattleColors
                        : currentCamo.PropertyKeysToColors;

                foreach (var propertyKeyColorPair in propertyKeysToColors)
                    paintableMaterial.SetColor(propertyKeyColorPair.Key, propertyKeyColorPair.Value);
            }
        }

        FixLODsMaterial();
    }

    /// <summary>
    /// Активация комплекта наклеек.
    /// </summary>
    /// <param name="decal">Объект наклейки.</param>
    public void DrawDecal(Decal decal)
    {
        if (GameData.IsGame(Game.MetalForce))
        {
            DrawDecalMF(decal);
            return;
        }

        foreach (StickerKit stickerKit in GetComponentsInChildren<StickerKit>(true))
            stickerKit.TryActivate(decal);
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

        foreach (Shader patternShader in PatternShaders)
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
    /// Показать щит.
    /// </summary>
    public void ShowShield(bool active, float duration = 0)
    {
        if (Shield == null)
            return;

        if (GameData.IsGame(Game.BattleOfWarplanes | Game.WingsOfWar))
            ShowShieldBOW(active, duration);
        else
            ShowShieldDefault(active);
    }

    /// <summary>
    /// Костылище для копирования цветов дефолтного раскрашивания на лоды.
    /// </summary>
    public void FixLODsMaterial()
    {
        if (BattleController.Instance == null || !GameData.IsGame(Game.BattleOfWarplanes | Game.WingsOfWar))
            return;

        Material sourceMaterial = GetPaintableMaterial();

        if (sourceMaterial == null)
            return;

        List<Material> lodPaintableMaterials = GetLODPaintableMaterials();

        foreach (Material material in lodPaintableMaterials)
        {
            material.SetColor(Pattern.COLOR_FIRST_KEY, sourceMaterial.GetColor(Pattern.COLOR_FIRST_KEY));
            material.SetColor(Pattern.COLOR_SECOND_KEY, sourceMaterial.GetColor(Pattern.COLOR_SECOND_KEY));
            material.SetColor(Pattern.COLOR_THIRD_KEY, sourceMaterial.GetColor(Pattern.COLOR_THIRD_KEY));
            material.SetTexture(Pattern.MASK_TEX_KEY, sourceMaterial.GetTexture(Pattern.MASK_TEX_KEY));
            material.SetTextureScale(Pattern.MASK_TEX_KEY, PATTERN_TEXTURE_SCALE_LOD_BOW);
        }
    }

    private void OnTankJoinedBattle(EventId id, EventInfo ei)
    {
        EventInfo_I info = (EventInfo_I)ei;

        int playerId = info.int1;

        if (playerId == vehicleController.data.playerId)
            ShowSuperWeapon(vehicleController.SuperWeaponInfo);
    }

    private void DrawDecalMF(Decal decal)
    {
        if (gameObject == null || transform.Find("Body") == null || VehicleInfo.isComingSoon)
            return;

        string stickerWrapperPath = "Body/MF_Obves_stickers";
        Transform stickersWrapper = transform.Find(stickerWrapperPath);

        if (stickersWrapper == null)
        {
            Debug.LogError(string.Format("'{0}' not found!", stickerWrapperPath), gameObject);
            return;
        }

        stickersWrapper.gameObject.SetActive(decal != null);

        foreach (StickerKitMF stickerKit in StickerKitsMF)
            stickerKit.TryActivate(decal);

        if (decal != null)
            RefreshDefaultMaterials(); // На обвесе под стикеры может быть другой шейдер.

        DrawCamouflage(currentCamo, VehicleInfo.id);
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

                if (searchInShared && CheckDuplicateDefaultMaterial(childMaterial))
                    continue;

                if (childMaterial.shader.name == targetShader.name)
                    collectedMaterials.Add(childMaterial);
            }
        }

        return collectedMaterials;
    }

    private bool CheckDuplicateDefaultMaterial(Material childMaterial)
    {
        if (!defaultMaterials.ContainsKey(childMaterial.shader))
            return false;

        foreach (Material defaultMaterial in defaultMaterials[childMaterial.shader])
        {
            if (childMaterial.name.Contains(defaultMaterial.name))
                return true;
        }

        return false;
    }

    private List<Shader> FindPatternShaders()
    {
        List<Shader> result = new List<Shader>();

        Renderer[] renderers = GetComponentsInChildren<Renderer>(includeInactive: true);

        foreach (Renderer rend in renderers)
        {
            foreach (Material material in rend.sharedMaterials)
            {
                if (material != null &&
                    material.HasProperty(Pattern.COLOR_FIRST_KEY) &&
                    material.HasProperty(Pattern.COLOR_SECOND_KEY) &&
                    material.HasProperty(Pattern.COLOR_THIRD_KEY) &&
                    !result.Contains(material.shader))
                {
                    result.Add(material.shader);
                }
            }
        }

        return result;
    }

    private void ShowSuperWeapon(ConsumableInfo superWeapon)
    {
        if (superWeapon == null)
            return;

        switch (superWeapon.SuperWeaponType)
        {
            case SuperWeaponType.ATGW:
                ATGW.gameObject.SetActive(true);
                break;
            case SuperWeaponType.AGS:
                AGS.gameObject.SetActive(true);
                break;
            case SuperWeaponType.MachineGun:
                MachineGun.gameObject.SetActive(true);
                break;
        }
    }

    private void ShowShieldDefault(bool active)
    {
        Shield.gameObject.SetActive(active);

        if (active)
        {
            RefreshDefaultMaterials(); // Чтобы взять маты с щита.
            DrawCamouflage(currentCamo, VehicleInfo.id);
        }
    }

    private void ShowShieldBOW(bool active, float duration)
    {
        Shield.gameObject.SetActive(active);

        if (active && vehicleController.IsMain)
        {
            ShieldEffectBOW shieldEffect = Shield.gameObject.GetComponent<ShieldEffectBOW>();
            shieldEffect.SetEndEffectDelay(duration);
            shieldEffect.Play();
        }
    }

    private List<Material> GetLODPaintableMaterials()
    {
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(true);
        List<Material> result = new List<Material>();

        foreach (MeshRenderer renderer in renderers)
        {
            if (!renderer.transform.name.Contains("_LOD") || renderer.transform.name.Contains("_LOD0"))
                continue;

            foreach (Material material in renderer.materials)
            {
                if (material != null &&
                    material.HasProperty(Pattern.COLOR_FIRST_KEY) &&
                    material.HasProperty(Pattern.COLOR_SECOND_KEY) &&
                    material.HasProperty(Pattern.COLOR_THIRD_KEY))
                {
                    result.Add(material);
                }
            }
        }

        return result;
    }

    private Material GetPaintableMaterial()
    {
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(true);

        foreach (MeshRenderer renderer in renderers)
        {
            if (renderer.transform.name.Contains("_LOD") && !renderer.transform.name.Contains("_LOD0"))
                continue;

            foreach (Material material in renderer.materials)
            {
                if (material != null &&
                    material.HasProperty(Pattern.COLOR_FIRST_KEY) &&
                    material.HasProperty(Pattern.COLOR_SECOND_KEY) &&
                    material.HasProperty(Pattern.COLOR_THIRD_KEY))
                {
                    return material;
                }
            }
        }

        return null;
    }
}
