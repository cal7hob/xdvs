using System.Collections;

public class AESerializedItemDictionary : AESerializedItemIDictionary
{
    public AESerializedItemDictionary(AESerializedDictionary parent, object key, object value, int id) : base(parent, key, value, id)
    {
        parentList = parent;
        dictKey_ = new AESerializedItemKey(parent, key, id);
    }
    
    public AESerializedDictionary parentList;

    public override bool SetValue(object value)
    {
        if (!isClass)
        {
            if (id == int.MaxValue) return false;
            //Debug.Log(this.GetType() + ": set item " + parent.type + " " + id);
            IList list = parentList.values;// (IList)parent.value;
            //if (list.Count <= id) return;
            if (list[id].Equals(value)) return false;
            //Debug.Log(this.GetType() + ": set item " + parent.name + " " + id);
            list[id] = value;
        }
        objectParse = value;
        return true;
    }

    public override void ListRemoveThisItem()
    {
        //((IList)parent.value).Remove(value);
        parentList.trashId.Add(id);
        id = int.MaxValue;
    }
}