using System.Collections.Generic;

public class AEPropertyListDictionary : AEPropertyClassDictionary
{
    private List<object> keysValue = new List<object>();

    public override AESerializedProperty this[object key]
    {
        get
        {
            int id = keys.IndexOf(key);
            if (id == -1)
            {
                return null;
            }
            else
            {
                return values[id];
            }
        }
    }

    public override AESerializedProperty GetProperty(object keyValue)
    {
        int id = keysValue.IndexOf(keyValue);
        if (id == -1)
        {
            return null;
        }
        else
        {
            return values[id];
        }
    }

    public override void Add(object key, AESerializedProperty value)//, bool extendKeyValue
    {
        keys.Add(key);
        keysValue.Add(value.value);
        values.Add(value);
    }

    public override bool Remove(object key)
    {
        int index = keys.IndexOf(key);
        if (index != -1)
        {
            keys.RemoveAt(index);
            keysValue.RemoveAt(index);
            values.RemoveAt(index);
            return true;
        }
        return false;
    }

    public override bool Remove(AESerializedProperty property)
    {
        int index = values.IndexOf(property);
        if (index != -1)
        {
            keys.RemoveAt(index);
            keysValue.RemoveAt(index);
            values.RemoveAt(index);
            return true;
        }
        return false;
    }

    public override void Clear()
    {
        keys.Clear();
        keysValue.Clear();
        values.Clear();
    }
}