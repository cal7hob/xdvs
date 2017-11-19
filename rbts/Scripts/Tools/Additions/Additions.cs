using UnityEngine;

public static class Additions
{
    public static Transform GetObjectByNameInChildren(this GameObject obj, string name)
    {
        if (obj == null)
        {
            return null;
        }

        Transform result;
        foreach (Transform child in obj.transform)
        {
            if (null == child)
            {
                continue;
            }

            if (child.name == name)
            {
                return child;
            }
            if ((result = GetObjectByNameInChildren(child.gameObject, name)) != null) return result;
        }
        return null;
    }
}
