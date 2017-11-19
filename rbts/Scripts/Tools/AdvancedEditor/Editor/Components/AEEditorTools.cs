using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Reflection;
using Newtonsoft.Json;//.JsonEditor; //.JsonNET

public static class AEEditorTools //PSYTools SITools
{
    /*public delegate void Subscribe(AESerializedObject serializedObject);

    public static AESerializedObject GetResources<T>(string path, Subscribe subscribe = null, Subscribe subscribeParser = null) where T : Component
    {
        // subscribe, using, time not using
        T obj = LoadResources<T>(path);
        AESerializedObject resources = AEResourcesDictionary.instance[path];
        if (resources == null || resources.value != obj)
        {
            //Debug.Log("resources.value != obj " + path);
            if (subscribeParser == null)
            {
                resources = new AESerializedObject(obj);
            }
            else
            {
                resources = new AESerializedObject(obj, false);
                subscribeParser(resources);
                resources.Parse();
            }
            if (subscribe != null) subscribe(resources);
            //resources.Parse();

            AEResourcesDictionary.instance[path] = resources;
        }
        else
        {
            resources.Update(); //or resources.PrefabApply();
        }

        return resources;
    }*/

    /*public abstract class Setting_<T> where T : Setting_<T>
    {
        [NonSerialized]
        protected static string pathSetting = "Library" + Path.DirectorySeparatorChar + "EditorSetting" + Path.DirectorySeparatorChar;
        [NonSerialized]
        protected static T single;

        public virtual void Init()
        {
            Set();
        }

        public virtual void Clear()
        {
            (single = Activator.CreateInstance<T>()).Init();
        }

        public static T Get()
        {
            if (single == null)
            {
                typeof(T)
                    #if NETFX_CORE
                    .GetTypeInfo()
                    #endif
                    .GetMethod("InitPath").Invoke(null, new object[0]);
                single = AETools.LoadObjectJson<T>(pathSetting);
                if (single == null)
                {
                    single = Activator.CreateInstance<T>();
                    single.Init();
                }
            }
            return single;
        }

        public static void Set()
        {
            AETools.SetJson(pathSetting, single);
        }
    }*/

    /*public abstract class Setting<T> : AETools.Setting<T> where T : Setting<T>
    {
        [NonSerialized] public static AESerializedObject SOSetting;
        
        public static void Load()
        {
            SOSetting = new AESerializedObject(Get());
            SOSetting.changeEvent += Set;
        }

        private static void Set(AESerializedProperty property)
        {
            Set();
        }

        public static void SetModified()
        {
            if (SOSetting.GetIsModified())
            {
                //SetJson(pathSetting, single);
                Set();
            }
        }
    }*/

    public static T LoadResources<T>(string path) where T : Component
    {
        GameObject obj = Resources.Load<GameObject>(path);

        T result;
        if (obj == null)
        {
            GameObject newObj = new GameObject();
            result = newObj.AddComponent<T>();
            obj = PrefabUtility.CreatePrefab(path + ".prefab", newObj, ReplacePrefabOptions.ConnectToPrefab);
            UnityEngine.Object.DestroyImmediate(newObj);
            AssetDatabase.Refresh();
        }
        else
        {
            result = obj.GetComponent<T>();
            if (result == null)
            {
                result = obj.AddComponent<T>();
                EditorUtility.SetDirty((UnityEngine.Object)result);
                AssetDatabase.SaveAssets();
            }
        }
        return result;
    }

    public static T LoadAssetResources<T>(string path) where T : Component
    {
        path += ".prefab";
        GameObject obj;
        T result;
        if (!File.Exists(path)) //obj == null
        {
            GameObject newObj = new GameObject();
            newObj.AddComponent<T>(); //result = 
            obj = PrefabUtility.CreatePrefab(path, newObj, ReplacePrefabOptions.ConnectToPrefab);
            UnityEngine.Object.DestroyImmediate(newObj);
            AssetDatabase.Refresh();
        }
        else
        {
            obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        //if (obj == null) CL.Log(DebugSource.Editor, "obj == null");
        result = obj.GetComponent<T>();
        if (result == null)
        {
            result = obj.AddComponent<T>();
            EditorUtility.SetDirty((UnityEngine.Object)result);
            AssetDatabase.SaveAssets();
        }
        return result;
    }

    public static T LoadAsset<T>(string path) where T : ScriptableObject
    {
        path += ".asset";
        T result;
        if (!File.Exists(path)) //obj == null
        {
            result = ScriptableObject.CreateInstance<T>(); //new T();
            //path = AssetDatabase.GenerateUniqueAssetPath(path);
            AssetDatabase.CreateAsset(result, path);
            //UnityEngine.Object.DestroyImmediate(newObj);
            //AssetDatabase.Refresh();
        }
        else
        {
            result = AssetDatabase.LoadAssetAtPath<T>(path);
        }
        //EditorUtility.SetDirty((UnityEngine.Object)result);
        //AssetDatabase.SaveAssets();
        return result;
    }

    /*public static GUIContent[] StringToGUIContent(string[] names)
    {
        int count = names.Length;
        GUIContent[] res = new GUIContent[count];
        for (int i = 0; i < count; i++)
        {
            res[i] = new GUIContent(names[i]);
        }
        return res;
    }*/

    public static string StringFormat(string str) //this string str
    {
        StringBuilder res = new StringBuilder(str);
        res[0] = char.ToUpper(res[0]);//)str.ToUpper()[0];
        return res.ToString();
    }

    public static string GetName(string path)
    {
        path = Path.GetFileName(path);
        return path.Substring(0, path.IndexOf('.'));
    }

    public static bool ColorButton(Rect rect, GUIContent name, bool isSelect, Color selectColor)
    {
        if (isSelect)
        {
            bool result;
            Color colorDefault = GUI.color;
            GUI.color = selectColor;
            result = GUI.Button(rect, name);
            GUI.color = colorDefault;
            return result;
        }
        else
        {
            return GUI.Button(rect, name);
        }
    }

    public static bool ColorButton(Rect rect, String name, bool isSelect, Color selectColor)
    {
        if (isSelect)
        {
            bool result;
            Color colorDefault = GUI.color;
            GUI.color = selectColor;
            result = GUI.Button(rect, name);
            GUI.color = colorDefault;
            return result;
        }
        else
        {
            return GUI.Button(rect, name);
        }
    }

    public static bool ColorButton(Rect rect, String name, bool isSelect, Color selectColor, GUIStyle style)
    {
        if (isSelect)
        {
            bool result;
            Color colorDefault = GUI.color;
            GUI.color = selectColor;
            result = GUI.Button(rect, name, style);
            GUI.color = colorDefault;
            return result;
        }
        else
        {
            return GUI.Button(rect, name, style);
        }
    }

    public static bool ColorButton(Rect rect, Texture texture, bool isSelect, Color color)
    {
        if (isSelect)
        {
            bool result;
            Color colorDefault = GUI.color;
            GUI.color = color;
            result = GUI.Button(rect, texture);
            GUI.color = colorDefault;
            return result;
        }
        else
        {
            return GUI.Button(rect, texture);
        }
    }

    public static Texture2D LoadTexture(string path) //path = "Assets/" + path + ".PNG";
    {
        return UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        /*Texture2D result = null;
        if (File.Exists(path))
        {
            result = new Texture2D(2, 2);
            result.LoadImage(File.ReadAllBytes(path));
            string name = Path.GetFileName(path);
            result.name = name.Substring(0, name.IndexOf('.'));
        }
        return result;*/
    }

    public static void SetTexture(string path, Texture2D texture2D) //path = "Assets/" + path + ".PNG";
    {
        File.WriteAllBytes(path, texture2D.GetRawTextureData());
    }

    public static bool isOpenWindow(Type type)
    {
        UnityEngine.Object[] windows = Resources.FindObjectsOfTypeAll(type);
        if (windows != null && windows.Length > 0) return true;
        return false;
    }

    public static bool isOpenWindow<T>() where T : EditorWindow
    {
        T[] windows = Resources.FindObjectsOfTypeAll<T>();
        if (windows != null && windows.Length > 0) return true;
        return false;
    }

    public static T GetWindow<T>() where T : EditorWindow
    {
        T[] windows = Resources.FindObjectsOfTypeAll<T>();
        if (windows != null && windows.Length > 0) return windows[0];
        return null;
    }

    /// <summary>
    /// Get Roots GameObject in scene
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<GameObject> SceneRoots()
    {
        return EditorSceneManager.GetActiveScene().GetRootGameObjects(); //fix for unity 4.4.1f1
        /*HierarchyProperty prop = new HierarchyProperty(HierarchyType.GameObjects);
        int[] expanded = new int[0];
        while (prop.Next(expanded))
        {
            yield return prop.pptrValue as GameObject;
        }*/
    }

    /// <summary>
    /// Find objects of type and IncludeInactive object
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static List<T> FindObjectsOfType<T>()
    {
        List<T> result = new List<T>();
        foreach (GameObject go in SceneRoots())
        {
            foreach (T obj in go.GetComponentsInChildren<T>(true))
            {
                result.Add(obj);
            }
        }
        return result;
    }

    /// <summary>
    /// Find object of type and IncludeInactive object
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T FindObjectOfType<T>()
    {
        foreach (GameObject go in SceneRoots())
        {
            foreach (T obj in go.GetComponentsInChildren<T>(true))
            {
                return obj;
            }
        }
        return default(T);
    }

    /// <summary>
    /// Find object of name and IncludeInactive object
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static Transform Find(string name)
    {
        Transform result;
        foreach (GameObject go in SceneRoots())
        {
            if ((result = go.GetObjectByNameInChildren(name)) != null) return result;
        }
        return null;
    }

    /*public static List<T> FindObjectsOfType<T>(this UnityEngine.SceneManagement.Scene scene) // not work, on not load scene
    {
        CL.Log(DebugSource.Editor, " " + scene != null); 
        List<T> result = new List<T>();
        foreach (GameObject go in scene.GetRootGameObjects())
        {
            foreach (T obj in go.GetComponentsInChildren<T>(true))
            {
                result.Add(obj);
            }
        }
        return result;
    }*/

    private static Dictionary<object, object> resourceTemp;
    public static T GetObjectTemp<T>(object key)
    {
        if (resourceTemp == null) ReadResourceTemp();
        if (!resourceTemp.ContainsKey(key)) return default(T);
        //CL.Log(DebugSource.Editor, resourceTemp[key].GetType());
        return (T)resourceTemp[key];
    }

    public static int GetObjectIntTemp(object key)
    {
        return Convert.ToInt32(GetObjectTemp<Int64>(key));
    }

    public static string GetObjectStringTemp(object key)
    {
        return GetObjectTemp<string>(key);
    }

    private static string pathTempJson = "EditorSetting" + Path.DirectorySeparatorChar + "EditorTemp.json";// @"EditorSetting\EditorTemp.json";
    private static void ReadResourceTemp()
    {
        resourceTemp = LoadObjectJson<Dictionary<object, object>>(pathTempJson);
        if (resourceTemp == null) resourceTemp = new Dictionary<object, object>();
    }

    public static void SetObjectTemp(object key, object value)
    {
        if (resourceTemp == null) ReadResourceTemp();
        if (resourceTemp.ContainsKey(key))
        {
            if (resourceTemp[key] == value) return;
            resourceTemp[key] = value;
        }
        else
        {
            resourceTemp.Add(key, value);
        }
        SetJson(pathTempJson, resourceTemp);
    }

    public static string GetPathToCurretScript(int frame = 0)
    {
        return new System.Diagnostics.StackTrace(1, true).GetFrame(frame).GetFileName().Replace("\\", "/");
    }

    public static string GetDirectoryCurretScript(int frame = 1)
    {
        return Path.GetDirectoryName(GetPathToCurretScript(frame));
    }

    public static string GetDirectoryCurretScriptShort()
    {
        string resuslt = GetDirectoryCurretScript(2);
        int index = resuslt.IndexOf("Assets");
        return resuslt.Substring(index, resuslt.Length - index);
    }

    public static bool IsDefined<T>(this Enum value)
    {
        return Enum.IsDefined(typeof(T), value.ToString());
    }

    public static bool TryToEnum<T>(this Enum value, out T result)
    {
        Type type = typeof(T);
        if (Enum.IsDefined(type, value.ToString()))
        {
            result = (T)Enum.Parse(type, value.ToString());
            return true;
        }
        else
        {
            result = default(T);
            return false;
        }
    }
    
    /// <summary>
    /// Clone object and copy link UnityEngine.object
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <returns></returns>
    public static T CloneObj<T>(this object source)
    {
        return (T)source.CloneObj();
    }

    /// <summary>
    /// Clone object and copy link UnityEngine.object
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static object CloneObj(this object source)
    {
        Type type = source.GetType();

        if (type.IsClass)
        {
            if (source == null) return null;
            if (typeof(UnityEngine.Object).IsAssignableFrom(type)) return source;
            if (type.Equals(typeof(string))) return source;

            if (type.IsArray)
            {
                Array sourceI = source as Array;
                int sourceLength = sourceI.Length;
                Array resultI = Array.CreateInstance(type.GetElementType(), sourceLength); //result as Array;

                for (int i = 0; i < sourceLength; i++)
                {
                    resultI.SetValue(sourceI.GetValue(i), i);
                }
                return resultI;
            }

            object result = Activator.CreateInstance(type);

            if (typeof(IList).IsAssignableFrom(type))
            {
                IList resultI = result as IList;
                foreach (object item in source as IList)
                {
                    resultI.Add(CloneObj(item));
                }
                return result;
            }

            if (typeof(IDictionary).IsAssignableFrom(type))
            {
                IDictionary resultI = result as IDictionary;
                foreach (KeyValuePair<object, object> item in source as IDictionary)
                {
                    resultI.Add(CloneObj(item.Key), CloneObj(item.Value));
                }
                return result;
            }

            foreach (FieldInfo fieldInfo in type.GetFields())
            {
                fieldInfo.SetValue(result, CloneObj(fieldInfo.GetValue(source)));
            }
        }

        return source;
    }

    public static T LoadObjectJson<T>(string path)
    {
        if (!File.Exists(path)) return default(T);
        return JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
    }

    public static void SetJson(string path, object value)
    {
        if (!Directory.Exists(Path.GetDirectoryName(path))) Directory.CreateDirectory(Path.GetDirectoryName(path)); //Directory.GetParent(path).FullName
        File.WriteAllText(path, JsonConvert.SerializeObject(value));
    }
}
