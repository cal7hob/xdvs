using UnityEngine;

public class AESerializedItemKeyNative : AESerializedItemIList
{
    public AESerializedItemKeyNative(AESerializedDictionaryNative parent, object object_, int id) : base(parent, object_, id) //AESerializedDictionary
    {
        parentList = parent;
    }

    public AESerializedDictionaryNative parentList;
    
    public override bool SetValue(object value)
    {
        if (!isClass)
        {
            if (id == int.MaxValue) return false;
            //IList list = parentList.keys;//parent.dictKeyItem.value;
            //if (list.Count <= id) return;
            if (!parentList.iDictionary.Contains(this.value)) return false;
            if (this.value.Equals(value)) return false;

            //Debug.Log(this.GetType() + ": set item " + parent.name + " " + id);
            parentList.ChangeKey(this.value, value);
        }
        objectParse = value;
        return true;
    }

    public override void ListRemoveThisItem()
    {
        Debug.Log(this.GetType() + ": ListRemoveThisItem(): not support, use AESerializedItemDictionary: " + type);
    }
}