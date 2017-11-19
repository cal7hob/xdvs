using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class AESerializedList : AESerializedIList
{
    public AESerializedList(AESerializedProperty parent, FieldInfo fieldInfo): base(parent,fieldInfo) { }

//=============================================================================================================================
//Parser
//=============================================================================================================================
    public override void Parse()
    {
        if (objectParse == null) return;
        if (CustomAttributeIsDefined(typeof(AECustomAttributes)))
        {
            object[] params_ = GetCustomAttributeParams(AECustomAttributes.eType.ListKey);
            if (params_ != null)
            {
                keyName = (string)params_[0];
            }
        }

        AESerializedItemList serializedItemList;
        if (keyName != null)
        {
            AESerializedProperty serializedKey;
            foreach (object item in objectParse as IList)
            {
                serializedItemList = new AESerializedItemList(this, item, id);
                serializedItemList.isMonoBehaviour__ = itemIsMonoBehaviour;
                serializedKey = serializedItemList[keyName];
                properties.Add(serializedKey.value, serializedItemList);
                serializedItemList.nameSet = serializedKey.value.ToString();
                serializedObject.SubscribeChangeProperty(ChangeKey, serializedKey);
                id++;
            }
            return;
        }

        foreach (object item in objectParse as IList)
        {
            serializedItemList = new AESerializedItemList(this, item, id);
            properties.Add(id, serializedItemList);
            serializedItemList.isMonoBehaviour__ = itemIsMonoBehaviour;
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
        id = 0;
        foreach (object item in objectParse as IList)
        {
            property = properties.GetProperty(item);
            if (property  == null)
            {
                AESerializedItemList itemProperty = new AESerializedItemList(this, item, id);
                itemProperty.isMonoBehaviour__ = itemIsMonoBehaviour;
                if (keyName != null)
                {
                    newProperties.Add(property[keyName].value, itemProperty);
                }
                else
                {
                    newProperties.Add(id, itemProperty);
                }
            }
            else
            {
                newProperties.Add(id, property);
                ((AESerializedItemList)property).id = id;
                property.Update();
            }
            id++;
        }
        properties = newProperties;
    }

    public virtual bool CheckChangeItems()
    {
        IList objectParseList = (IList)objectParse;
        if (objectParseList.Count != properties.Count) return true;
        foreach (object item in objectParse as IList)
        {
            if (properties.GetProperty(item) == null) return true;
        }
        return false;
    }

//=============================================================================================================================
//properties object
//=============================================================================================================================
    public override bool isList
    {
        get
        {
            return true;
        }
    }

    public override bool ContainsKey(object key)
    {
        return ((IList)value).Contains(key);
    }

    public override int IndexOf(object key)
    {
        return ((IList)value).IndexOf(key);
    }

    //public delegate int JNComparison(object x, object y);

    public override void Sort<T>(Comparison<T> comparison) //object //IComparer //IComparer comparison
    {
        List<T> list = (List<T>)value;
        list.Sort(comparison);
        AEPropertyDictionary sortedProperties = new AEPropertyListDictionary();
        int i = 0;
        foreach (T item in list)
        {
            sortedProperties.Add(i, properties.GetProperty(item));
            i++;
        }
        properties = sortedProperties;
    }

//=============================================================================================================================
//List
//=============================================================================================================================
    public override AESerializedItem Add()
    {
        return Add(GetItemDefaultObject());
    }

    public override AESerializedItem Add(object[] args) //params
    {
        return Add(Activator.CreateInstance(itemType, args));
    }

    public override AESerializedItem Add(object value)
    {
        ((IList)this.value).Add(value);
        AESerializedItem item = new AESerializedItemList(this, value, id);
        item.isMonoBehaviour__ = itemIsMonoBehaviour;
        if (keyName != null)
        {
            serializedObject.SubscribeChangeProperty(ChangeKey, item[keyName]);
            properties.Add(item[keyName].value, item);
        }
        else
        {
            properties.Add(id, item);
        }
        id++;
        serializedObject.Change(this);
        return item;
    }

    public override Type itemType
    {
        get
        {
            //http://stackoverflow.com/questions/1043755/c-sharp-generic-list-t-how-to-get-the-type-of-t
            return type.GetGenericArguments()[0];
        }
    }

    public override void ListRemove(AESerializedProperty property)
    {
        int index = properties.Values.IndexOf(property);
        if (index == -1)
        {
            Debug.Log(this.GetType() + ": error ListRemove(AESerializedProperty property) " + index);
            return;
        }
        if (trashId.Contains(index))
        {
            Debug.Log(this.GetType() + ": duplicate remove ListRemove(AESerializedProperty property) " + index);
            return;
        }
        
        trashId.Add(index);
        serializedObject.Change(property);
    }

    public override void ListRemove(object value)
    {
        ListRemove(properties.GetProperty(value));
    }

    public override void Clear()
    {
        base.Clear();
        ((IList)value).Clear();
        serializedObject.Change(this);
    }

    public override bool Synchronize()
    {
        /*if (trashId.Count > 0) // safe remove for foreach
        {
            IList list = (IList)value;
            AEPropertyDictionary propertiesSort = new AEPropertyListDictionary(); // temp fix (add in AEPropertyDictionary create or synhronize)
            id = 0;
            foreach (AESerializedItemDictionary property in properties) //AESerializedProperty AESerializedItemDictionary
            {
                if (property.id == int.MaxValue)
                {
                    list.Remove(property.value);
                    //RemoveImmediate(property.dictKey);
                }
                else
                {
                    propertiesSort.Add(property.dictKey, property);
                    property.id = id;
                    id++;
                }
            }
            properties = propertiesSort;
            trashId.Clear();
        }*/
        if (trashId.Count > 0) // safe remove for foreach
        {
            serializedObject.Change(this); // for fix control modify and save on prefab
            trashId.Sort(delegate(int us1, int us2) { return -us1.CompareTo(us2); });
            IList list = (IList)value;
            //Debug.Log(this.GetType() + ": Synchronize()");
            foreach (int idRemove in trashId)
            {
                //Debug.Log(this.GetType() + ": trashId " + idRemove);
                AESerializedItemIList item = (AESerializedItemIList)properties.GetPropertyAt(idRemove);
                if (item != null)
                {
                    properties.Remove(item);
                    list.RemoveAt(idRemove);
                }
                else
                {
                    Debug.Log(this.GetType() + ": error parent.properties.Remove(name) " + PathDebug());
                }
            }
            trashId.Clear();

            AEPropertyDictionary propertiesSort = new AEPropertyListDictionary(); // temp fix (add in AEPropertyDictionary create or synhronize)
            id = 0;

            if (keyName != null)
            {
                foreach (AESerializedItemList property in properties)
                {
                    propertiesSort.Add(property[keyName].value, property);
                    property.id = id;
                    id++;
                }
            }
            else
            {
                foreach (AESerializedItemList property in properties)
                {
                    propertiesSort.Add(id, property);
                    property.id = id;
                    id++;
                }
            }
            properties = propertiesSort;
            return true;
        }
        return false;
    }
}
