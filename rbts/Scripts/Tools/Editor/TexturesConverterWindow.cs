using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class TexturesConverterWindow : EditorWindow
{
    public class Data
    {
        public string search = "";
        public TextureFormat format;// = TextureFormat.ARGB32;
        public TextureImporterFormat targetFormat;
        public string searchPath = "Assets";
    }

    public class TextureData
    {
        public TextureData() { }

        public TextureData(string guid, string path, string name, TextureFormat format)
        {
            this.guid = guid;
            this.path = path;
            this.name = name;
            nameLower = name.ToLower();
            this.format = format;
        }

        public string guid;
        public string path;
        public string name;
        public string nameLower;
        public TextureFormat format;

        public static bool GetTextureData(string guid, out TextureData textureData)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path == null)
            {
                textureData = null;
                return false;
            }

            TextureImporter texture = AssetImporter.GetAtPath(path) as TextureImporter;
            if (texture != null)
            {
                TextureFormat format;
                string name;

                TextureImporterCompression texImporterCompression = texture.textureCompression;
                TextureImporterFormat texImporterFormat;
                int maxTexSize = 0;

                texture.GetPlatformTextureSettings(EditorUserBuildSettings.activeBuildTarget.ToString(), out maxTexSize,
                    out texImporterFormat);

                if (!texImporterFormat.TryToEnum<TextureFormat>(out format))
                {
                    if (texImporterFormat == TextureImporterFormat.RGBA16)
                    {
                        format = TextureFormat.RGBA4444;
                        name = AEEditorTools.GetName(path);
                    }
                    else
                    {
                        Texture2D texture2D = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                        format = texture2D.format;
                        name = texture2D.name;
                    }
                }
                else
                {
                    name = AEEditorTools.GetName(path);
                }

                textureData = new TextureData(guid, path, name, format);
                return true;
            }

            textureData = null;
            return false;
        }

        public Texture2D GetTexture()
        {
            Texture2D result = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Refresh(result);
            return result;
        }

        private void Refresh(Texture2D texture)
        {
            if (format != texture.format) format = texture.format;
        }

        public void Convert(TextureImporterFormat format)
        {
            TextureImporter texture = AssetImporter.GetAtPath(path) as TextureImporter;
            if (texture != null)
            {
                texture.textureType = TextureImporterType.Default;
                texture.isReadable = true;

                var platformSettings = new TextureImporterPlatformSettings
                {
                    format = format,
                };
                
                texture.SetPlatformTextureSettings(platformSettings);
                AssetDatabase.ImportAsset(path);
                AssetDatabase.Refresh();
                if (!format.TryToEnum<TextureFormat>(out this.format))
                {
                    GetTexture(); //for refresh format
                }
            }
        }
        
        public bool Synchronize()
        {
            if (File.Exists(this.path)) return true;
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path == null)
            {
                return false;
            }

            if (path != this.path)
            {
                this.path = path;
                string name = AEEditorTools.GetName(path);
                if (name != this.name)
                {
                    this.name = name;
                    nameLower = name.ToLower();
                }
            }
            return true;
        }
    }

//=============================================================================================================================
//Values global
//=============================================================================================================================
    private static TexturesConverterWindow window;
    private Vector2 scrollBoxMain = Vector2.zero;
    private static Data setting;
    private static AESerializedObject SOSetting;
    private List<TextureData> paintTextures = new List<TextureData>();
    private List<TextureData> selects = new List<TextureData>();
    private Dictionary<string, TextureData> textures = new Dictionary<string, TextureData>();
    private bool inConvertProgress = false;
    private static string pathSetting = "EditorSetting" + Path.DirectorySeparatorChar + "TextureData.json";

    private GUIStyle label;
    private GUIStyle styleColor;
    private GUIStyle styleRichText;
    private int texturesCountOld = 0;

//=============================================================================================================================
//Init
//=============================================================================================================================
    [MenuItem("Tools/Converter Textures")]
    public static void Init()
    {
        window = GetWindow<TexturesConverterWindow>("Converter Textures");
        window.minSize = new Vector2(200, 200); //width height
        window.Load();
    }

    public void Load()
    {
        inConvertProgress = false;
        SOSetting = new AESerializedObject(GetSetting());
        SubscribeHandler();
        textures = AEEditorTools.LoadObjectJson<Dictionary<string, TextureData>>(pathSetting) ?? new Dictionary<string, TextureData>();
    }

    void InitStyle()
    {
        label = new GUIStyle(GUI.skin.GetStyle("label"));
        
        styleColor = new GUIStyle(label);

        styleRichText = new GUIStyle(label);
        styleRichText.richText = true;
    }

    public void Clear()
    {
        SOSetting["format"].value = 0;
        SOSetting["search"].value = "";
        SOSetting["searchPath"].value = "Assets";
        Find();
    }

    public static Data GetSetting()
    {
        if (setting == null)
        {
            setting = new Data();
        }
        return setting;
    }

    private void LoadData()
    {
        textures.Clear();
        paintTextures.Clear();
        TextureData itemTexture;
        string path = SOSetting["searchPath"].stringValue;

        float index = 0;
        float progress = 0;
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:texture2D", new string[] { path });
        int count = guids.Length;
        foreach (string guid in guids)
        {
            if (!TextureData.GetTextureData(guid, out itemTexture)) continue;
            progress = index / count;
            if (EditorUtility.DisplayCancelableProgressBar("Load data progress", Mathf.Round(progress * 100) + "% " + index + "\\" + count + " " + itemTexture.name, progress))
            {
                EditorUtility.ClearProgressBar();
                Load();
                return;
            }
            textures.Add(itemTexture.guid, itemTexture);
            index++;
        }
        EditorUtility.ClearProgressBar();
        if (path == "Assets") Set();
    }

    private void Synchronize()
    {
        TextureData item;
        string path = SOSetting["searchPath"].stringValue;

        List<string> removes = new List<string>();
        foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:texture2D", new string[] { path }))
        {
            if (textures.ContainsKey(guid))
            {
                if (!textures[guid].Synchronize())
                {
                    removes.Add(guid);
                }
                continue;
            }

            if (!TextureData.GetTextureData(guid, out item)) continue;
            textures.Add(item.guid, item);
        }

        if (removes.Count > 0) foreach (string guid in removes) textures.Remove(guid);

        if (path == "Assets") Set();
    }

    private void GetPath()
    {
        Object[] selectedAsset = Selection.GetFiltered(typeof(Object), SelectionMode.DeepAssets);
        if (selectedAsset.Length == 0) return;
        SOSetting["searchPath"].value = Path.GetDirectoryName(AssetDatabase.GetAssetPath(selectedAsset[0]));
    }

    private void Set()
    {
        AEEditorTools.SetJson(pathSetting, textures);
    }

    void Find()
    {
        TextureFormat format = SOSetting["format"].GetValue<TextureFormat>();
        bool isNotSelectFormat = format == 0;

        string search = SOSetting["search"].stringValue.ToLower();
        bool isNotSearchText = search == "";

        string path = SOSetting["searchPath"].stringValue;
        bool isRootPath = search == "Assets";

        selects.Clear();
        paintTextures.Clear();
        foreach (KeyValuePair<string, TextureData> item in textures)
        {
            if (!isNotSearchText && !item.Value.nameLower.Contains(search)) continue;
            if (!isRootPath && !item.Value.path.StartsWith(path)) continue;
            if (!isNotSelectFormat && item.Value.format != format) continue;
            paintTextures.Add(item.Value);
        }
    }
    
    private void Convert()
    {
        if (selects.Count == 0)
        {
            EditorUtility.DisplayDialog("Convert", "Not select textures", "Ok");
            return;
        }

        TextureImporterFormat format = SOSetting["targetFormat"].GetValue<TextureImporterFormat>();
        if (format == 0)
        {
            EditorUtility.DisplayDialog("Convert", "Not select target format", "Ok");
            return;
        }

        if (!EditorUtility.DisplayDialog("Convert", "Are you sure you want to convert " + selects.Count + " textures", "Yes", "Not")) return;

        inConvertProgress = true;
        float index = 0;
        float progress = 0;
        foreach (TextureData item in selects)
        {
            progress = index / selects.Count;
            if (EditorUtility.DisplayCancelableProgressBar("Convert progress", Mathf.Round(progress * 100) + "% " + index + "\\" + selects.Count + " " + item.name, progress))
            {
                ConvertCompleted();
                return;
            }
            item.Convert(format);
            index++;
        }
        ConvertCompleted();
        EditorUtility.DisplayDialog("Convert", "Converting completed", "Ok");
    }

    private void ConvertCompleted()
    {
        EditorUtility.ClearProgressBar();
        Set();
        inConvertProgress = false;
    }
    
    void Select(TextureData select)
    {
        if (inConvertProgress) return;
        if (selects.Contains(select))
        {
            selects.Remove(select);
        }
        else
        {
            selects.Add(select);
        }

        Selection.activeObject = select.GetTexture();
    }

//=============================================================================================================================
//OnGUI
//=============================================================================================================================
    void OnGUI()
    {
        if (window == null) Init();
        if (label == null) InitStyle();
        AERectPosition rect = new AERectPosition((int)window.position.width);
        if (SOSetting == null)
        {
            EditorGUI.LabelField(rect.Next(), "Not setting");
            return;
        }
        SOSetting.ReInit(rect);

//=============================================================================================================================
//Setting top bar
//=============================================================================================================================
        AERectPosition rectTop = rect.Clone();
        SOSetting.foldout = EditorGUI.Foldout(rectTop.NextLine(10, 20, rect.separator), SOSetting.foldout, "", true);
        if (SOSetting.foldout)
        {
            rect.startY += rect.separator;
            rect.Tab();
            SOSetting.PropertyFieldChilds(true, true);
            rect.TabEnd();
        }

        if (GUI.Button(rectTop.NextLine(90, 20), "Synchronize")) Synchronize();
        if (GUI.Button(rectTop.NextLine(45, 20), "Load")) LoadData();
        if (GUI.Button(rectTop.NextLine(45, 20), "Set")) Set();

        rectTop.NextLine(5, 20);
        if (GUI.Button(rectTop.NextLine(60, 20), "GetPath")) GetPath();
        if (GUI.Button(rectTop.NextLine(45, 20), "Clear")) Clear();
        //if (GUI.Button(rectTop.NextLine(45, 20), "Find")) Find();

        rectTop.NextLine(5, 20);
        if (GUI.Button(rectTop.NextLine(65, 20), "Deselect")) selects.Clear();
        if (GUI.Button(rectTop.NextLine(65, 20), "Select all")) selects = new List<TextureData>(paintTextures);
        if (GUI.Button(rectTop.NextLine(60, 20), "Convert")) Convert();
        //if (GUI.Button(rectTop.NextLine(60, 20), "Clear M")) GC.Collect(); //100000, GCCollectionMode.Forced

//=============================================================================================================================
//Print Textures
//=============================================================================================================================
        rectTop = rect.Clone();
        rectTop.Next((int)window.position.width - 10, (int)(window.position.height - rect.startY - 25));

        scrollBoxMain = GUI.BeginScrollView(rectTop.GetRect(), scrollBoxMain, new Rect(0, rect.NextNotSet().yMax - 20, 0, paintTextures.Count * rect.height));

        int width = (int)window.position.width - 10;
        int heightLineBox = rect.height - 3;
        AERectPosition rectBox = rect.Clone();
        rectBox.startY -= 2;
        rect.separator = 0;

        foreach (TextureData item in paintTextures)
        {
            GUI.Box(rectBox.Next(width, heightLineBox), "");
            if (selects.Contains(item)) GUI.Box(rectBox.GetRect(), "");
            if (GUI.Button(rectBox.GetRect(), "", styleColor))
            {
                Select(item);
            }

            rect.Next();
            EditorGUI.LabelField(rect.NextLine(), item.name); //width  //+ " " + item.format // + " " + item.guid + " " + item.path
            EditorGUI.LabelField(rect.NextLine(), item.format.ToString());
        }

        if (texturesCountOld != (texturesCountOld = textures.Count))
        {
            scrollBoxMain.y = rect.startY;
        }

        GUI.EndScrollView();
    }

//=============================================================================================================================
//Subscribe
//=============================================================================================================================
    void SubscribeHandler()
    {
        SOSetting.SubscribeChangeProperty(Find, SOSetting["format"]);
        SOSetting.SubscribeChangeProperty(Find, SOSetting["search"]);
        SOSetting.SubscribeChangeProperty(Find, SOSetting["searchPath"]);
    }

    void Find(AESerializedProperty property) { Find(); }
}
