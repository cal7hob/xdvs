using System.Collections.Generic;
using UnityEngine;

public static class AdditionsFTRI
{
    public static List<T> ToList<T>(this T[] source)
    {
        if (source == null)
            return new List<T>();
        List<T> list = new List<T>();
        for (int i = 0; i < source.Length; i++)
            list.Add(source[i]);
        return list;
    }
}

[System.Serializable]
public class VectorI2
{
    public int x = 0;
    public int y = 0;

    public VectorI2()
    {
    }

    public VectorI2(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public static VectorI2 zero
    {
        get
        {
            return new VectorI2();
        }
    }

    public override string ToString()
    {
        return string.Format("<{0} {1}>", x, y);
    }
}
