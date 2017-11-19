using System.Collections;
using System.Collections.Generic;

public class AEDictionary<TKey, TValue> : IEnumerable
{
    public List<TKey> keys = new List<TKey>();
    public List<TValue> values = new List<TValue>();
    private List<TKey> trash = new List<TKey>();

    public virtual TValue this[TKey key]
    {
        get
        {
            int index = keys.IndexOf(key);
            if (index == -1)
            {
                return default(TValue);
            }
            return values[index];
        }
        set
        {
            int index = keys.IndexOf(key);
            if (index == -1)
            {
                Add(key, value);
            }
            else
            {
                values[index] = value;
            }
        }
    }

    public virtual void SetKey(TValue keyValue, TKey newKey)
    {
        keys[values.IndexOf(keyValue)] = newKey;
    }

    public virtual TKey GetKey(TValue value)
    {
        return keys[values.IndexOf(value)];
    }

    public virtual void Add(TKey key, TValue value)//, bool extendKeyValue
    {
        if (keys.Contains(key)) return;
        keys.Add(key);
        values.Add(value);
    }

    public virtual bool Remove(TKey key)
    {
        int index = keys.IndexOf(key);
        if (index != -1)
        {
            RemoveAt(index);
            return true;
        }
        return false;
    }

    public virtual void RemoveAt(int index)
    {
        values.RemoveAt(index);
        keys.RemoveAt(index);
    }
    
    public virtual bool RemoveSafe(TKey key)
    {
        if (keys.Contains(key))
        {
            if (trash.Contains(key))
            {
                return false;
            }
            else
            {
                trash.Add(key);
                return true;
            }
        }
        return false;
    }

    public bool Synchronize()
    {
        if (trash.Count == 0) return false;
        foreach (TKey key in trash) Remove(key);
        trash.Clear();
        return true;
    }

    public virtual bool ContainsKey(TKey key)
    {
        return keys.Contains(key);
    }

    public virtual int IndexOfKey(TKey key)
    {
        return keys.IndexOf(key);
    }

    public virtual List<TKey> Keys
    {
        get
        {
            return keys;
        }
    }

    public virtual List<TValue> Values
    {
        get
        {
            return values;
        }
    }

    public virtual int Count
    {

        get
        {
            return values.Count;
        }
    }

    public virtual void Clear()
    {
        keys.Clear();
        values.Clear();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        Synchronize();
        return values.GetEnumerator();
    }
}
