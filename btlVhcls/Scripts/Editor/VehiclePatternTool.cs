using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class VehiclePatternTool : EditorWindow
{
    public class CamouflageSettings
    {
        public Texture texture;
        public Color colorFirst;
        public Color colorSecond;
        public Color colorThird;
        public Vector2 defaultScale;
        public Vector2 customScale;

        public static CamouflageSettings Parse(GameObject gameObject)
        {
            Material camoMaterial = FindCamoMaterial(gameObject);

            if (camoMaterial == null)
                return null;

            CamouflageSettings result = new CamouflageSettings();

            result.texture = camoMaterial.GetTexture(Pattern.MASK_TEX_KEY);
            result.colorFirst = camoMaterial.GetColor(Pattern.COLOR_FIRST_KEY);
            result.colorSecond = camoMaterial.GetColor(Pattern.COLOR_SECOND_KEY);
            result.colorThird = camoMaterial.GetColor(Pattern.COLOR_THIRD_KEY);
            result.defaultScale = camoMaterial.GetTextureScale(Pattern.MASK_TEX_KEY);
            result.customScale = result.defaultScale;

            return result;
        }

        public static List<Material> FindCamoMaterials(GameObject gameObject)
        {
            List<Material> result = new List<Material>();

            Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>();

            bool collectShared = !Application.isPlaying;

            foreach (Renderer rend in renderers)
            {
                foreach (Material material in collectShared ? rend.sharedMaterials : rend.materials)
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

            return result.Count == 0 ? null : result;
        }

        public static Material FindCamoMaterial(GameObject gameObject)
        {
            List<Material> materials = FindCamoMaterials(gameObject);

            if (materials != null)
                return materials[0];

            return null;
        }
    }

    private const string RESOURCES_FOLDER_PATH = "Assets/Resources/";
    private const string FILENAME_PREFIX = "CamoPattern_";
    private const string CAMO_FOLDER_PATH = "Camouflages/";

    private bool customScalingEnabled;
    private int patternId;
    private int vehicleId;
    private int currentShowPatternId;
    private Vector2 scrollPosition;
    private Vector2 defaultScale;
    private Vector2 customScale;
    private Texture texture;
    private Color colorFirst;
    private Color colorSecond;
    private Color colorThird;
    private GameObject activeGO;
    private CamouflageSettings camoSettings;
    private List<Material> selectionMaterials;

    private static string EntitiesResourcesFolderPath
    {
        get { return GameManager.CurrentResourcesFolder + "/Entities/"; }
    }

    void OnEnable()
    {
        Selection.selectionChanged += OnSelectionChanged;
    }

    void OnDisable()
    {
        Selection.selectionChanged -= OnSelectionChanged;
    }

    void OnGUI()
    {
        DrawGUI();
    }

    void OnInspectorUpdate()
    {
        Repaint();
    }

    [MenuItem(
        itemName:           "GameObject/Camouflage/Camouflage Editor",
        isValidateFunction: false,
        priority:           0)]
    public static void SaveAs()
    {
        VehiclePatternTool window = new VehiclePatternTool();

        window.titleContent = new GUIContent("Редактор камуфляжей");
        window.minSize = new Vector2(355, 340);
        window.maxSize = new Vector2(355, 340);

        window.ShowUtility();
        window.OnSelectionChanged();
    }

    [MenuItem(
        itemName:           "GameObject/Camouflage/Convert to entity",
        isValidateFunction: false,
        priority:           0)]
    public static void ConvertToEntity(MenuCommand menuCommand)
    {
        if (Selection.objects.Length > 1 && menuCommand.context != Selection.objects[0])
            return;

        foreach (GameObject gameObject in Selection.gameObjects)
            ConvertToEntity(gameObject.GetComponent<PatternInEditor>());
    }

    private static void ConvertToEntity(PatternInEditor patternInEditor)
    {
        string camoResourcePath = EntitiesResourcesFolderPath + CAMO_FOLDER_PATH + FILENAME_PREFIX + patternInEditor.ParsedId;
        PatternEntity patternEntity = Resources.Load<PatternEntity>(camoResourcePath);

        if (patternEntity == null)
        {
            patternEntity = CreateInstance<PatternEntity>();
            HelpTools.CreateFolderRecursively(RESOURCES_FOLDER_PATH + EntitiesResourcesFolderPath + CAMO_FOLDER_PATH);
            AssetDatabase.CreateAsset(patternEntity, RESOURCES_FOLDER_PATH + camoResourcePath + ".asset");
        }

        patternEntity.SetTexture(patternInEditor.textureMask);
        patternEntity.scale = patternInEditor.scale;

        patternEntity.colors = new List<PatternEntity.PropertyKeyColorPair>();
        patternEntity.scales = new List<PatternEntity.VehicleMaskScalePair>();

        foreach (var keyColorPair in patternInEditor.colors)
            patternEntity.colors.Add(new PatternEntity.PropertyKeyColorPair(keyColorPair.propertyKey, keyColorPair.color));

        foreach (var tankMaskScalePair in patternInEditor.scales)
            patternEntity.scales.Add(new PatternEntity.VehicleMaskScalePair { vehicleId = (int)tankMaskScalePair.tank, scale = tankMaskScalePair.scale });

        EditorUtility.SetDirty(patternEntity);
        EditorGUIUtility.PingObject(patternEntity);
    }

    private static int ParseVehicleId(GameObject activeGameObject)
    {
        if (activeGameObject == null)
            return 0;

        string idString = Regex.Match(activeGameObject.name, @"^_?(\d+)_").Groups[1].ToString();
        return idString.Length > 0 ? int.Parse(idString) : 0;
    }

    private void OnSelectionChanged()
    {
        if (Selection.activeGameObject == null)
            return;

        activeGO = Selection.activeGameObject.transform.root.gameObject;
        camoSettings = CamouflageSettings.Parse(activeGO);

        if (camoSettings != null)
            UpdateProperties();

        selectionMaterials = CamouflageSettings.FindCamoMaterials(activeGO);
    }

    private void DrawGUI()
    {
        if (camoSettings == null)
        {
            EditorGUILayout.LabelField("ПОДХОДЯЩИЕ МАТЕРИАЛЫ НЕ НАЙДЕНЫ");
            return;
        }

        vehicleId = ParseVehicleId(activeGO);

        EditorGUIUtility.labelWidth = 200.0f;

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        patternId = EditorGUILayout.IntField("ID камуфляжа", patternId);

        EditorGUILayout.Space();

        defaultScale = EditorGUILayout.Vector2Field("Тайлинг по умолчанию", defaultScale);

        EditorGUILayout.Space();

        customScalingEnabled = EditorGUILayout.Toggle("Для конкретной машины", customScalingEnabled);

        if (customScalingEnabled)
        {
            EditorGUILayout.LabelField("ID машины: " + vehicleId);
            customScale = EditorGUILayout.Vector2Field("Тайлинг", customScale);
        }

        EditorGUILayout.Space();

        texture = (Texture)EditorGUILayout.ObjectField("Текстура", texture, typeof(Texture), false);

        EditorGUILayout.Space();

        colorFirst = EditorGUILayout.ColorField("Цвет 1", colorFirst);
        colorSecond = EditorGUILayout.ColorField("Цвет 2", colorSecond);
        colorThird = EditorGUILayout.ColorField("Цвет 3", colorThird);

        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Сохранить"))
            Save();

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();

        EditorGUIUtility.labelWidth = 130.0f;

        EditorGUILayout.LabelField("Просмотр сохранённых:");

        EditorGUIUtility.labelWidth = 25.0f;

        EditorGUILayout.LabelField(currentShowPatternId.ToString());

        if (GUILayout.Button("◄"))
            LoadPattern(-1);

        if (GUILayout.Button("►"))
            LoadPattern(1);

        EditorGUILayout.EndHorizontal();

        UpdateCachedSettings();
        UpdateMaterials();
    }

    private void UpdateProperties()
    {
        texture = camoSettings.texture;
        colorFirst = camoSettings.colorFirst;
        colorSecond = camoSettings.colorSecond;
        colorThird = camoSettings.colorThird;

        if (customScalingEnabled)
        {
            defaultScale = Vector2.one;
            customScale = camoSettings.customScale;
        }
        else
        {
            defaultScale = camoSettings.defaultScale;
            customScale = Vector2.one;
        }
    }

    private void UpdateCachedSettings()
    {
        camoSettings.texture = texture;
        camoSettings.colorFirst = colorFirst;
        camoSettings.colorSecond = colorSecond;
        camoSettings.colorThird = colorThird;
        camoSettings.defaultScale = defaultScale;
        camoSettings.customScale = customScale;
    }

    private void UpdateMaterials()
    {
        if (selectionMaterials == null)
            return;

        foreach (Material material in selectionMaterials)
        {
            material.SetTexture(Pattern.MASK_TEX_KEY, camoSettings.texture);
            material.SetColor(Pattern.COLOR_FIRST_KEY, camoSettings.colorFirst);
            material.SetColor(Pattern.COLOR_SECOND_KEY, camoSettings.colorSecond);
            material.SetColor(Pattern.COLOR_THIRD_KEY, camoSettings.colorThird);

            if (customScalingEnabled)
                material.SetTextureScale(Pattern.MASK_TEX_KEY, camoSettings.customScale);
            else
                material.SetTextureScale(Pattern.MASK_TEX_KEY, camoSettings.defaultScale);
        }
    }

    private void Save()
    {
        string camoResourcePath = EntitiesResourcesFolderPath + CAMO_FOLDER_PATH + FILENAME_PREFIX + patternId;
        PatternEntity patternEntity = Resources.Load<PatternEntity>(camoResourcePath);

        if (patternEntity == null)
        {
            patternEntity = CreateInstance<PatternEntity>();
            HelpTools.CreateFolderRecursively(RESOURCES_FOLDER_PATH + EntitiesResourcesFolderPath + CAMO_FOLDER_PATH);
            AssetDatabase.CreateAsset(patternEntity, RESOURCES_FOLDER_PATH + camoResourcePath + ".asset");
            EditorGUIUtility.PingObject(patternEntity);
        }

        patternEntity.SetTexture(camoSettings.texture);
        patternEntity.scale = defaultScale;

        patternEntity.colors = new List<PatternEntity.PropertyKeyColorPair>
        {
            new PatternEntity.PropertyKeyColorPair(Pattern.COLOR_FIRST_KEY, colorFirst),
            new PatternEntity.PropertyKeyColorPair(Pattern.COLOR_SECOND_KEY, colorSecond),
            new PatternEntity.PropertyKeyColorPair(Pattern.COLOR_THIRD_KEY, colorThird)
        };

        PatternEntity.VehicleMaskScalePair sameCustomScale = patternEntity.scales.FirstOrDefault(pair => pair.vehicleId == vehicleId);

        if (sameCustomScale != null)
            patternEntity.scales.Remove(sameCustomScale);

        if (customScalingEnabled)
            patternEntity.scales.Add(new PatternEntity.VehicleMaskScalePair { scale = customScale, vehicleId = vehicleId });

        EditorUtility.SetDirty(patternEntity);
        EditorGUIUtility.PingObject(patternEntity);
    }

    private void LoadPattern(int offset)
    {
        PatternEntity[] patternEntities = Resources.LoadAll<PatternEntity>(EntitiesResourcesFolderPath + CAMO_FOLDER_PATH);

        currentShowPatternId += offset;

        if (currentShowPatternId > patternEntities.Length)
            currentShowPatternId = 1;

        if (currentShowPatternId <= 0)
            currentShowPatternId = patternEntities.Length;

        PatternEntity currentPatternEntity = null;

        foreach (PatternEntity patternEntity in patternEntities)
        {
            if (patternEntity.ParsedId == currentShowPatternId)
            {
                currentPatternEntity = patternEntity;
                break;
            }
        }

        if (currentPatternEntity != null)
        {
            DrawCamouflage(activeGO, currentPatternEntity);
            patternId = currentShowPatternId;
            defaultScale = currentPatternEntity.scale;

            customScalingEnabled = false;

            foreach (PatternEntity.VehicleMaskScalePair vehicleMaskScalePair in currentPatternEntity.scales)
            {
                if (vehicleMaskScalePair.vehicleId == vehicleId)
                {
                    customScale = vehicleMaskScalePair.scale;
                    customScalingEnabled = true;
                    break;
                }
            }
        }

        camoSettings = CamouflageSettings.Parse(activeGO);
        UpdateProperties();
    }

    private void DrawCamouflage(GameObject gameObject, PatternEntity patternEntity)
    {
        List<Material> paintableMaterials = CamouflageSettings.FindCamoMaterials(gameObject);

        if (paintableMaterials == null)
            return;

        foreach (Material material in paintableMaterials)
        {
            material.SetTexture(Pattern.MASK_TEX_KEY, patternEntity.LoadTextureMask());

            foreach (var propertyKeyColorPair in patternEntity.colors)
                material.SetColor(propertyKeyColorPair.propertyKey, propertyKeyColorPair.color);

            material.SetTextureScale(Pattern.MASK_TEX_KEY, patternEntity.scale);

            foreach (PatternEntity.VehicleMaskScalePair vehicleMaskScalePair in patternEntity.scales)
            {
                if (vehicleMaskScalePair.vehicleId == vehicleId)
                {
                    material.SetTextureScale(Pattern.MASK_TEX_KEY, vehicleMaskScalePair.scale);
                    break;
                }
            }
        }
    }
}
