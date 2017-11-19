using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

public class AESerializedDictionary : AESerializedIList
{
    public AESerializedDictionary(AESerializedProperty parent, FieldInfo fieldInfo) : base(parent, fieldInfo)
    {
        if (!itemKeyIsClass) newProperty = new AESerializedItem(this, AEPropertyField.GetDefaultObject(itemTypeKey)); //AEPropertyField.typeToObjectDefault[itemTypeKey]
    }

    private AESerializedProperty keysProperty;
    private AESerializedProperty valuesProperty;
    public IList keys;
    public IList values;
    public AESerializedItem newProperty;

//=============================================================================================================================
//Parser
//=============================================================================================================================
    public override void Parse()
    {
        FieldInfo[] fieldInfos = type.GetFields();
        foreach (FieldInfo fieldInfo in fieldInfos)
        {
            properties.Add(fieldInfo.Name, new AESerializedProperty(this, fieldInfo));
        }

        keysProperty = properties["keys"];
        valuesProperty = properties["values"];

        if (keysProperty == null || valuesProperty == null)
        {
            Debug.Log(this.GetType() + ": keys or values == null " + Name);
            PrintDebug();
        }
        properties.Clear();

        keys = keysProperty.value as IList;
        values = valuesProperty.value as IList;

        AESerializedItemDictionary item;
        for(int i = 0; i<values.Count; i++)
        {
            item = new AESerializedItemDictionary(this, keys[i], values[i], id);
            item.isMonoBehaviour__ = itemIsMonoBehaviour;
            properties.Add(keys[i], item);
            id++;
        }
    }

    public override void Update()
    {
        if (!CheckChangeItems())
        {
            base.Update();
            return;
        }

        AEPropertyDictionary newProperties = new AEPropertyListDictionary();
        AESerializedProperty property;
        
        object item;
        id = 0;
        for (int i = 0; i < values.Count; i++)
        {
            item = values[i];
            property = properties.GetProperty(item);
            if (property  == null)
            {
                AESerializedItemDictionary itemProperty = new AESerializedItemDictionary(this, keys[i], item, id);
                itemProperty.isMonoBehaviour__ = itemIsMonoBehaviour;
                newProperties.Add(keys[i], itemProperty);
                Debug.Log(this.GetType() + ": new item " + Name + " " + id + " " + properties.GetType());
                //Debug.Log(this.GetType() + ": new item " + Name + " " + id + " " + properties.GetType() + " " + item.GetHashCode() + " old " + properties[id.ToString()].value.GetHashCode());
            }
            else
            {
                //Debug.Log(this.GetType() + ": item " + Name + " " + id + " " + properties.GetType() + " " + item.GetHashCode() + " old " + properties[id.ToString()].value.GetHashCode());
                newProperties.Add(keys[i], property);
                property.Update();
            }
            id++;
        }
        properties = newProperties;
    }

    bool CheckChangeItems()
    {
        if (values.Count != properties.Count) return true;
        foreach (object item in values)
        {
            if (properties.GetProperty(item) == null) return true;
        }
        return false;
    }

//=============================================================================================================================
//clone object
//=============================================================================================================================
    public override void CloneProperty(AESerializedProperty fieldObject) { Debug.Log(this.GetType() + ": CloneProperty not support temp fix"); }

//=============================================================================================================================
//properties object
//=============================================================================================================================
    public override bool isDictionary
    {
        get
        {
            return true;
        }
    }

//=============================================================================================================================
//List
//=============================================================================================================================
    public override AESerializedItem itemNewKey
    {
        get
        {
            if (itemKeyIsClass)
            {
                //Debug.Log(this.GetType() + ": itemNewKey: itemKeyIsClass");
                return new AESerializedItem(this, Activator.CreateInstance(itemTypeKey));
            }
            else
            {
                //Debug.Log(this.GetType() + ": itemNewKey: not class");
                return newProperty;
            }
            //return itemKeyIsClass ? new AESerializedItem(this, Activator.CreateInstance(itemTypeKey)) : newProperty; //Key
        }
    }

    public override AESerializedItem Add()
    {
        Debug.Log(this.GetType() + ": ListAdd(): not suport add, not key: " + type);
        return null;
    }

    public override AESerializedItem DictAdd(object key)
    {
        object newObject = GetItemDefaultObject();
        keys.Add(key);
        values.Add(newObject);
        AESerializedItem item = new AESerializedItemDictionary(this, key, newObject, id);
        item.isMonoBehaviour__ = itemIsMonoBehaviour;
        properties.Add(key, item);
        id++;
        return item;
    }

    public override Type itemType
    {
        get
        {
            return valuesProperty.type.GetGenericArguments()[0];
        }
    }

    public override Type itemTypeKey
    {
        get
        {
            return keysProperty.type.GetGenericArguments()[0];
        }
    }

    public override bool itemKeyIsClass// not working
    {
        get
        {
            Type itemType = itemTypeKey;
            return itemType.IsClass && !AEPropertyField.objectTypePrint.ContainsKey(itemType);
        }
    }

    public override void ListRemove(AESerializedProperty property)
    {
        AESerializedItemList propertyItem = (AESerializedItemList)property;
        trashId.Add(propertyItem.id);
        propertyItem.id = int.MaxValue;
    }

    public virtual bool RemoveImmediate(object key)
    {
        int index = keys.IndexOf(key);
        if (index != -1)
        {
            values.RemoveAt(index);
            keys.RemoveAt(index);
            return true;
        }
        return false;
    }

    public override void Clear()
    {
        base.Clear();
        keys.Clear();
        values.Clear();
    }

    public override bool Synchronize()
    {
        if (trashId.Count > 0) // safe remove for foreach
        {
            AEPropertyDictionary propertiesSort = new AEPropertyListDictionary(); // temp fix (add in AEPropertyDictionary create or synhronize)
            id = 0;
            foreach (AESerializedItemDictionary property in properties)
            {
                if (property.id == int.MaxValue)
                {
                    RemoveImmediate(property.dictKey);
                }
                else
                {
                    propertiesSort.Add(property.dictKey, property);
                    property.id = id;
                    property.dictKeyItem.id = id;
                    id++;
                }
            }
            properties = propertiesSort;
            trashId.Clear();
            return true;
        }
        return false;
    }
}
