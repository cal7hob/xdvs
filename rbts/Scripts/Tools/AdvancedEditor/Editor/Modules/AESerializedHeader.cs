using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;

public class AESerializedHeader : AESerializedProperty
{
    public AESerializedHeader() { }

    public AESerializedHeader(AESerializedProperty parent, FieldInfo fieldInfo)
    {
        this.fieldInfo = fieldInfo;
        this.serializedObject = parent.serializedObject;
        this.parent = parent;
        name_ = fieldInfo.Name + "Header";
        header = ((HeaderAttribute)GetCustomAttribute(headerType)).header;
        Init();
    }

    static Type headerType = typeof(HeaderAttribute);
    private string header;

//=============================================================================================================================
//Parser
//=============================================================================================================================
    public override void Init()
    {
        properties = new AEPropertyValueDictionary();
    }

    public override void ReInit() { }

    public override void ReInit(AERectPosition rect) { }

    public override void Parse() { }

    public override void Update() { }

    public override bool Synchronize() { return false; }

//=============================================================================================================================
//clone object
//=============================================================================================================================
    public override void CloneProperty(AESerializedProperty fieldObject) { }

//=============================================================================================================================
//properties object
//=============================================================================================================================
    public override string name
    {
        get
        {
            return name_;
        }
    }

    public override object valueGUIChanged
    {
        get
        {
            return null;
        }

        set { }
    }

    public override object value
    {
        get
        {
            return header;
        }

        set { }
    }

    public override object valueNotChangeControl
    {
        get
        {
            return null;
        }

        set { }
    }

    public override bool SetValue(object value)
    {
        return false;
    }

    public override Tvalue GetValue<Tvalue>()
    {
        return default(Tvalue);
    }

    public override Enum enumValue
    {
        get
        {
            return null;
        }

        set { }
    }

    public override int intValue
    {
        get
        {
            return 0;
        }

        set { }
    }

    public override long longValue
    {
        get
        {
            return 0;
        }

        set { }
    }

    public override uint uintValue
    {
        get
        {
            return 0;
        }

        set { }
    }

    public override float floatValue
    {
        get
        {
            return 0;
        }

        set { }
    }

    public override string stringValue
    {
        get
        {
            return header;
        }

        set { }
    }

    public override bool boolValue
    {
        get
        {
            return false;
        }

        set { }
    }

    public override Vector2 vector2Value
    {
        get
        {
            return new Vector2();
        }

        set { }
    }

    public override Vector3 vector3Value
    {
        get
        {
            return new Vector3();
        }

        set { }
    }

    public override Color colorValue
    {
        get
        {
            return Color.red;
        }

        set { }
    }

    public override UnityEngine.Object unityObjectValue
    {
        get
        {
            return null;
        }

        set { }
    }

    public override Type type
    {
        get
        {
            return headerType;
        }
    }

    public override bool isClass
    {
        get
        {
            return false;
        }
    }

    public override bool isEnum
    {
        get
        {
            return false;
        }
    }

    public override bool isList
    {
        get
        {
            return false;
        }
    }

    public override bool isArray
    {
        get
        {
            return false;
        }
    }

    public override bool isDictionary
    {
        get
        {
            return false;
        }
    }

    public override bool isVirtual
    {
        get
        {
            return false;
        }
    }

//=============================================================================================================================
//Custom Attributes
//=============================================================================================================================
    public override object[] GetCustomAttributes()
    {
        return null;
    }

    public override object[] GetCustomAttributeParams(AECustomAttributes.eType typeAttribute)
    {
        return null;
    }

    public override bool CustomAttributeIsDefined(Type type)
    {
        return false;
    }


    public override object[] GetCustomAttributesType()
    {
        return null;
    }

    public override object GetCustomAttributeType(Type type)
    {
        return null;
    }

    public override bool CustomAttributeTypeIsDefined(Type type)
    {
        return false;
    }

//=============================================================================================================================
//Histori set
//=============================================================================================================================
    public override void HistoriSet() { }

    public override void HistoriOff() { }

//=============================================================================================================================
//property field
//=============================================================================================================================
    public override void PropertyFieldChild(object name, bool printName = true, bool printChild = false, bool manualRect = true) { }

    public override void PropertyFieldChilds(bool printName = true, bool printChild = false, bool manualRect = true) { }

//=============================================================================================================================
//get property
//=============================================================================================================================
    
    /// <summary>
    /// Get Uninty Serialized Property
    /// </summary>
    /// <returns></returns>
    public override SerializedProperty GetUnintySP()
    {
        return null;
    }

    public override string[] GetNamesChildren()
    {
        return new string[0];
    }
}
