using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class AESerializedClass : AESerializedProperty
{
    public AESerializedClass() { }

    public AESerializedClass(AESerializedProperty parent, FieldInfo fieldInfo): base(parent,fieldInfo)
    {
        Parse();
    }

//=============================================================================================================================
//Parser
//=============================================================================================================================
    public override void Init()
    {
        properties = new AEPropertyClassDictionary();
    }

    public override void Parse()
    {
        if (isMonoBehaviour_) return;
        if (value == null) return;
        
        foreach (FieldInfo fieldInfo in type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public)) ParseItem(fieldInfo);
    }

    private static Type headerType = typeof(HeaderAttribute);
    private static Type aEDictionaryAttributeType = typeof(AEDictionaryAttribute);
    private static Type iDictionaryType = typeof(IDictionary);
    private static Type iListType = typeof(IList);
    private static Type monoBehaviourType = typeof(UnityEngine.Object); //MonoBehaviour UnityEngine.Object
    private static Type serializeField = typeof(SerializeField);
    //private static Type transformType = typeof(Transform);

    protected virtual void ParseItem(FieldInfo fieldInfo)
    {
        //if (!fieldInfo.FieldType.IsSerializable /*|| !fieldInfo.FieldType.IsDefined(typeof(SerializeField), false)*/) continue;
        if (fieldInfo.IsDefined(headerType, false))
        {
            if (!fieldInfo.FieldType.IsSerializable && !fieldInfo.IsDefined(serializeField, false))
            {
                if (fieldInfo.IsPrivate) return;
                if (!AEPropertyField.objectTypePrint.ContainsKey(fieldInfo.FieldType)) return;
            }
            properties.Add(fieldInfo.Name + "Header", new AESerializedHeader(this, fieldInfo));
        }

        /*if (fieldInfo.IsPrivate && !fieldInfo.IsDefined(serializeField, false))
         {
             Debug.Log("_Def_" + fieldInfo.Name);
             return;
         }*/

        //if (fieldInfo.IsStatic) return;

        //if (iDictionaryType.IsAssignableFrom(fieldInfo.FieldType)) return;

        if (serializedObject.parseHandlers.ContainsKey(fieldInfo.FieldType))
        {
            properties.Add(fieldInfo.Name, (AESerializedProperty)Activator.CreateInstance(serializedObject.parseHandlers[fieldInfo.FieldType], this, fieldInfo));
            return;
        }
        
        if (fieldInfo.FieldType.IsArray)
        {
            properties.Add(fieldInfo.Name, new AESerializedArray(this, fieldInfo));
            return;
        }

        if (iListType.IsAssignableFrom(fieldInfo.FieldType)) //is List
        {
            properties.Add(fieldInfo.Name, new AESerializedList(this, fieldInfo));
            return;
        }

        if (iDictionaryType.IsAssignableFrom(fieldInfo.FieldType)) //is Dictionary Native
        {
            properties.Add(fieldInfo.Name, new AESerializedDictionaryNative(this, fieldInfo));
            return;
        }

        if (fieldInfo.FieldType.IsDefined(aEDictionaryAttributeType, false)) //is Dictionary
        {
            properties.Add(fieldInfo.Name, new AESerializedDictionary(this, fieldInfo));
            return;
        }

        if (fieldInfo.FieldType.IsClass && !AEPropertyField.objectTypePrint.ContainsKey(fieldInfo.FieldType)) //isClass
        {
            properties.Add(fieldInfo.Name, new AESerializedClass(this, fieldInfo));
            return;
        }
        
        properties.Add(fieldInfo.Name, new AESerializedProperty(this, fieldInfo));
    }

    public override void Update()
    {
        foreach (AESerializedProperty property in this)
        {
            property.Update();
        }
    }

//=============================================================================================================================
//clone object
//=============================================================================================================================
    public override void CloneProperty(AESerializedProperty fieldObject)
    {
        if (value == null) return;
        if (fieldObject.value == null && isClass)
        {
            fieldObject.value = Activator.CreateInstance(fieldObject.type);
            fieldObject.Parse();
        }
        if (fieldObject.type != type)
        {
            //CL.Log(DebugSource.Editor, "Type: " + fieldObject.type + " != " + type + ", use recursive clone");
            AESerializedProperty fieldProperty;
            foreach (AESerializedProperty property in this)
            {
                if ((fieldProperty = fieldObject[property.name]) != null) property.CloneProperty(fieldProperty);
            }
            return;
        }

        //Dictionary<string, AESerializedProperty>.Enumerator ie = fieldObject.GetEnumerator();
        //System.Collections.IEnumerator ie = fieldObject.GetEnumerator();
        //List<AESerializedProperty>.Enumerator ie = properties.Values.GetEnumerator(); //fieldObject.GetEnumerator();
        //Dictionary<string, AESerializedProperty>.Enumerator ie = fieldObject.properties.GetEnumerator();// properties.Values.GetEnumerator();
        List<AESerializedProperty>.Enumerator ie = fieldObject.properties.GetEnumerator();
        foreach (AESerializedProperty property in this)
        {
            ie.MoveNext();
            property.CloneProperty(ie.Current);
        }
    }

//=============================================================================================================================
//properties object
//=============================================================================================================================
    public override string name
    {
        get
        {
            return fieldInfo.Name;
        }
    }

    public virtual string nameSet
    {
        get
        {
            return base.name;
        }

        set
        {
            name_ = value;
        }
    }

    public override bool isClass
    {
        get
        {
            return true;
        }
    }

    protected virtual bool isClass_
    {
        get
        {
            return type.IsClass && !AEPropertyField.objectTypePrint.ContainsKey(type);
        }
    }

    public bool isMonoBehaviour__;

    public override bool isMonoBehaviour
    {
        get
        {
            return isMonoBehaviour__;
        }
    }

    protected virtual bool isMonoBehaviour_
    {
        get
        {
            return isMonoBehaviour__ = CheckMonoBehaviour(type);
        }
    }

    protected virtual bool CheckMonoBehaviour(Type type)
    {
        return monoBehaviourType.IsAssignableFrom(type);
    }

//=============================================================================================================================
//List
//=============================================================================================================================
    public override int count
    {
        get
        {
            return properties.Count;
        }
    }

    public override void Clear()
    {
        properties.Clear();
    }

    public override int countVisible
    {
        get
        {
            int result = 1;
            if (!foldout) return result;
            foreach (AESerializedProperty property in this)
            {
                result += property.countVisible;
            }
            return result;
        }
    }

//=============================================================================================================================
//Method call
//=============================================================================================================================
    public override object MethodCall(string name, object[] parameters)
    {
        MethodInfo methodInfo = type.GetMethod(name);
        if (methodInfo == null) return null;
        return methodInfo.Invoke(value, parameters);
    }

//=============================================================================================================================
//get property
//=============================================================================================================================
    public override AESerializedProperty GetPropertyPath(string name)
    {
        string[] namesProperty = name.Split('.');
        if (namesProperty.Length == 0) return null;
        AESerializedProperty propertySelect = this;
        foreach (string nameProperty in namesProperty) propertySelect = propertySelect.GetProperty(nameProperty);
        return propertySelect;
    }

    public override AESerializedProperty GetProperty(object name, bool silent = false)
    {
        if (properties.ContainsKey(name))
        {
            return properties[name];
        }
        else
        {
            if (silent) return null; 
            Debug.Log(this.GetType() + ": PropertyField: not object " + name + " " + isList);
            PrintDebug();
            return null;
        }
    }

    public override AESerializedProperty GetPropertyAt(int index)
    {
        return properties.GetPropertyAt(index);
    }

    public override string[] GetNamesChildren()
    {
        string[] result = new string[count];
        int i = 0;
        foreach (AESerializedProperty property in this)
        {
            result[i++] = property.name;
        }
        return result;
    }

    public override bool PropertyContainsKey(object key)
    {
        return properties.ContainsKey(key);
    }

    public override AESerializedProperty GetPropertyFirst()
    {
        List<AESerializedProperty>.Enumerator ie = properties.GetEnumerator();
        ie.MoveNext();
        return ie.Current;
    }

    public override AESerializedProperty this[object key]
    {
        get
        {
            if (properties.ContainsKey(key))
            {
                return properties[key];
            }
            else
            {
                return null;
            }
        }
    }

    public override T GetProperty<T>(string name)
    {
        PropertyInfo propertyInfo = type.GetProperty(name);
        if (propertyInfo == null) return default(T);
        return (T)propertyInfo.GetValue(value, null);
    }

    public override UnityEditor.SerializedProperty GetUnintySPChild(string name)
    {
        return parent.GetUnintySPChild(this.name).FindPropertyRelative(name);
    }

    /*public virtual Dictionary<string, AESerializedProperty>.ValueCollection.Enumerator GetEnumerator()//public virtual IEnumerator GetEnumerator() //IEnumerable.GetEnumerator()
    {
        return new Dictionary<string, AESerializedProperty>.ValueCollection.Enumerator();
    }*/

    /*public override List<AESerializedProperty>.Enumerator GetEnumerator()//public virtual IEnumerator GetEnumerator() //IEnumerable.GetEnumerator()
    {
        return properties.GetEnumerator();
    }*/
}
