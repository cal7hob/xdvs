using System;
using System.Collections.Generic;
using UnityEngine;

public static class AEResourcesDispatcher
{
    private class Subscribes
    {
        public Subscribes(AESerializedObject serializedObject, int handle, bool saveOnScene)
        {
            this.serializedObject = serializedObject;
            this.handle = handle;
            this.saveOnScene = saveOnScene;
        }

        public int handle;
        public AESerializedObject serializedObject;
        public Dictionary<Type, AEList<AESerializedObject.ChangeHandler>> changeHandlers = new Dictionary<Type, AEList<AESerializedObject.ChangeHandler>>();
        public Dictionary<AESerializedProperty, AEList<AESerializedObject.ChangeHandler>> changePropertyHandlers = new Dictionary<AESerializedProperty, AEList<AESerializedObject.ChangeHandler>>();
        public bool saveOnScene;
        private bool subscribe = false;
        public bool onSubscribe
        {
            get
            {
                if (!subscribe)
                {
                    return subscribe = true;
                }
                return false;
            }
        }

        public void Update(AESerializedObject serializedObject)
        {
            this.serializedObject = serializedObject;
            changeHandlers.Clear();
            changePropertyHandlers.Clear();

            foreach (KeyValuePair<Type, AEList<AESerializedObject.ChangeHandler>> item in changeHandlers)
            {
                foreach (AESerializedObject.ChangeHandler handler in item.Value)
                {
                    serializedObject.SubscribeChange(handler, item.Key);
                }
            }

            foreach (KeyValuePair<AESerializedProperty, AEList<AESerializedObject.ChangeHandler>> item in changePropertyHandlers)
            {
                foreach (AESerializedObject.ChangeHandler handler in item.Value)
                {
                    serializedObject.UnsubscribeChangeProperty(handler, item.Key);
                }
            }
        }
    }

    private static Dictionary<Type, Dictionary<Type, Type>> parsersAll = new Dictionary<Type,Dictionary<Type,Type>>();
    private static AEDictionary<object, AESerializedObject> resources = new AEDictionary<object, AESerializedObject>();

    private static Dictionary<int, Subscribes> openResources = new Dictionary<int, Subscribes>();
    private static int handleCurrentId = 0;

    /// <summary>
    /// Сохранение используемого парсера, вызывать из метода с атрибутом [UnityEditor.Callbacks.DidReloadScripts]
    /// </summary>
    /// <param name="usingType">Для какого типа объекта применять парсеры</param>
    /// <param name="parsers">Словарь с парсерами: key-Тип Сериализуемого обекта, value-Тип класса сериaлизатора</param>
    public static void SetParsers(Type usingType, Dictionary<Type, Type> parsers)//Type type
    {
        if (parsersAll.ContainsKey(usingType))
        {
            parsersAll[usingType].Merge(parsers);
        }
        else
        {
            parsersAll.Add(usingType, parsers);
        }
        //CL.Log(DebugSource.Editor, usingType + " " + parsersAll.Count + " " + parsers.Count);
    }

    /*public static AESerializedObject GetResourcesScene<T>(this UnityEngine.SceneManagement.Scene scene, ref int handle) where T : Component
    {
        List<T> objs = scene.FindObjectsOfType<T>();
        if (objs.Count == 0) return null;
        return GetResources<T>(ref handle, objs[0], true);
    }*/

    public static AESerializedObject GetResourcesScene<T>(ref int handle) where T : Component
    {
        T obj = UnityEngine.Object.FindObjectOfType<T>();
        return GetResources<T>(ref handle, obj, true);
    }

    public static AESerializedObject GetResourcesScene<T>(ref int handle, object obj) where T : Component
    {
        return GetResources<T>(ref handle, obj, true);
    }

    public static AESerializedObject GetResources<T>(string path, ref int handle) where T : Component
    {
        return GetResources<T>(ref handle, path, false);
    }

    public static AESerializedObject GetResources(int handle)
    {
        if (openResources.ContainsKey(handle))
        {
            return openResources[handle].serializedObject;
        }
        return null;
    }

    private static AESerializedObject GetResources<T>(ref int handle, object key, bool isObject) where T : Component
    {
        //CL.Log(DebugSource.Editor, "idUsingResource count: " + idUsingResource.Count + " " + key);
        if (key == null)
        {
            if (openResources.ContainsKey(handle)) openResources.Remove(handle);
            return null;
        }

        object obj;
        object keyType;
        if (isObject)
        {
            obj = keyType = key;
        }
        else
        {
            keyType = key + "(" + typeof(T) + ")";
            obj = AEEditorTools.LoadResources<T>((string)key);
        }

        AESerializedObject resource = resources[keyType];
        if (resource == null || resource.value != obj) //not or old resource
        {
            //Debug.Log("resources.value != obj " + key);
            Type type = typeof(T);
            if (parsersAll.ContainsKey(type))
            {
                resource = new AESerializedObject(obj, false);
                resource.SubscribeParseHandler(parsersAll[type]);
                resource.Parse();
                if (openResources.ContainsKey(handle))
                {
                    openResources[handle].Update(resource);
                }
            }
            else
            {
                resource = new AESerializedObject(obj);
            }
            resources[keyType] = resource;
        }
        else
        {
            resource.Update();
        }

        GetHandle(ref handle, resource, isObject);
        return resource;
    }

    private static void GetHandle(ref int handle, AESerializedObject resource, bool isObject)
    {
        if (!openResources.ContainsKey(handle))
        {
            handleCurrentId++;
            handle = handleCurrentId;
            openResources.Add(handle, new Subscribes(resource, handle, isObject));
            //CL.Log(DebugSource.Editor, "idUsingResource end Add count: " + idUsingResource.Count);
            return;
        }
    }

    public static void FreeResource(int handle)
    {
        if (openResources.ContainsKey(handle))
        {
            Subscribes subscribes = openResources[handle];
            foreach (KeyValuePair<Type, AEList<AESerializedObject.ChangeHandler>> item in subscribes.changeHandlers)
            {
                foreach (AESerializedObject.ChangeHandler handler in item.Value)
                {
                    //item.Value.RemoveSafe(handler);
                    //if (item.Value.Count == 0) subscribes.changeHandlers.RemoveSafe(item.Key);
                    subscribes.serializedObject.UnsubscribeChange(handler, item.Key);
                }
                item.Value.Clear();
            }
            subscribes.changeHandlers.Clear();

            foreach (KeyValuePair<AESerializedProperty, AEList<AESerializedObject.ChangeHandler>> item in subscribes.changePropertyHandlers)
            {
                foreach (AESerializedObject.ChangeHandler handler in item.Value)
                {
                    //item.Value.RemoveSafe(handler);
                    //if (item.Value.Count == 0) subscribes.changeHandlers.RemoveSafe(item.Key);
                    subscribes.serializedObject.UnsubscribeChangeProperty(handler, item.Key);
                }
                item.Value.Clear();
            }
            subscribes.changeHandlers.Clear();
            if (subscribes.saveOnScene)
            {
                subscribes.serializedObject.SceneApply();
            }
            else
            {
                subscribes.serializedObject.PrefabApply();
            }
            openResources.Remove(handle);
        }
    }

    public static void ApplyResource(int handle)
    {
        if (openResources.ContainsKey(handle))
        {
            Subscribes subscribes = openResources[handle];
            if (subscribes.saveOnScene)
            {
                subscribes.serializedObject.SceneApply();
            }
            else
            {
                subscribes.serializedObject.PrefabApply();
            }
        }
    }

    public static void SubscribeChange(int handle, AESerializedObject.ChangeHandler handler, Type type)
    {
        if (openResources.ContainsKey(handle))
        {
            Subscribes subscribes = openResources[handle];
            if (subscribes.changeHandlers.ContainsKey(type))
            {
                AEList<AESerializedObject.ChangeHandler> changeHandlersList = subscribes.changeHandlers[type];
                if (changeHandlersList.Contains(handler)) return;
                changeHandlersList.Add(handler);
            }
            else
            {
                subscribes.changeHandlers.Add(type, new AEList<AESerializedObject.ChangeHandler> { handler });
            }
            subscribes.serializedObject.SubscribeChange(handler, type);
        }
    }

    public static void SubscribeChangeProperty(int handle, AESerializedObject.ChangeHandler handler, AESerializedProperty property)
    {
        if (openResources.ContainsKey(handle))
        {
            Subscribes subscribes = openResources[handle];
            if (subscribes.changePropertyHandlers.ContainsKey(property))
            {
                AEList<AESerializedObject.ChangeHandler> changeHandlersList = subscribes.changePropertyHandlers[property];
                if (changeHandlersList.Contains(handler)) return;
                changeHandlersList.Add(handler);
            }
            else
            {
                subscribes.changePropertyHandlers.Add(property, new AEList<AESerializedObject.ChangeHandler> { handler });
            }

            subscribes.serializedObject.SubscribeChangeProperty(handler, property);
        }
    }

    public static bool SubscribeCheck(int handle)
    {
        if (openResources.ContainsKey(handle)) return false;
        return openResources[handle].onSubscribe;
    }
}
