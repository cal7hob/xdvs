using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AESerializedItemArray : AESerializedItemIList
{
    public AESerializedItemArray(AESerializedArray parent, object object_, int id) : base(parent, object_, id)
    {
        parentArray = parent;
        //this.id = id;
    }

    public AESerializedArray parentArray;

    public override bool SetValue(object value)
    {
        if (!isClass)
        {
            if (id == int.MaxValue) return false;
            IList list = (IList)parent.value;
            //if (list.Count <= id) return;
            object item = list[id];
            if (item != null && item.Equals(value)) return false;
            //if (this.value != null && this.value.Equals(value)) return false;
            //Debug.Log(this.GetType() + ": set item " + parent.name + " " + id);
            list[id] = value;
        }
        objectParse = value;
        return true;
    }

    public override void ListRemoveThisItem()
    {
        //((IList)parent.value).Remove(value);
        parentArray.trashId.Add(id);
        id = int.MaxValue;
    }

    public override void Move(int index)
    {
        index = Mathf.Clamp(index, 0, parent.count - 1);
        List<object> iList = new List<object>();
        int i = 0;
        for (; i < parent.count; i++) iList.Add(parent[i].value);//valueArray.valueArray.GetValue(i)
        iList.Remove(value);
        iList.Insert(index, value);
        AESerializedArray valueArray = (AESerializedArray)parent;
        for (i = 0; i < parent.count; i++) valueArray.valueArray.SetValue(iList[i], i);
        AEPropertyDictionary sortedProperties = new AEPropertyListDictionary();
        AESerializedProperty itemProperty;
        i = 0;
        foreach (object item in iList)
        {
            sortedProperties.Add(i, itemProperty = parent.properties.GetProperty(item));
            ((AESerializedItemIList)itemProperty).id = i;
            i++;
        }
        parent.properties = sortedProperties;
    }

    /*public override void RemoveThisItemImmediate()
    {
        //((IDictionary)parent.value).Remove(key);
        //parentList.keys.
        parentArray.RemoveImmediate(key);
        parent.properties.Remove(key);
        //parent.properties.Remove(this);
    }*/
}