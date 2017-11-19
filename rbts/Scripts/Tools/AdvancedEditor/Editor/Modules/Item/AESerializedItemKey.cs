using UnityEngine;

public class AESerializedItemKey : AESerializedItemIList
{
    public AESerializedItemKey(AESerializedDictionary parent, object object_, int id) : base(parent, object_, id) //AESerializedDictionary
    {
        parentList = parent;
        //this.id = id;
    }

    public AESerializedDictionary parentList;
    //public int id = 0;

    /*public override string name
    {
        get
        {
            //if (!isClass) return "Item " + id;
            return base.name;
        }
    }*/
    
    public override bool SetValue(object value)
    {
        if (!isClass)
        {
            if (id == int.MaxValue) return false;
            //IList list = parentList.keys;//parent.dictKeyItem.value;
            //if (list.Count <= id) return;
            if (parentList.keys[id].Equals(value)) return false;
            //Debug.Log(this.GetType() + ": set item " + parent.name + " " + id);
            parentList.keys[id] = value;
        }
        objectParse = value;
        return true;
    }

    public override void ListRemoveThisItem()
    {
        Debug.Log(this.GetType() + ": ListRemoveThisItem(): not support, use AESerializedItemDictionary: " + type);
        //parentList.trashId.Add(id);
        //id = int.MaxValue;
    }

    /*public override void RemoveThisItemImmediate()
    {
        ((IList)parent.value).Remove(value);
        parent.properties.Remove(this);
    }*/
}