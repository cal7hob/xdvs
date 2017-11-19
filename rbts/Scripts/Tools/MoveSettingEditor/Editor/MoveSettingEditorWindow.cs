using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class MoveSettingEditorWindow : EditorWindow
{
    protected static MoveSettingEditorWindow window;
    protected static string nameEditor = "Move Setting Editor";
    protected AESerializedObject SOSettings;

    [MenuItem("Tools/Move Setting Editor")]
    public static void Init()
    {
        window = GetWindow<MoveSettingEditorWindow>(nameEditor);
        window.minSize = new Vector2(310, 100); //width height
        window.Load();
    }

    void OnDestroy() { PrefabApply(); }

    public void Load()
    {
        string settingPath = AEEditorTools.GetDirectoryCurretScriptShort();
        settingPath = settingPath.Substring(0, settingPath.LastIndexOf('/')); //Path.DirectorySeparatorChar
        settingPath += "/MoveSettingEditorSettings";
        SOSettings = new AESerializedObject(AEEditorTools.LoadAsset<MoveSettingEditorSettings>(settingPath)); //Resources
        if (SOSettings["methods"].count == 0)
        {
            SOSettings["methods"].Add("InitOtherComponents");
        }
        SubscribeHandlerSettings();
    }

    private void PrefabApply() { if (SOSettings != null) SOSettings.PrefabApply(); }
    
    public virtual void OnGUI()
    {
        if (window == null) Init();
        AERectPosition rect = new AERectPosition((int)window.position.width);
        if (SOSettings == null) return;
        SOSettings.ReInit(rect);

        if (GUI.Button(rect.NextLine(100), "Copy")) ButtonCopy();
        if (GUI.Button(rect.NextLine(100), "Init")) ButtonInit();

        SOSettings.PropertyFieldChilds(true, true, false);
    }

    protected void ButtonCopy()
    {
        MonoScript MSSource = SOSettings["source"].GetValue<MonoScript>(), MSTarget = SOSettings["target"].GetValue<MonoScript>();
        if (MSSource == null) { EditorUtility.DisplayDialog(nameEditor, "Not source type", "Ok"); return; }
        if (MSTarget == null) { EditorUtility.DisplayDialog(nameEditor, "Not target type", "Ok"); return; }
        Type typeSource = MSSource.GetClass(), typeTarget = MSTarget.GetClass();
        string[] guids = AssetDatabase.FindAssets("t:prefab", SOSettings["searchPath"].stringValue.Split(';')); //new string[] { SOSettings["searchPath"].stringValue }
        int count = guids.Length, countProcessed = 0;
        bool testOnOnePrefab = SOSettings["testOnOnePrefab"].boolValue;
        float index = 0, progress = 0;
        string path;
        UnityEngine.Object OSource;
        MonoBehaviour MBSource;
        AESerializedObject SOTarget;
        foreach (string guid in guids)//for (int i = 0; i < guids.Length; i++)
        {
            path = AssetDatabase.GUIDToAssetPath(guid);
            progress = index / count;
            if (EditorUtility.DisplayCancelableProgressBar("Edit progress " + countProcessed, Mathf.Round(progress * 100) + "% " + index + "\\" + count + " " + AEEditorTools.GetName(path), progress))
            {
                EditorUtility.ClearProgressBar();
                return;
            }

            if ((OSource = AssetDatabase.LoadAssetAtPath(path, typeSource)) != null)
            {
                //CL.Log(DebugSource.Editor, OSource.name, OSource);
                MBSource = (MonoBehaviour)OSource;
                SOTarget = new AESerializedObject(MBSource.gameObject.GetComponent(typeTarget) ?? MBSource.gameObject.AddComponent(typeTarget));
                new AESerializedObject(OSource).CloneProperty(SOTarget);
                SOTarget.PrefabApply();
                countProcessed++;
                if (testOnOnePrefab)
                {
                    EditorUtility.ClearProgressBar();
                    return;
                }
            }
            index++;
        }

        EditorUtility.ClearProgressBar();
        EditorUtility.DisplayDialog(nameEditor, "Edit all prefab", "Ok");
    }

    protected void ButtonInit()
    {
        MonoScript MSSource = SOSettings["source"].GetValue<MonoScript>();
        if (MSSource == null) { EditorUtility.DisplayDialog(nameEditor, "Not source type", "Ok"); return; }
        Type typeSource = MSSource.GetClass();//, typeTarget = MSTarget.GetClass();
        string[] guids = AssetDatabase.FindAssets("t:prefab", SOSettings["searchPath"].stringValue.Split(';')); //new string[] { SOSettings["searchPath"].stringValue }
        int count = guids.Length, countProcessed = 0;
        bool testOnOnePrefab = SOSettings["testOnOnePrefab"].boolValue;
        float index = 0, progress = 0;
        string path;
        UnityEngine.Object OSource;

        List<MethodInfo> methods = new List<MethodInfo>();
        MethodInfo method;
        foreach (string methodName in SOSettings["methods"].GetValue<List<string>>())
        {
            if ((method = typeSource.GetMethod(methodName)) == null)
            {
                if (EditorUtility.DisplayDialog(nameEditor, "Not method " + methodName + " continue?", "Yes", "Not")) return;
            }
            else
            {
                methods.Add(method);
            }
        }

        foreach (string guid in guids)//for (int i = 0; i < guids.Length; i++)
        {
            path = AssetDatabase.GUIDToAssetPath(guid);
            progress = index / count;
            if (EditorUtility.DisplayCancelableProgressBar("Edit progress " + countProcessed, Mathf.Round(progress * 100) + "% " + index + "\\" + count + " " + AEEditorTools.GetName(path), progress))
            {
                EditorUtility.ClearProgressBar();
                return;
            }

            if ((OSource = AssetDatabase.LoadAssetAtPath(path, typeSource)) != null)
            {
                foreach (MethodInfo methodInfo in methods)
                {
                    methodInfo.Invoke(OSource, null);
                }
                EditorUtility.SetDirty(OSource);
                countProcessed++;
                if (testOnOnePrefab)
                {
                    EditorUtility.ClearProgressBar();
                    return;
                }
            }
            index++;
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog(nameEditor, "Edit all prefabs", "Ok");
    }
    
    void SubscribeHandlerSettings()
    {
        SOSettings.changeEvent += ChangeSet;
    }

    void ChangeSet(AESerializedProperty property) { PrefabApply(); }
}