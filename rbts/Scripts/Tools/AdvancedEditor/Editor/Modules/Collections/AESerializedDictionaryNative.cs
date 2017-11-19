using UnityEngine;
using System;
using System.Reflection;
using System.Collections;

public class AESerializedDictionaryNative : AESerializedIList
{
    public AESerializedDictionaryNative(AESerializedProperty parent, FieldInfo fieldInfo) : base(parent, fieldInfo)
    {
        if (!itemKeyIsClass) newProperty = new AESerializedItem(this, AEPropertyField.GetDefaultObject(itemTypeKey));
    }

    public AESerializedItem newProperty;
    public IDictionary iDictionary;

//=============================================================================================================================
//Parser
//=============================================================================================================================
    public override void Parse()
    {
        //foreach (KeyValuePair<object, object> item in objectParse as IDictionary)
        if (objectParse == null) return;
        iDictionary = objectParse as IDictionary;
        foreach (DictionaryEntry item in iDictionary)
        {
            properties.Add(item.Key, new AESerializedItemDictionaryNative(this, item.Key, item.Value, id));
            id++;
        }
    }

    public override void Update()
    {
        iDictionary = objectParse as IDictionary;
        foreach (DictionaryEntry item in iDictionary)
        {
            Debug.Log(this.GetType() + ": item " + item.Key);
        }
        return;

        /*if (!CheckChangeItems())
        {
            base.Update();
            return;
        }

        AEPropertyDictionary newProperties = new AEPropertyListDictionary();
        AESerializedProperty property;
        id = 0;
        foreach (DictionaryEntry item in iDictionary)
        {
            property = properties.GetProperty(item);
            if (property  == null)
            {
                newProperties.Add(id.ToString(), new AESerializedItemDictionaryNative(this, item.Key, item.Value, id));
                //Debug.Log(this.GetType() + ": new item " + Name + " " + id + " " + properties.GetType());
                //Debug.Log(this.GetType() + ": new item " + Name + " " + id + " " + properties.GetType() + " " + item.GetHashCode() + " old " + properties[id.ToString()].value.GetHashCode());
            }
            else
            {
                //Debug.Log(this.GetType() + ": item " + Name + " " + id + " " + properties.GetType() + " " + item.GetHashCode() + " old " + properties[id.ToString()].value.GetHashCode());
                newProperties.Add(id.ToString(), property);
                property.Update();
            }
            id++;
        }
        properties = newProperties;*/
    }

    bool CheckChangeItems()
    {
        //IDictionary objectParseList = (IDictionary)objectParse;
        if (iDictionary.Count != properties.Count) return true;
        foreach (DictionaryEntry item in iDictionary)
        {
            if (properties.GetProperty(item.Value) == null) return true;
        }
        return false;
    }

//=============================================================================================================================
//clone object
//=============================================================================================================================
    public override void CloneProperty(AESerializedProperty fieldObject) { }

    /*public override void CloneProperty(AESerializedProperty fieldObject)
    {
        if (value == null) return;
        if (fieldObject.value == null && isClass) Activator.CreateInstance(fieldObject.type);
        if (fieldObject.type != type)
        {
            Debug.Log(this.GetType() + ": fieldObject.type != type");
            return;
        }

        if (!fieldObject.isList)
        {
            Debug.Log(this.GetType() + ": fieldObject not list");
            return;
        }

        fieldObject.Clear();
            
        foreach (AESerializedProperty property in this)
        {
            //if (property.value == null) return;
            property.CloneProperty(fieldObject.ListAdd());
        }
    }*/

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
        Debug.Log(this.GetType() + ": ListAdd(): non suport add non key: " + type);
        return null;
    }

    public override AESerializedItem DictAdd(object key)
    {
        object newObject = GetItemDefaultObject();
        iDictionary.Add(key, newObject);
        AESerializedItem item = new AESerializedItemDictionaryNative(this, key, newObject, id);
        item.isMonoBehaviour__ = itemIsMonoBehaviour;
        properties.Add(key, item);
        id++;
        return item;
    }

    /*public override AESerializedItem Add(object key)
    {
        object newObject = Activator.CreateInstance(itemType);
        //object newObjectKey = Activator.CreateInstance(itemTypeKey);
        ((IDictionary)value).Add(key, newObject);
        //((Dictionary<int, JNModuleHangar>)value).Add((int)key, (JNModuleHangar)newObject);

        AESerializedItem item = new AESerializedItemDictionaryNative(this, newObject, key, id);
        properties.Add(id, item);
        id++;
        return item;
    }*/

    public override Type itemType
    {
        get
        {
            //http://stackoverflow.com/questions/1043755/c-sharp-generic-list-t-how-to-get-the-type-of-t
            return type.GetGenericArguments()[1];
        }
    }

    public override Type itemTypeKey
    {
        get
        {
            return type.GetGenericArguments()[0];
        }
    }

    public override bool itemKeyIsClass
    {
        get
        {
            Type itemType = itemTypeKey;
            return itemType.IsClass && !AEPropertyField.objectTypePrint.ContainsKey(itemType);
        }
    }

    public override void Clear()
    {
        base.Clear();
        ((IDictionary)value).Clear();
    }

    public override void ListRemove(AESerializedProperty property)
    {
        AESerializedItemList propertyItem = (AESerializedItemList)property;
        trashId.Add(propertyItem.id);
        propertyItem.id = int.MaxValue;
    }

    public override bool Synchronize()
    {
        //foreach (KeyValuePair<object, object> item in objectParse as IDictionary)

        if (trashId.Count > 0) // safe remove for foreach
        {
            AEPropertyDictionary propertiesSort = new AEPropertyListDictionary(); // temp fix (add in AEPropertyDictionary create or synhronize)
            id = 0;
            foreach (AESerializedItemDictionaryNative property in properties)
            {
                if (property.id == int.MaxValue)
                {
                    iDictionary.Remove(property.dictKey); //RemoveImmediate(property.dictKey);
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

        /*if (trashId.Count > 0) // safe remove for foreach
        {
            IDictionary iDictionary = objectParse as IDictionary;
            if (iDictionary != null)
            {
                AEPropertyDictionary propertiesSort = new AEPropertyListDictionary(); // temp fix (add in AEPropertyDictionary create or synhronize)
                id = 0;
                foreach (AESerializedItemDictionaryNative property in this) //AESerializedProperty
                {
                    if (property.id == int.MaxValue)
                    {
                        iDictionary.Remove(property.key);
                    }
                    else
                    {
                        propertiesSort.Add(id.ToString(), property);
                        property.id = id;
                        id++;
                    }
                }

                properties = propertiesSort;
            }
            else
            {
                Debug.Log(this.GetType() + ": not convert to IDictionary " + name);
            }
            trashId.Clear();
            return false;
        }*/

        /*if (trashId.Count > 0) // safe remove for foreach
        {
            IDictionary list = (IDictionary)value;
            foreach (int idRemove in trashId)
            {
                if (properties.Remove(idRemove.ToString()))
                {
                    //list.RemoveAt(idRemove);
                    list.Remove(idRemove);
                }
                else
                {
                    Debug.Log(this.GetType() + ": error parent.properties.Remove(name)");
                }
            }
            trashId.Clear();

            AEPropertyDictionary propertiesSort = new AEPropertyListDictionary(); // temp fix (add in AEPropertyDictionary create or synhronize)
            id = 0;
            foreach (AESerializedItemList property in this) //AESerializedProperty
            {
                propertiesSort.Add(id.ToString(), property);
                property.id = id;
                id++;
            }
            properties = propertiesSort;
        }*/
        //return false;
    }

    public void ChangeKey(object key, object newKey)
    {
        IDictionary newIDictionary = (IDictionary)Activator.CreateInstance(type, null);
        foreach (DictionaryEntry item in iDictionary) //KeyValuePair<object, object>
        {
            if (item.Key == key)
            {
                newIDictionary.Add(newKey, item.Value);
            }
            else
            {
                newIDictionary.Add(item.Key, item.Value);
            }
        }
        value = iDictionary = newIDictionary;
    }
}
