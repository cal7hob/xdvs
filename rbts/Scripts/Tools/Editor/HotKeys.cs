//*****************************************************************************************************************************
//HotKeys
//Help Add hot key

//F12           [MenuItem("Tools/Hotkeys/Version Control _F12")]
//Ctrl+U        [MenuItem("Tools/Hotkeys/Version Control %u")]
//Alt+A         [MenuItem("Tools/Hotkeys/Version Control &a")]
//Shift+Q       [MenuItem("Tools/Hotkeys/Version Control #q")]
//Ctrl+Shift+U  [MenuItem("Tools/Hotkeys/Version Control %#u")]
//Shift+F3      [MenuItem("Tools/Hotkeys/Version Control #F3")]

//Help Menu popup
//[MenuItem("Assets/Tools/Revert Prefabs", false, 1)] menu popup on window Project
//[MenuItem("GameObject/Tools/Revert Prefabs", false, 1)] menu popup on window Hierorchy

//HotKeys (Play, Create Folder) http://unity3d.ru/distribution/viewtopic.php?f=69&t=4444
//http://docs.unity3d.com/ScriptReference/MenuItem.html

//Command for start unity3d and update http://docs.unity3d.com/Manual/CommandLineArguments.html
//"C:\Program Files\Unity\Editor\Unity.exe" -executeMethod UpdateManager.VersionControlUpdate -projectPath D:\Projects\JavelinStrike5
//*****************************************************************************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
//using UnityEngine.SceneManagement;

public class HotKeys : EditorWindow
{
//=============================================================================================================================
//Event
//=============================================================================================================================
    private static HotKeys window;
    public static event Action exitEditor;
    public static event Action saveInProjectWindow;

    /*public class Setting : AEEditorTools.Setting<Setting>
    {
        public bool onCompileSwitchEnglish = false;
        public string openScene = "";

        public static void InitPath()
        {
            fileSetting = "HotKeysSetting";
        }
    }

    [MenuItem("Tools/Hotkeys/Settings")]
    public static void Init()
    {
        window = GetWindow<HotKeys>("HotKeys Setting");
        window.minSize = new Vector2(310, 50); //width height
        window.Load();
    }

    public void Load()
    {
        Setting.Load();
    }

    void OnDestroy()
    {
        Setting.SetModified();
    }

    void OnGUI()
    {
        if (window == null) Init();
        AERectPosition rect = new AERectPosition((int)window.position.width);
        Setting.SOSetting.ReInit(rect);
        rect.startY -= rect.height;
        Setting.SOSetting.PropertyFieldChilds(true, true, false);
    }

//=============================================================================================================================
//Open Scene
//=============================================================================================================================
    //Ctrl+I
    [MenuItem("Tools/Hotkeys/Scens/Open Scene %i")]
    public static void OpenScene()
    {
        EditorSceneManager.OpenScene(Setting.Get().openScene);//"Assets\\_Scenes\\WorkScenes\\4_MoonValley\\scn_b_MoonValley_Act_3.unity"
        //EditorSceneManager.OpenScene("Assets\\_Scenes\\WorkScenes\\4_MoonValley\\scn_b_MoonValley_Act_3.unity", UnityEditor.SceneManagement.OpenSceneMode.Additive);
    }

    //Alt+M
    [MenuItem("Tools/Hotkeys/Scens/Open Scene Start &m")] //f
    static void OpenSceneStart()
    {
        EditorSceneManager.OpenScene(EditorBuildSettings.scenes[0].path);
    }*/

//=============================================================================================================================
//Prefabs
//=============================================================================================================================
    [MenuItem("GameObject/Prefab/Revert Name On Prefab", false, 1)]
    static void RevertNameOnPrefab()
    {
        GameObject go;
        foreach (UnityEngine.Object obj in Selection.objects)
        {
            go = (GameObject)PrefabUtility.GetPrefabParent(obj);
            if (go != null) ((GameObject)obj).name = go.name;
        }
    }

    //F10
    [MenuItem("GameObject/Prefab/Revert", false, 2)]
    static void RevertPrefabs()
    {
        foreach (UnityEngine.Object obj in Selection.objects) PrefabUtility.RevertPrefabInstance((GameObject)obj);
    }

    [MenuItem("GameObject/Prefab/Apply", false, 3)]
    static void ApplyPrefabs()
    {
        UnityEngine.Object objPrefab;
        foreach (UnityEngine.Object obj in Selection.objects)
        {
            objPrefab = PrefabUtility.GetPrefabParent(obj);
            if (objPrefab != null) PrefabUtility.ReplacePrefab((GameObject)obj, objPrefab);
        }
    }

    [MenuItem("GameObject/Select Childs", false, 1)]
    static void SelectChilds()
    {
        List<GameObject> result = new List<GameObject>();
        foreach (GameObject go in Selection.gameObjects)
        {
            foreach (Transform TChild in go.transform)
            {
                result.Add(TChild.gameObject);
            }
        }
        Selection.objects = result.ToArray();
    }

    [MenuItem("GameObject/Select All Nested Childs", false, 1)]
    static void SelectAllNestedChilds()
    {
        List<GameObject> result = new List<GameObject>();
        foreach (GameObject go in Selection.gameObjects)
        {
            SelectAllNestedChilds_(go.transform, result);
        }
        Selection.objects = result.ToArray();
    }

    private static void SelectAllNestedChilds_(Transform transform, List<GameObject> result)
    {
        foreach (Transform TChild in transform)
        {
            result.Add(TChild.gameObject);
            SelectAllNestedChilds_(TChild, result);
        }
    }

    [MenuItem("Tools/Hotkeys/Find Materials In Scene")]
    static void FindMaterialsInScene()
    {
        string path;
        Type type = typeof(Material);
        foreach (UnityEngine.Object m in Resources.FindObjectsOfTypeAll(type)) // GameObject.FindObjectsOfType<Material>()
        {
            if (!result.Contains(path = AssetDatabase.GetAssetPath(m)))
            {
                result.Add(path);
            }
        }

        PrintResult();
    }

    [MenuItem("Tools/Hotkeys/Find Meshes In Scene")]
    static void FindMeshesInScene()
    {
        string path;
        foreach (MeshFilter m in AEEditorTools.FindObjectsOfType<MeshFilter>())
        {
            if (!result.Contains(path = AssetDatabase.GetAssetPath(m.sharedMesh))) //.sharedMesh
            {
                result.Add(path);
            }
        }

        PrintResult();
    }

    static List<string> result = new List<string>();
    [MenuItem("Tools/Hotkeys/Find Prefabs In Scene")]
    static void FindPrefabsInScene()
    {
        foreach (GameObject go in AEEditorTools.SceneRoots())
        {
            FindPrefabs(go.transform);
        }

        PrintResult();
    }

    private static void FindPrefabs(Transform t)
    {
        UnityEngine.Object objPrefab;
        if ((objPrefab = PrefabUtility.GetPrefabParent(t)) != null)
        {
            string path;
            if (!result.Contains(path = AssetDatabase.GetAssetPath(objPrefab)))
            {
                result.Add(path);
                return;
            }
        }
        foreach (Transform child in t)
        {
            FindPrefabs(child);
        }
    }

    private static void PrintResult()
    {
        result.Sort();
        result.Clear();
    }

//=============================================================================================================================
//Play, Pause...
//=============================================================================================================================
    //F11
    [MenuItem("Tools/Hotkeys/Play _F11")]
    static void Play()
    {
        EditorApplication.ExecuteMenuItem("Edit/Play");
    }

    //F12
    [MenuItem("Tools/Hotkeys/Pause _F12")]
    static void Pause()
    {
        EditorApplication.ExecuteMenuItem("Edit/Pause");
    }

    [MenuItem("Tools/Hotkeys/Quality %&q")]
    static void Quality()
    {
        EditorApplication.ExecuteMenuItem("Edit/Project Settings/Quality");
    }

//=============================================================================================================================
//Get Property On Select Window Ctrl+G
//=============================================================================================================================
    //Ctrl+G
    [MenuItem("Tools/Hotkeys/Get Property On Select Window _F9")] //, priority = 1 %g
    static void GetPropertySelectWindow()
    {
        Type type = focusedWindow.GetType();
        GetInfoType(type);
    }

    public static void GetInfoType(Type type)
    {
        string param;
        foreach (MethodInfo methodInfo in type.GetMethods())
        {
            param = "(";
            foreach (ParameterInfo parameterInfo in methodInfo.GetParameters())
            {
                param += parameterInfo.ParameterType + " " + parameterInfo.Name + ", ";
            }
            param += ")";
        }
    }

//=============================================================================================================================
//Edit Ctrl+K
//=============================================================================================================================
    [MenuItem("Tools/Hotkeys/Hotkeys Edit %k")]
    static void Edit()
    {
        UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(AEEditorTools.GetPathToCurretScript(), 32);
        /*ProcessStartInfo processStartInfo = new ProcessStartInfo("Assets\\_Scripts\\Editor\\JNUnitEditorWindow\\Debug\\VSIDEApi.exe", new FileInfo("Assets\\_Scripts\\Editor\\JNUnitEditorWindow\\Debug\\HotKeys.cs").FullName + " 31");
        processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
        Process.Start(processStartInfo);*/
    }

//=============================================================================================================================
//Window
//=============================================================================================================================
    //Ctrl+Shift+Z
    [MenuItem("Tools/Hotkeys/Window/Undo Change Select Window %#z")]
    static void UndoChange()
    {
        MethodInfo methodInfo = EditorWindow.focusedWindow.GetType().GetMethod("PerformUndo");
        if (methodInfo != null)
        {
            methodInfo.Invoke(EditorWindow.focusedWindow, new object[0]);
            return;
        }
        Undo.PerformUndo();
    }

    //Alt+S
    [MenuItem("Tools/Hotkeys/Window/Save &s")]
    static void SaveChange()
    {
        MethodInfo methodInfo = EditorWindow.focusedWindow.GetType().GetMethod("PrefabApply");
        if (methodInfo != null)
        {
            methodInfo.Invoke(EditorWindow.focusedWindow, new object[0]);
            return;
        }
        else
        {
            if (EditorWindow.focusedWindow.titleContent.text == "Project")
            {
                if (saveInProjectWindow != null) saveInProjectWindow();
                return;
            }
        }
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
    }

    //Alt+U Ctrl+Shift+U %#u
    [MenuItem("Tools/Hotkeys/Window/Update &u")]
    static void Update()
    {
        MethodInfo methodInfo = EditorWindow.focusedWindow.GetType().GetMethod("Load");
        if (methodInfo != null) methodInfo.Invoke(EditorWindow.focusedWindow, new object[0]);
    }

    //Alt+F4
    [MenuItem("Tools/Hotkeys/Window/Close Window &F4")]
    static void Close_()
    {
        string windowTitle = Window.GetCurrentTitle();
        if (windowTitle != null && windowTitle.StartsWith("Unity"))
        {
            if (exitEditor != null) exitEditor();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            EditorApplication.Exit(0);
            return;
        }

        EditorWindow window = EditorWindow.focusedWindow;
        if (window != null) window.Close();
    }

    //[InitializeOnLoad]
    /*public class FixHotKeys : ScriptableObject//UnityEngine.Object
    {
        static FixHotKeys()
        {
            if ((DateTime.Now - System.Diagnostics.Process.GetCurrentProcess().StartTime).TotalSeconds < 100) Switch();
            Init();
        }

        //[UnityEditor.Callbacks.DidReloadScripts]
        public static void Init()
        {
            if (Setting.Get().onCompileSwitchEnglish) EventCompile.compile += delegate (bool on) { if (on) Switch(); };
            //int keyboardLayoutId = EditorTools.GetObjectIntTemp("keyboardLayoutId");
            //if (keyboardLayoutId != 0) { Keyboard.KeyboardLayout.SetCurrent(keyboardLayoutId); EditorTools.SetObjectTemp("keyboardLayoutId", 0); }
            //EventCompile.compile += delegate (bool on) { if (on && (keyboardLayoutId = Keyboard.KeyboardLayout.GetCurrent().KeyboardLayoutId) != 1033) { EditorTools.SetObjectTemp("keyboardLayoutId", keyboardLayoutId); Keyboard.KeyboardLayout.SetCurrent(1033); } }; //SwitchLanguage
        }

        public static void Switch()
        {
            if (Keyboard.KeyboardLayout.GetCurrent().KeyboardLayoutId != 1033) { Keyboard.KeyboardLayout.SetCurrent(1033); }
        }

        [MenuItem("Tools/Hotkeys/FixHotKeys _F10")]
        public static void SwitchImmediate()
        {
            Switch();
            Reimport(AEEditorTools.GetPathToCurretScript());
        }

        private static void Reimport(string path)
        {
            UnityEngine.Object[] objects = Selection.objects;
            Selection.activeInstanceID = GetInstanceIDFromGUID(GetGUID(path));
            EditorApplication.ExecuteMenuItem("Assets/Reimport");
            Selection.objects = objects;
        }

        private static string GetGUID(string path)
        {
            //return AssetDatabase.AssetPathToGUID(path); //not work
            path += ".meta";
            if (File.Exists(path))
            {
                string fileMeta = File.ReadAllText(path);
                string guid;
                int startIndex = 0;
                if (fileMeta.GetParam(out guid, ref startIndex, "guid: ", "\ntimeCreated:"))
                {
                    return guid;
                }
            }
            return "";
        }

        private static int GetInstanceIDFromGUID(string guid)
        {
            MethodInfo method = typeof(AssetDatabase).GetMethod("GetInstanceIDFromGUID" , System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            int result = (int)method.Invoke(null, new object[] { guid });
            //CL.Log("id " + result + " " + guid + " " + UnityEditor.Unsupported.GetLocalIdentifierInFile(result));
            return result;
        }
    }*/
}