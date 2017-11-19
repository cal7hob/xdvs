using System.Collections.Generic;

public class AEPropertyClassDictionary : AEPropertyDictionary
{
    protected List<object> keys = new List<object>();
    protected List<AESerializedProperty> values = new List<AESerializedProperty>();

    public override AESerializedProperty GetPropertyAt(int index)
    {
        return values[index];
    }

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

    public override void Add(object key, AESerializedProperty value)//, bool extendKeyValue
    {
        keys.Add(key);
        values.Add(value);
    }

    public override bool Remove(object key)
    {
        int index = keys.IndexOf(key);
        if (index != -1)
        {
            keys.RemoveAt(index);
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
            values.RemoveAt(index);
            return true;
        }
        return false;
    }

    public override bool ContainsKey(object key)
    {
        return keys.Contains(key);
    }

    public override List<object> Keys
    {
        get
        {
            return keys;
        }
    }

    public override List<AESerializedProperty> Values
    {
        get
        {
            return values;
        }
    }

    public override int Count
    {

        get
        {
            return values.Count;
        }
    }

    public override void Clear()
    {
        keys.Clear();
        values.Clear();
    }

    public override List<AESerializedProperty>.Enumerator GetEnumerator()
    {
        return values.GetEnumerator();
    }
}