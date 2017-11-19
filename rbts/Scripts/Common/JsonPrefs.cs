using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public interface IAppPrefs
{
    /**
     * Returns a list of all keys, including subkeys, that can be read using the IAppPrefs object.
     *
     *  Example:
     *
     *  JsonPrefs settings;
     *  settings.setValue("fridge/color", QColor(Qt::white));
     *  settings.setValue("fridge/size", QSize(32, 96));
     *  settings.setValue("sofa", true);
     *  settings.setValue("tv", false);
     *
     *  List<string> keys = settings.allKeys();
     *  // keys: ["fridge/color", "fridge/size", "sofa", "tv"]
     *  If a group is set using beginGroup(), only the keys in the group are returned, without the group prefix:
     *
     *  settings.beginGroup("fridge");
     *  keys = settings.allKeys();
     *  // keys: ["color", "size"]
     *  See also childGroups() and childKeys().
     */
    //List<string> AllKeys ();
    List<string> ChildGroups ();
    List<string> ChildKeys ();

    string Group ();
    void BeginGroup (string prefix);
    void EndGroup ();

    //int BeginReadArray (string prefix);
    ////void BeginWriteArray (string prefix, int size = -1);
    //void EndArray ();
    //void SetArrayIndex (int i);

    bool Contains (string key);
    object Value (string key, object defaultValue = default(object));
    //void SetValue (string key, object value);
    //void Remove (string key);

    //void Clear ();

    bool ValueBool (string key, bool defaultValue = default(bool));
    string ValueString (string key, string defaultValue = default(string));
    int ValueInt (string key, int defaultValue = default(int));
    long ValueLong (string key, long defaultValue = default(long));
    float ValueFloat (string key, float defaultValue = default(float));
    double ValueDouble (string key, double defaultValue = default(double));
    List<object> ValueObjectList (string key, List<object> defaultValue = default(List<object>));
    Dictionary<string, object> ValueObjectDict (string key, Dictionary<string, object> defaultValue = default(Dictionary<string, object>));
}


public class JsonPrefs : IAppPrefs
{
    public JsonPrefs (object data)
    {
        m_data = data;
        m_root = m_data;
    }

    public JsonPrefs (string json)
    {
        try {
            m_data = MiniJSON.Json.Deserialize (json);
            m_root = m_data;
        }
        catch (Exception e) {
            Debug.LogError ("JsonPrefs: Can't deserialize JSON data, error: " + e.Message);
        }
        if (m_data == null) {
            Debug.LogWarning ("JsonPrefs: Deserialized data is null, " + json);
        }
    }

    public override string ToString ()
    {
        return MiniJSON.Json.Serialize (m_data);
    }




    //public List<string> AllKeys ()
    //{
    //    return new List<string>();
    //}

    public List<string> ChildGroups ()
    {
        var ret = new List<string> ();
        if (m_root == null) {
            return ret;
        }
        if (IsDict (ref m_root)) {
            foreach (var pair in m_root as Dictionary<string, object>) {
                var v = pair.Value;
                if (IsContainer (ref v)) {
                    ret.Add (pair.Key);
                }
            }
        }
        else if (IsList (ref m_root)) {
            var list = m_root as List<object>;
            for (int i = 0; i < list.Count; i++) {
                var v = list[i];
                if (IsContainer (ref v)) {
                    ret.Add (i.ToString ());
                }
            }
        }
        return ret;
    }

    public List<string> ChildKeys ()
    {
        var ret = new List<string> ();
        if (m_root == null) {
            return ret;
        }
        if (IsDict (ref m_root)) {
            foreach (var pair in m_root as Dictionary<string, object>) {
                var v = pair.Value;
                if (!IsContainer (ref v)) {
                    ret.Add (pair.Key);
                }
            }
        }
        else if (IsList (ref m_root)) {
            var list = m_root as List<object>;
            for (int i = 0; i < list.Count; i++) {
                var v = list[i];
                if (!IsContainer (ref v)) {
                    ret.Add (i.ToString ());
                }
            }
        }
        return ret;
    }



    public string Group ()
    {
        int cnt = m_prefix.Count;
        if (cnt > 0) {
            return m_prefix[cnt-1];
        }
        else {
            return "";
        }
    }

    public void BeginGroup (string prefix)
    {
        bool ok = false;
        var path = TestKey (prefix, out ok);
        if (!ok) {
            return;
        }
        m_prefix.AddRange (path);
        path = m_prefix.ToArray ();
        m_root = LValue (ref path, 0, ref m_data, out ok);
    }

    public void EndGroup ()
    {
        if (m_prefix.Count <= 0) {
            return;
        }
        m_prefix.RemoveAt (m_prefix.Count - 1);
        var path = m_prefix.ToArray ();
        bool ok;
        m_root = LValue (ref path, 0, ref m_data, out ok);
    }



    //public int BeginReadArray (string prefix)
    //{
    //    return 0;
    //}

    //public void BeginWriteArray (string prefix, int size = -1)
    //{

    //}

    //public void EndArray ()
    //{

    //}

    //public void SetArrayIndex (int i)
    //{

    //}

    //public void Clear ()
    //{

    //}

    public bool Contains (string key)
    {
        bool ok = false;
        var path = TestKey (key, out ok);
        if (!ok) {
            return false;
        }
        /*var val = */LValue (ref path, 0, ref m_root, out ok);
        return ok;
    }

    //public void Remove (string key)
    //{

    //}

    //public void SetValue (string key, object value)
    //{

    //}

    public object Value (string key, object defaultValue = default(object))
    {
        bool ok = false;
        var path = TestKey (key, out ok);
        if (!ok) {
            return defaultValue;
        }
        var val = LValue (ref path, 0, ref m_root, out ok);
        if (ok) {
            return val;
        }
        return defaultValue;
    }

    public bool ValueBool (string key, bool defaultValue = default(bool))
    {
        try {
            return Convert.ToBoolean (Value (key, defaultValue), System.Globalization.CultureInfo.InvariantCulture);
        }
        catch {
            return defaultValue;
        }
    }

    public string ValueString (string key, string defaultValue = default(string))
    {
        try {
            return Convert.ToString (Value (key, defaultValue));
        }
        catch {
            return defaultValue;
        }
    }

    public int ValueInt (string key, int defaultValue = default(int))
    {
        try {
            return Convert.ToInt32 (Value (key, defaultValue), System.Globalization.CultureInfo.InvariantCulture);
        }
        catch {
            return defaultValue;
        }
    }

    public long ValueLong (string key, long defaultValue = default(long))
    {
        try {
            return Convert.ToInt64 (Value (key, defaultValue), System.Globalization.CultureInfo.InvariantCulture);
        }
        catch {
            return defaultValue;
        }
    }

    public float ValueFloat (string key, float defaultValue = default(float))
    {
        try {
            return Convert.ToSingle (Value (key, defaultValue), System.Globalization.CultureInfo.InvariantCulture);
        }
        catch {
            return defaultValue;
        }
    }

    public double ValueDouble (string key, double defaultValue = default(double))
    {
        var o = Value (key, defaultValue);
        try {
            return Convert.ToDouble (o, System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (FormatException) {
            if (o is string) {
                var v = (o as string).Replace (",", ".");
                try {
                    return Convert.ToDouble (v, System.Globalization.CultureInfo.InvariantCulture);
                }
                catch {
                    return defaultValue;
                }
            }
            return defaultValue;
        }
        catch {
            return defaultValue;
        }
    }

    public List<object> ValueObjectList (string key)
    {
        var r = Value (key, null) as List<object>;
        if (r == null) {
            return new List<object> ();
        }
        return r;
    }

    public List<object> ValueObjectList (string key, List<object> defaultValue)
    {
        var r = Value (key, defaultValue) as List<object>;
        if (r == null) {
            return defaultValue;
        }
        return r;
    }

    public Dictionary<string, object> ValueObjectDict (string key)
    {
        var r = Value (key, null) as Dictionary<string, object>;
        if (r == null) {
            return new Dictionary<string, object> ();
        }
        return r;
    }

    public Dictionary<string, object> ValueObjectDict (string key, Dictionary<string, object> defaultValue)
    {
        var r = Value (key, defaultValue) as Dictionary<string, object>;
        if (r == null) {
            return defaultValue;
        }
        return r;
    }


    public ProfileInfo.Price ValuePrice(string key)
    {
        // get dictionary
        var priceDict = Value(key, null) as Dictionary<string, object>;
        if(priceDict == null) return null;
        // get prefs from dictionary
        var pricePrefs = new JsonPrefs(priceDict);

        // parse price json
        string currencyString = pricePrefs.ValueString("currency");
        if (string.IsNullOrEmpty(currencyString)) return null;
        var currency =
            (ProfileInfo.PriceCurrency)
                Enum.Parse(typeof (ProfileInfo.PriceCurrency), pricePrefs.ValueString("currency"), true);
        var amount = pricePrefs.ValueInt("value");

        return new ProfileInfo.Price(amount, currency);
    }




























    /** Все дерево данных */
    private object m_data = null;
    /** Корень текущего префикса */
    private object m_root = null;
    /** Текущий префикс */
    private List<string> m_prefix = new List<string> ();

    private string[] TestKey (string key, out bool ok)
    {
        ok = false;
        string[] ret = {};
        if (string.IsNullOrEmpty (key)) {
            Debug.LogError ("JsonPrefs: Can't read empty or null key!");
            return ret;
        }
        //if (m_data == null) {
        //    Debug.LogError ("JsonPrefs: Data for reading values are null!");
        //    return ret;
        //}
        ok = true;
        return key.Split (new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
    }

    private object LValue (ref string[] path, int index, ref object o, out bool ok)
    {
        ok = false;
        if (index == (path.Length)) {
            ok = true;
            return o;
        }
        if (o == null) {
            return null;
        }
        //Debug.Log ("path: /" + string.Join ("/", path, 0, index + 1) + ", index: " + index + ", ok: " + ok);
        if (IsDict (ref o)) {
            //Debug.Log ("object is dictionary");
            var dict = o as Dictionary<string, object>;
            if (!dict.ContainsKey (path[index])) {
                //Debug.LogWarning ("Key " + path[index] + " not found, available keys: " + string.Join (",", dict.Keys.ToArray<string> ()));
                return null;
            }
            var val = dict[path[index]];
            return LValue (ref path, index + 1, ref val, out ok);
        }
        else if (IsList (ref o)) {
            //Debug.Log ("object is list");
            var list = o as List<object>;
            try
            {
                var ind = Convert.ToInt32(path[index]);
                if (ind < 0)
                {
                    //Debug.LogWarning ("Key " + path[index] + " can't be less then zero");
                    return null;
                }
                if (ind >= list.Count)
                {
                    //Debug.LogWarning ("Key " + path[index] + " can't be equal or more then " + list.Count + " count values");
                    return null;
                }
                var val = list[ind];
                return LValue(ref path, index + 1, ref val, out ok);
            }
            catch
            {
                return null;
            }
        }
        else {
            //Debug.LogWarning ("object have unsupported, type: " + o.GetType ());
        }
        return null;
    }

    private bool IsDict (ref object o)
    {
        return o is Dictionary<string, object>;
    }

    private bool IsList (ref object o)
    {
        return o is List<object>;
    }

    private bool IsContainer (ref object o)
    {
        return IsDict (ref o) || IsList (ref o);
    }

}
