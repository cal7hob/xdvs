//*****************************************************************************************************************************
//Advanced editor
//*****************************************************************************************************************************

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class AESerializedObject : AESerializedItem
{
    public AESerializedObject(object object_, bool parse = true)
    {
        this.serializedObject = this;
        this.parent = this;
        this.objectParse = object_;
        rectPosition = new AERectPosition();
        Init();
        if (isClass && parse) Parse();
    }

    private class HistoryItem
    {
        public HistoryItem(AESerializedProperty property)
        {
            this.property = property;
            value = property.oldValue;
        }

        public AESerializedProperty property;
        public object value;

        public void Set()
        {
            value = property.oldValue;
        }

        public void SetChange()
        {
            property.value = value;
        }
    }

    public AESerializedProperty buffer;
    public List<AESerializedProperty> bufferList;
    public AERectPosition rectPosition;
    private bool onUpdateUnitySO = false;
    private bool isModified_ = false;
    public bool isModified
    {
        get
        {
            return isModified_;
        }
    }

    public AEPropertyField propertyField = new AEPropertyField();

    public Dictionary<Type, PaintHandler> paintHandlers = new Dictionary<Type, PaintHandler>();
    public delegate void PaintHandler(AESerializedProperty property, AEPropertyFieldSetting setting);

    public Dictionary<Type, AEList<ChangeHandler>> changeHandlers = new Dictionary<Type, AEList<ChangeHandler>>();
    public Dictionary<AESerializedProperty, AEList<ChangeHandler>> changePropertyHandlers = new Dictionary<AESerializedProperty, AEList<ChangeHandler>>();
    public delegate void ChangeHandler(AESerializedProperty property);
    public event ChangeHandler changeEvent;

    public Dictionary<Type, Type> parseHandlers = new Dictionary<Type, Type>();
    private List<HistoryItem> stackChange = new List<HistoryItem>();
    public List<AESerializedProperty> historiControll = new List<AESerializedProperty>();

    public void SubscribePaintHandler(PaintHandler handler, Type type)
    {
        paintHandlers.Add(type, handler);
    }

    public bool SubscribePaintContains(Type type)
    {
        return paintHandlers.ContainsKey(type);
    }

    public void SubscribeChange(ChangeHandler handler, Type type)
    {
        if (changeHandlers.ContainsKey(type))
        {
            changeHandlers[type].Add(handler);
        }
        else
        {
            changeHandlers.Add(type, new AEList<ChangeHandler> { handler });
        }
    }

    public bool UnsubscribeChange(ChangeHandler handler, Type type)
    {
        if (changeHandlers.ContainsKey(type))
        {
            AEList<ChangeHandler> changeHandler = changeHandlers[type];
            changeHandler.RemoveSafe(handler);
            if (changeHandler.Count == 0) changeHandlers.Remove(type);
            return true;
        }
        else
        {
            Debug.Log(this.GetType() + ": Not UnsubscribeChangePropertyHandler " + type);
            return false;
        }
    }

    public void SubscribeChangeProperty(ChangeHandler handler, AESerializedProperty property)
    {
        if (changePropertyHandlers.ContainsKey(property))
        {
            changePropertyHandlers[property].Add(handler);
        }
        else
        {
            changePropertyHandlers.Add(property, new AEList<ChangeHandler> { handler });
        }
    }

    public bool UnsubscribeChangeProperty(ChangeHandler handler, AESerializedProperty property)
    {
        if (changePropertyHandlers.ContainsKey(property))
        {
            AEList<ChangeHandler> changeHandler = changePropertyHandlers[property];
            if (!changeHandler.RemoveSafe(handler))
            {
                Debug.Log(property.GetType() + ": Not UnsubscribeChangePropertyHandler " + property.name + " " + property.value + " " + property.parent["systemLanguage"].value);
                return false;
            }
            return true;
        }
        else
        {
            Debug.Log(property.GetType() + ": Not UnsubscribeChangePropertyHandler " + property.name + " " + property.value);
            return false;
        }
    }

    public void SubscribeParseHandler(Type customItemType, Type type)
    {
        parseHandlers.Add(type, customItemType);
    }

    public void SubscribeParseHandler(Dictionary<Type, Type> parsers)
    {
        parseHandlers = parsers;
    }

    public void Change(AESerializedProperty property)
    {
        //Debug.Log(property.GetType() + ": Change " + property.name + " " + property.value);

        if (historiControll.Contains(property) && !property.isClass)// && !property.type.IsEnum
        {
            if (stackChange.Count > 0)
            {
                if (stackChange[stackChange.Count - 1].property != property)
                {
                    stackChange.Add(new HistoryItem(property));
                    //Debug.Log(property.GetType() + ": Change " + stackChange.Count + " " + property.name + " " + property.value);
                }
            }
            else
            {
                stackChange.Add(new HistoryItem(property));
            }
        }

        if (changeHandlers.ContainsKey(property.type))
        {
            //Debug.Log(property.GetType() + ": Change " + property.name + " " + property.value + " " + changeHandlers[property.type].Count);
            foreach (ChangeHandler handler in changeHandlers[property.type])
            {
                handler(property);
            }
        }

        if (changePropertyHandlers.ContainsKey(property))
        {
            AEList<ChangeHandler> handlers = changePropertyHandlers[property];
            foreach (ChangeHandler handler in handlers)
            {
                handler(property);
            }
        }

        if (!property.isVirtual)
        {
            if (!isModified_) isModified_ = true;
            if (onUpdateUnitySO) onUpdateUnitySO = true;
        }
        if (changeEvent != null) changeEvent(property);
    }

    public void ParseCustom(params string[] parseNames)
    {
        if (isClass)
        {
            List<string> listParseNames = parseNames.ToList();
            foreach (FieldInfo fieldInfo in type.GetFields()) if(listParseNames.Contains(fieldInfo.Name)) ParseItem(fieldInfo);
        }
    }

    public void CancelChange()
    {
        int index = stackChange.Count - 1;
        if (index == -1) return;
        //Debug.Log(GetType() + ": CancelChange " + stackChange.Count + " " + stackChange[index].property.name + " " + stackChange[index].value);
        stackChange[index].SetChange();
        stackChange.RemoveAt(index);
    }

    public override void Parse()
    {
        stackChange.Clear();
        base.Parse();
    }

    public override void Init()
    {
        properties = new AEPropertyClassDictionary();
        typeCache = objectParse.GetType();
        isClassCache = isClass_;
    }

    public override void ReInit()
    {
        serializedObject.rectPosition = new AERectPosition();
        base.ReInit();
    }

    public override void ReInit(AERectPosition rect)
    {
        serializedObject.rectPosition = rect;
        base.ReInit();
    }

    public override void Update()
    {
        base.Update();
        isModified_ = false;
    }

    public override bool isMonoBehaviour
    {
        get
        {
            return false;
        }
    }

    protected override bool isMonoBehaviour_
    {
        get
        {
            return false;
        }
    }

    public void ChangeParseObject(object objectParse)
    {
        this.objectParse = objectParse;
        properties.Clear();
        if (isClass_) Parse();
    }

    private bool isManualRectOne = false;
    private bool isOffPrintNameOne_ = false;

    public bool isOffPrintNameOne
    {
        get
        {
            if (isOffPrintNameOne_)
            {
                return true;
            }
            else
            {
                isOffPrintNameOne_ = true;
                return false;
            }
        }
    }

    public void ChangeOffPrintNameOne()
    {
        isOffPrintNameOne_ = false;
    }

    public void ChangeManualRectOne()
    {
        isManualRectOne = true;
    }

    public Rect GetRectNext(bool manualRect)
    {
        if (isManualRectOne)
        {
            isManualRectOne = false;
            return rectPosition.GetRect();
        }
        else
        {
            return manualRect ? rectPosition.GetRect() : rectPosition.Next();
        }
    }

    public Rect GetRectNext(bool manualRect, int width)
    {
        if (isManualRectOne)
        {
            isManualRectOne = false;
            return rectPosition.GetRect();
        }
        else
        {
            return manualRect ? rectPosition.GetRect() : rectPosition.Next(width);
        }
    }

    public Rect GetRectNext(bool manualRect, int width, int height)
    {
        if (isManualRectOne)
        {
            isManualRectOne = false;
            return rectPosition.GetRect(height);
        }
        else
        {
            return manualRect ? rectPosition.GetRect() : rectPosition.Next(width, height);
        }
    }

    public bool PrefabApply()
    {
        if (isModified_)
        {
            EditorUtility.SetDirty((UnityEngine.Object)value);
            AssetDatabase.SaveAssets();
            isModified_ = false;
            return true;
        }
        return false;
    }

    public bool SceneApply()
    {
        if (isModified_)
        {
            EditorUtility.SetDirty((UnityEngine.Object)value);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());//EditorApplication.SaveScene();
            isModified_ = false;
            return true;
        }
        return false;
    }

    public bool SceneSetDirty()
    {
        if (isModified_)
        {
            EditorUtility.SetDirty((UnityEngine.Object)value);
            isModified_ = false;
            return true;
        }
        return false;
    }

    public bool GetIsModified()
    {
        if (isModified_)
        {
            isModified_ = false;
            return true;
        }
        return false;
    }

    public override bool Synchronize()
    {
        foreach (KeyValuePair<Type, AEList<ChangeHandler>> handler in changeHandlers) handler.Value.Synchronize();
        foreach (KeyValuePair<AESerializedProperty, AEList<ChangeHandler>> handler in changePropertyHandlers) handler.Value.Synchronize();
        return true;
    }

    private SerializedObject so;
    /// <summary>
    /// Get Unity Serialized Object
    /// </summary>
    /// <returns></returns>
    public SerializedObject GetUnitySO()
    {
        if (so != null)
        {
            so.Update();
            return so;
        }
        return so = new SerializedObject((UnityEngine.Object)value);
    }

    public override SerializedProperty GetUnintySPChild(string name)
    {
        return GetUnitySO().FindProperty(name);
    }

    public override string name
    {
        get
        {
            return name_ ?? (name_ = GetProperty<String>("name") ?? base.name);
        }
    }
}
