using System.Collections;
using UnityEngine;

public class AESerializedItemList : AESerializedItemIList
{
    public AESerializedItemList(AESerializedList parent, object object_, int id): base(parent, object_, id)
    {
        parentList = parent;
        //this.id = id;
    }

    public AESerializedList parentList;
    //public int id = 0;

    /*public override string name
    {
        get
        {
            if (!isClass) return "Item " + id;
            return base.name;
        }
    }*/

    public override bool SetValue(object value)
    {
        if (!isClass)
        {
            if (value == null) return false;
            int index = parent.properties.Values.IndexOf(this);
            if (index == -1) return false;
            IList list = (IList)parent.value;
            if (list[index] != null && list[index].Equals(value)) return false;
            //Debug.Log(this.GetType() + ": set item " + parent.name + " " + id);
            list[index] = value;
            objectParse = value;
            //if (parentList.keyName != null && parentList.keyName == name) parent.properties.Keys[parent.properties.Values.IndexOf(this)] = value;
            return true;
        }
        else
        {
            objectParse = value;
            return true;
        }
    }

    public override void ListRemoveThisItem()
    {
        //((IList)parent.value).Remove(value);
        /*parentList.trashId.Add(id);
        id = int.MaxValue;*/
        parent.ListRemove(this);
    }

    public override void Move(int index)
    {
        index = Mathf.Clamp(index, 0, parent.count -1);
        IList iList = (IList)parent.value;
        //iList.Remove(value);
        iList.RemoveAt(id);
        iList.Insert(index, value);

        AEPropertyDictionary sortedProperties = new AEPropertyListDictionary();
        int i = 0;
        AESerializedProperty itemProperty;
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
        ((IList)parent.value).Remove(value);
        parent.properties.Remove(this);
    }*/

    /*public override UnityEditor.SerializedProperty GetUnintySPChild(string name)
    {
        return parent.GetUnintySPChild(id.ToString()).FindPropertyRelative(name);
    }*/
}