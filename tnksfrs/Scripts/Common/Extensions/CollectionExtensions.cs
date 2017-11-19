using System.Collections.Generic;

public static class CollectionExtensions
{
    public static T GetRandomItem<T>(this T[] source)
    {
        return source[MiscTools.random.Next(0, source.Length)];
    }

    public static T GetRandomItem<T>(this List<T> source)
    {
        return source[MiscTools.random.Next(0, source.Count)];
    }

    public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> source, TKey key)
    {
        TValue result;
        return source.TryGetValue(key, out result) ? result : default(TValue);
    }
}
