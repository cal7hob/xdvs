using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public static class GOExtensions
{
    public static void SetLayerRecursively(this GameObject go, int layer)
    {
        Transform[] children = go.GetComponentsInChildren<Transform>(true);
        foreach (var child in children)
        {
            if (child.gameObject.layer != layer)
            {
                child.gameObject.layer = layer;
            }
        }
    }
}
