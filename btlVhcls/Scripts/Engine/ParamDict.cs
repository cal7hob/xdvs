using System.Collections.Generic;

public class ParamDict : Dictionary<string, object>
{
    public ParamDict Add(string key, object value, bool overrideValue = true)
    {
        if (ContainsKey(key) && overrideValue)
        {
            this[key] = value;
        }
        else if (!ContainsKey(key))
            base.Add(key, value);
        return this;
    }

    public object GetValue(string key)
    {
        if (ContainsKey(key)) return this[key];
        else return null;
    }
}
