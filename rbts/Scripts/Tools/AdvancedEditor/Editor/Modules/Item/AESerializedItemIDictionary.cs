using System;
using UnityEngine;

public class AESerializedItemIDictionary : AESerializedItemIList
{
    public AESerializedItemIDictionary(AESerializedProperty parent, object key, object value, int id) : base(parent, value, id) { }
    
    public AESerializedItemIList dictKey_;
    
    public override AESerializedItemIList dictKeyItem
    {
        get
        {
            return dictKey_;
        }

        set
        {
            Debug.Log(this.GetType() + ": dictionaryKey: set: not set key! " + name);
        }
    }

    public override object dictKey
    {
        get
        {
            return dictKey_.value;
        }

        set
        {
            Debug.Log(this.GetType() + ": dictionaryKey: set: not set key! " + name);
        }
        
    }

    public override Type itemTypeKey
    {
        get
        {
            return dictKey_.type;
        }
    }
}