using System.Collections;
using System.Collections.Generic;

public class AEPropertyDictionary : IEnumerable
{
    public virtual AESerializedProperty this[object key]
    {
        get { return null; }
    }

    public virtual AESerializedProperty GetProperty(object keyValue)
    {
        return null;
    }

    public virtual AESerializedProperty GetPropertyAt(int index)
    {
        return null;
    }

    public virtual void Add(object key, AESerializedProperty value) { }

    public virtual bool Remove(object key)
    {
        return false;
    }

    public virtual bool Remove(AESerializedProperty property)
    {
        return false;
    }

    public virtual bool ContainsKey(object key)
    {
        return false;
    }

    public virtual List<object> Keys { get { return null; } }

    public virtual List<AESerializedProperty> Values { get { return null; } }

    public virtual int Count { get { return 0; } }

    public virtual void Clear() { }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return Values.GetEnumerator();
    }

    public virtual List<AESerializedProperty>.Enumerator GetEnumerator()
    {
        return Values.GetEnumerator();
    }
}