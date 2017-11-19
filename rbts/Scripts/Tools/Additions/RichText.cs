using System.Collections.Generic;

public static class RichText
{
    public enum Param
    {
        Color,
        Size,
        Style,
    }

    public static string RichString(this string need, string format)
    {
        Dictionary<Param, string> dict = new Dictionary<Param,string>();
        string[] lst = format.Split(new char[] { ';' });
        for (int i = 0; i < lst.Length; i++)
        {
            string[] tmp = lst[i].Split(new char[] { ':' });
            if (tmp[0].ToLower() == "color")
            {
                dict.Add(Param.Color, tmp[1]);
            }
            if (tmp[0].ToLower() == "size")
            {
                dict.Add(Param.Size, tmp[1]);
            }
            if (tmp[0].ToLower() == "style")
            {
                dict.Add(Param.Style, tmp[1]);
            }
        }

        return need.RichString(dict);
    }

    public static string RichString(this string need, Dictionary<Param, string> parameters)
    {
        string res = need;
        string form = "";
        if (parameters.TryGetValue(Param.Color, out form))
        {
            res = string.Format("<color={0}>{1}</color>", form, res);
        }
        if (parameters.TryGetValue(Param.Size, out form))
        {
            res = string.Format("<size={0}>{1}</size>", form, res);
        }
        if (parameters.TryGetValue(Param.Style, out form))
        {
            string[] tmp = form.Split(new char[] { ',' });
            for (int i = 0; i < tmp.Length; i++)
            {
                res = string.Format("<{0}>{1}</{0}>", tmp[i], res);
            }
        }
        return res;
    }
    
}
