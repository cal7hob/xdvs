#define GoogleAnalyticsFix
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;



public class AEPropertyFieldSetting
{
    public AEPropertyFieldSetting(bool printName, bool printChild, bool manualRect)
    {
        this.printName = printName;
        this.printChild = printChild;
        this.manualRect = printChild? false: manualRect;
    }

    public AEPropertyFieldSetting(AEPropertyFieldSetting setting)
    {
        this.printName = setting.printName;
        this.printChild = setting.printChild;
        this.manualRect = setting.manualRect;
    }

    public bool printName;// = true;
    public bool printChild;// = false;
    public bool manualRect;// = true;
}

public class AESerializedProperty : IEnumerable//, IEnumerator
{
    public AESerializedProperty() { }

    public AESerializedProperty(AESerializedProperty parent, FieldInfo fieldInfo)
    {
        this.fieldInfo = fieldInfo;
        serializedObject = parent.serializedObject;
        this.parent = parent;

        objectParse = fieldInfo.GetValue(this.parent.objectParse);
        isHide = CustomAttributeIsDefined(typeof(HideInInspector)); //visible

        
        if (CustomAttributeIsDefined(rangeType)) rangeAttribute_ = (RangeAttribute)GetCustomAttribute(rangeType);

        name_ = CustomAttributeIsDefined(tooltipType) ? ((TooltipAttribute)GetCustomAttribute(tooltipType))
#if GoogleAnalyticsFix
            .text
#else
        .tooltip
#endif
            : fieldInfo.Name;

        Init();
    }

    protected static Type rangeType = typeof(RangeAttribute);
    protected static Type tooltipType = typeof(TooltipAttribute);

    protected string name_;
    protected RangeAttribute rangeAttribute_ = null;
    public RangeAttribute rangeAttribute
    {
        get
        {
            return rangeAttribute_;
        }
    }

    public AEPropertyDictionary properties;
    public AESerializedObject serializedObject;
    private object thisValue;
    private object oldValue_;
    public object oldValue
    {
        get
        {
            return oldValue_;
        }
    }

    public object objectParse
    {
        get
        {
            return thisValue;
        }

        set
        {
            oldValue_ = thisValue;
            thisValue = value;
        }
    }

    public FieldInfo fieldInfo;
    public AESerializedProperty parent;
    
    //[XmlType("FoldoutItem")] //http://habrahabr.ru/post/163071/
    public bool foldout = false;
    public bool isHide = false;
    protected SerializedProperty unintySPCache;
    

//=============================================================================================================================
//Parser
//=============================================================================================================================
    public virtual void Init()
    {
        properties = new AEPropertyValueDictionary();
    }

    public virtual void ReInit() { }

    public virtual void ReInit(AERectPosition rect) { }

    public virtual void Parse() { }

    public virtual void Update()
    {
        object newValue = fieldInfo.GetValue(parent.value);
        if (newValue == value) return;
        if (value == null || !value.Equals(newValue))
        {
            if (isEnum)
            {
                if (newValue.Equals((int)objectParse)) return;
                objectParse = Enum.ToObject(type, newValue);
            }
            else
            {
                objectParse = newValue;
            }
            fieldInfo.SetValue(parent.value, objectParse);
            serializedObject.Change(this);
        }
    }

    public virtual bool Synchronize() { return false; }

//=============================================================================================================================
//clone object
//=============================================================================================================================
    public virtual void CloneProperty(AESerializedProperty fieldObject)
    {
        if (value == null) return;
        fieldObject.value = value;
    }

//=============================================================================================================================
//properties object
//=============================================================================================================================
    public virtual string name
    {
        get
        {
            return name_;
        }
    }

    public string Name
    {
        get
        {
            return AEEditorTools.StringFormat(name);
        }
    }

    public override string ToString()
    {
        return name;
    }

    public virtual GUIContent GetNameGUIContent(bool printName)
    {
        return printName && serializedObject.isOffPrintNameOne ? new GUIContent(Name) : GUIContent.none;
    }

    public virtual object valueGUIChanged
    {
        get
        {
            return objectParse;
        }

        set
        {
            if (GUI.changed) this.value = value;
        }
    }

    public virtual object value
    {
        get
        {
            return objectParse;
        }

        set
        {
            if (SetValue(value)) serializedObject.Change(this);
        }
    }

    public virtual object valueNotChangeControl
    {
        get
        {
            return objectParse;
        }

        set
        {
            SetValue(value);
        }
    }

    public virtual bool SetValue(object value)
    {
        if (objectParse == null)
        {
            if (objectParse == value) return false;
        }
        else
        {
            if (objectParse.Equals(value)) return false;
        }

        if (isEnum)
        {
            if (value.Equals((int)objectParse)) return false;
            objectParse = Enum.ToObject(type, value);
        }
        else
        {
            objectParse = value;
        }
        fieldInfo.SetValue(parent.value, value);
        return true;
    }

    public virtual Tvalue GetValue <Tvalue>()
    {
        return (Tvalue)value;
    }

    public virtual Enum enumValue
    {
        get
        {
            return (Enum)objectParse;
        }

        set
        {
            this.value = value;
        }
    }

    public virtual byte byteValue
    {
        get
        {
            return (byte)objectParse;
        }

        set
        {
            this.value = value;
        }
    }

    public virtual int intValue
    {
        get
        {
            return (int)objectParse;
        }

        set
        {
            this.value = value;
        }
    }

    public virtual long longValue
    {
        get
        {
            return (long)objectParse;
        }

        set
        {
            this.value = value;
        }
    }

    public virtual uint uintValue
    {
        get
        {
            return (uint)objectParse;
        }

        set
        {
            this.value = value;
        }
    }

    public virtual float floatValue
    {
        get
        {
            return (float)objectParse;
        }

        set
        {
            this.value = value;
        }
    }

    public virtual string stringValue
    {
        get
        {
            return (string)objectParse;
        }

        set
        {
            this.value = value;
        }
    }

    public virtual bool boolValue
    {
        get
        {
            return (bool)objectParse;
        }

        set
        {
            this.value = value;
        }
    }

    public virtual Vector2 vector2Value
    {
        get
        {
            return (Vector2)objectParse;
        }

        set
        {
            this.value = value;
        }
    }

    public virtual Vector3 vector3Value
    {
        get
        {
            return (Vector3)objectParse;
        }

        set
        {
            this.value = value;
        }
    }

    public virtual Color colorValue
    {
        get
        {
            return (Color)objectParse;
        }

        set
        {
            this.value = value;
        }
    }

    public virtual UnityEngine.Object unityObjectValue
    {
        get
        {
            return (UnityEngine.Object)objectParse;
        }

        set
        {
            this.value = value;
        }
    }

    public virtual UnityEngine.GameObject gameObjectValue
    {
        get
        {
            return (UnityEngine.GameObject)objectParse;
        }

        set
        {
            this.value = value;
        }
    }

    public virtual Type type
    {
        get
        {
            return fieldInfo.FieldType;
        }
    }

    public virtual bool isClass
    {
        get
        {
            return false;
        }
    }

    public virtual bool isEnum
    {
        get
        {
            return type.IsEnum;
        }
    }

    public virtual bool isList
    {
        get
        {
            return false;
        }
    }

    public virtual bool isArray
    {
        get
        {
            return false;
        }
    }

    public virtual bool isDictionary
    {
        get
        {
            return false;
        }
    }

    public virtual bool isVirtual
    {
        get
        {
            return false;
        }
    }

    public virtual bool isMonoBehaviour
    {
        get
        {
            return false;
        }
    }

//=============================================================================================================================
//Custom Attributes
//=============================================================================================================================
    public virtual object[] GetCustomAttributes()
    {
        return fieldInfo.GetCustomAttributes(false);
    }

    public virtual object[] GetCustomAttributeParams(AECustomAttributes.eType typeAttribute)
    {
        Type type = typeof(AECustomAttributes);
        AECustomAttributes customAttribute;
        foreach (object attribute in fieldInfo.GetCustomAttributes(false)) //fieldInfo.GetCustomAttributes(type, false)
        {
            if (attribute.GetType() == type)
            {
                customAttribute = attribute as AECustomAttributes;
                if (customAttribute.type == typeAttribute) return customAttribute.params_;
            }
        }
        return null;
    }

    public virtual object GetCustomAttribute(Type type)
    {
        foreach (object attribute in fieldInfo.GetCustomAttributes(false))
        {
            if (attribute.GetType() == type) return attribute;
        }
        return null;
    }
    
    public virtual bool CustomAttributeIsDefined(Type type)
    {
        return fieldInfo.IsDefined(type, false);
    }


    public virtual object[] GetCustomAttributesType()
    {
        return type.GetCustomAttributes(false);
    }

    public virtual object GetCustomAttributeType(Type type)
    {
        foreach (object attribute in this.type.GetCustomAttributes(false))
        {
            if (attribute.GetType() == type) return attribute;
        }
        return null;
    }

    public virtual bool CustomAttributeTypeIsDefined(Type type)
    {
        return this.type.IsDefined(type, false);
    }

//=============================================================================================================================
//Method call
//=============================================================================================================================
    public virtual object MethodCall(string name, object[] parameters)
    {
        Debug.Log(this.GetType() + ": MethodCall(): " + name + " object not class: " + type);
        return null;
    }

//=============================================================================================================================
//Histori set
//=============================================================================================================================
    public virtual void HistoriSet()
    {
        if (serializedObject.historiControll.Contains(this))
        {
            Debug.Log(this.GetType() + ": duplicate" + name + " " + value);
            return;
        }
        serializedObject.historiControll.Add(this);
    }

    public virtual void HistoriOff()
    {
        serializedObject.historiControll.Remove(this);
    }

//=============================================================================================================================
//List
//=============================================================================================================================
    public virtual AESerializedItem AddTop()
    {
        Debug.Log(this.GetType() + ": ListAdd(): object not list: " + type);
        return null;
    }

    public virtual AESerializedItem AddTop(object value)
    {
        Debug.Log(this.GetType() + ": ListAdd(): object not list: " + type);
        return null;
    }
    
    public virtual AESerializedItem Add()
    {
        Debug.Log(this.GetType() + ": ListAdd(): object not list: " + type);
        return null;
    }

    public virtual AESerializedItem Add(object[] params_)
    {
        Debug.Log(this.GetType() + ": ListAdd(): object not list: " + type);
        return null;
    }

    public virtual AESerializedItem Add(object value)
    {
        Debug.Log(this.GetType() + ": ListAdd(): object not list: " + type);
        return null;
    }

    public virtual void Move(int index) { }

    public virtual Type itemType
    {
        get
        {
            Debug.Log(this.GetType() + ": itemType: object not list: " + type);
            return type;
        }
    }

    public virtual bool itemIsClass// not working
    {
        get
        {
            Debug.Log(this.GetType() + ": itemIsClass: object not list: " + type);
            return false;
        }
    }

    public virtual bool itemIsMonoBehaviour
    {
        get
        {
            Debug.Log(this.GetType() + ": itemIsMonoBehaviour: object not list: " + type);
            return false;
        }
    }

    public virtual void ListRemoveThisItem()
    {
        Debug.Log(this.GetType() + ": ListRemoveThisItem(): this object not list: " + type);
    }

    public virtual void ListRemove(AESerializedProperty property)
    {
        Debug.Log(this.GetType() + ": ListRemove(_" + property.name + "): this object not list: " + type);
    }

    public virtual void ListRemove(object value)
    {
        Debug.Log(this.GetType() + ": ListRemove(_object_" + value + "): this object not list: " + type);
    }

    public virtual int count
    {
        get
        {
            return 0;
        }

        set
        {
            Debug.Log(this.GetType() + ": count set: this object not list: " + type);
        }
    }

    public virtual void Clear() { }

    public virtual int countVisible
    {
        get
        {
            return 1;
        }
    }

    public virtual bool ContainsKey(object key)
    {
        return false;
    }

    public virtual int IndexOf(object key)
    {
        return -1;
    }

    public virtual void Sort<T>(Comparison<T> comparison) { }

//=============================================================================================================================
//Dictionary
//=============================================================================================================================

    public virtual AESerializedItem itemNewKey
    {
        get
        {
            Debug.Log(this.GetType() + ": itemNewKey: not support! " + name);
            return null;
        }
    }

    public virtual AESerializedItemIList dictKeyItem
    {
        get
        {
            Debug.Log(this.GetType() + ": dictKeyItem: not support! " + name);
            return null;
        }

        set
        {
            Debug.Log(this.GetType() + ": dictKeyItem: not support! " + name);
        }
    }
    
    public virtual object dictKey
    {
        get
        {
            Debug.Log(this.GetType() + ": dictKey: get: not dictionary " + name);
            return null;
        }

        set
        {
            Debug.Log(this.GetType() + ": dictKey: set: not dictionary " + name);
        }
    }
    
    public virtual Type itemTypeKey
    {
        get
        {
            Debug.Log(this.GetType() + ": itemTypeKey: object not dictionary: " + type);
            return type;
        }
    }

    public virtual bool itemKeyIsClass
    {
        get
        {
            Debug.Log(this.GetType() + ": itemKeyIsClass: object not dictionary: " + type);
            return false;
        }
    }

    public virtual AESerializedItem DictAdd(object key)
    {
        Debug.Log(this.GetType() + ": DictAdd(object key): not suport add: " + type);
        return null;
    }

    public virtual AESerializedItem DictAdd(object key, object value)
    {
        Debug.Log(this.GetType() + ": DictAdd(object key, object value): not suport add: " + type);
        return null;
    }
//=============================================================================================================================
//property field
//=============================================================================================================================
    public virtual void PropertyFieldChild(object name, bool printName = true, bool printChild = false, bool manualRect = true) //child
    {
        if (properties.ContainsKey(name))
        {
            serializedObject.propertyField.PropertyField(properties[name], new AEPropertyFieldSetting(printName, printChild, manualRect));
        }
        else
        {
            Debug.Log(this.GetType() + ": PropertyField: not object " + PathDebug() + "(." + name + ")");
            //PrintDebug();
        }
    }

    public virtual void PropertyFieldChilds(bool printName = true, bool printChild = false, bool manualRect = true)
    {
        AEPropertyFieldSetting setting = new AEPropertyFieldSetting(printName, printChild, manualRect);
        foreach (AESerializedProperty property in this) serializedObject.propertyField.PropertyField(property, setting);
    }

    public virtual void PropertyField(bool printName = true, bool printChild = false, bool manualRect = true)
    {
        serializedObject.propertyField.PropertyField(this, new AEPropertyFieldSetting(printName, printChild, manualRect));
    }

    public virtual void PropertyField(AEPropertyFieldSetting setting)
    {
        serializedObject.propertyField.PrintClass(this, setting); //it works only for classes
    }

    public virtual void PropertyField(AEPropertyField propertyField, bool printName = true, bool printChild = false, bool manualRect = true)
    {
        propertyField.PropertyField(this, new AEPropertyFieldSetting(printName, printChild, manualRect));
    }

//=============================================================================================================================
//get property
//=============================================================================================================================
    public virtual AESerializedProperty GetPropertyPath(string name)
    {
        Debug.Log(this.GetType() + ": GetPropertyPath(_" + name + " ) Warning, this object is values type");
        return null;
    }

    public virtual AESerializedProperty GetProperty(object name, bool silent = false)
    {
        Debug.Log(this.GetType() + ": GetProperty(_" + name + " ) Warning, this object is values type");
        return null;
    }

    public virtual AESerializedProperty GetPropertyAt(int index)
    {
        Debug.Log(this.GetType() + ": GetPropertyAt(_" + name + " ) Warning, this object is values type");
        return null;
    }


    public virtual AESerializedProperty GetPropertyFirst()
    {
        Debug.Log(this.GetType() + ": GetPropertyFirst(_" + name + " ) Warning, this object is values type");
        return null;
    }

    public virtual T GetProperty<T>(string name)
    {
        Debug.Log(this.GetType() + ": GetProperty(_" + name + " ) Warning, this object is values type");
        return default(T);
    }

    /// <summary>
    /// Get Uninty Serialized Property Child
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public virtual SerializedProperty GetUnintySPChild(string name)
    {
        Debug.Log(this.GetType() + ": " + name);
        return null;
    }

    /// <summary>
    /// Get Uninty Serialized Property
    /// </summary>
    /// <returns></returns>
    public virtual SerializedProperty GetUnintySP()
    {
        //if (unintySPCache != null) return unintySPCache;
        return /*unintySPCache =*/ parent.GetUnintySPChild(name);
    }

    public virtual void PrintDebug()
    {
        return;
    }

    public virtual List<string> GetPath()
    {
        AESerializedProperty select = this;
        List<String> path = new List<string>();
        while (select != serializedObject)
        {
            path.Add(select.name);
            select = select.parent;
        }
        path.Add(select.name);
        path.Reverse(0, path.Count);
        return path;
    }

    public virtual string PathDebug()
    {
        List<String> path = GetPath();
        string pathString = path[0];
        path.RemoveAt(0);
        foreach (string name_ in path) pathString += "." + name_;
        return pathString;
    }

    public virtual string[] GetNamesChildren()
    {
        return new string[0];
    }
    
    public virtual bool PropertyContainsKey(object key)
    {
        return false;
    }

    public virtual AESerializedProperty this[object key]
    {
        get
        {
            Debug.Log(this.GetType() + ": this[_" + name + " ] Warning, this object is values type");
            return null;
        }
    }

    public IEnumerator GetEnumerator()//public virtual IEnumerator GetEnumerator() //IEnumerable.GetEnumerator()
    {
        //return new List<AESerializedProperty>().GetEnumerator();
        //return new List<AESerializedProperty>.Enumerator();
        Synchronize();
        return properties.GetEnumerator();
    }

    /*public virtual List<AESerializedProperty>.Enumerator GetEnumerator() //virtual List<AESerializedProperty>.Enumerator 
    {
        return properties.GetEnumerator();
    }*/

    //bool List<AESerializedProperty>.Enumerator.MoveNext()
    /*bool IEnumerator.MoveNext()
    {
        return false;
    }

    //AESerializedProperty IEnumerator<AESerializedProperty>.Current //AESerializedProperty List<AESerializedProperty>.Enumerator.curent //AESerializedProperty
    object IEnumerator.Current
    {
        get { return null; }
    }

    public AESerializedProperty Current
    {
        get { return null; }
    }

    void IEnumerator.Reset() { }*/
}
