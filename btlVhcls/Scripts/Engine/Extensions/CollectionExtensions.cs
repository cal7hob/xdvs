using System;
using UnityEngine;
using System.Collections.Generic;

public static class CollectionExtensions
{
    public static T GetRandomItem<T>(this T[] source)
    {
        return source[MiscTools.random.Next(0, source.Length)];
    }

    public static T GetRandomItemOrDefault<T>(this T[] source)
    {
        return source.Length == 0 ? default(T) : source[MiscTools.random.Next(0, source.Length)];
    }

    public static T GetRandomItem<T>(this List<T> source)
    {
        return source[MiscTools.random.Next(0, source.Count)];
    }

    public static T GetRandomItemOrDefault<T>(this List<T> source)
    {
        return source.Count == 0 ? default(T) : source[MiscTools.random.Next(0, source.Count)];
    }

    public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> source, TKey key)
    {
        TValue result;
        return source.TryGetValue(key, out result) ? result : default(TValue);
    }

    public static bool Extract<T>(this Dictionary<string, object> dict, string key, ref T output, bool necessary = true)
    {
        object obj;

        if (!dict.TryGetValue(key, out obj))
        {
            if (necessary)
                Debug.LogErrorFormat("Dictionary parsing error. No value with key '{0}'", key);

            return false;
        }

        try
        {
            if (output is Enum)
            {
                string objString = obj as string;
                try
                {
                    output = (T) Enum.Parse(typeof (T), objString ?? Convert.ToString(obj), true);
                }
                catch (Exception)
                {
                    return false;
                }
            }
            else
            {
                try
                {
                    output = (T) obj;
                }
                catch (Exception)
                {
                    output = (T)Convert.ChangeType(obj, typeof(T));
                }
            }
        }
        catch
        {
            if (necessary)
            {
                Debug.LogErrorFormat("Dictionary parsing error. Value with key '{0}' is {1}, {2} expected.",
                    key, obj.GetType().Name, typeof (T).Name);
            }
            return false;
        }

        return true;
    }

    public static T ExtractOrDefault<T>(this Dictionary<string, object> dict, string key, T defaultValue = default(T))
    {
        T output = defaultValue;
        Extract(dict, key, ref output, false);

        return output;
    }

    public static string ToJsonString(this object obj)
    {
        return MiniJSON.Json.Serialize(obj);
    }
}