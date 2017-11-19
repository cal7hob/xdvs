public class AESerializedItemDictionaryNative : AESerializedItemIDictionary //AESerializedItemIList
{
    public AESerializedItemDictionaryNative(AESerializedDictionaryNative parent, object key, object object_, int id): base(parent, key, object_, id)
    {
        parentList = parent;
        dictKey_ = new AESerializedItemKeyNative(parent, key, id);
    }
    
    public AESerializedDictionaryNative parentList;
    
    public override bool SetValue(object value)
    {
        if (!isClass)
        {
            if (id == int.MaxValue) return false;
            //Debug.Log(this.GetType() + ": set item " + parent.type + " " + id);
            //IList list = parentList.values;// (IList)parent.value;
            //if (list.Count <= id) return;
            //IDictionary dic = (IDictionary)parentList.value;
            if (!parentList.iDictionary.Contains(dictKey_.value)) return false;
            if (parentList.iDictionary[dictKey_.value].Equals(value)) return false;
            //Debug.Log(this.GetType() + ": set item " + parent.name + " " + id);
            parentList.iDictionary[dictKey_.value] = value;
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