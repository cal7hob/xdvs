using System.Collections.Generic;
using UnityEngine;

public static class TransformExtensions
{
    public static void DestroyChildren(this Transform source)
    {
        List<Transform> children = new List<Transform>();

        foreach (Transform child in source)
            children.Add(child);

        foreach (Transform child in children)
            if (Application.isEditor && !Application.isPlaying)
                Object.DestroyImmediate(child.gameObject);
            else
                Object.Destroy(child.gameObject);
    }

    public static void SetX(this Transform source, float value)
    {
        source.position = new Vector3(value, source.position.y, source.position.z);
    }

    public static void SetY(this Transform source, float value)
    {
        source.position = new Vector3(source.position.x, value, source.position.z);
    }

    public static void SetZ(this Transform source, float value)
    {
        source.position = new Vector3(source.position.x, source.position.y, value);
    }

    public static void SetLocalX(this Transform source, float value)
    {
        source.localPosition = new Vector3(value, source.position.y, source.position.z);
    }

    public static void SetLocalY(this Transform source, float value)
    {
        source.localPosition = new Vector3(source.position.x, value, source.position.z);
    }

    public static void SetLocalZ(this Transform source, float value)
    {
        source.localPosition = new Vector3(source.position.x, source.position.y, value);
    }

    public static void AddToSortingOrderRecursively(this Transform transform, int orderToAdd, bool changeSprites, bool changeLabels)
    {
        if (transform == null)
            return;

        var tk2dBaseSprite = transform.GetComponent<tk2dBaseSprite>();

        if (tk2dBaseSprite != null)
            tk2dBaseSprite.SortingOrder += orderToAdd;

        var tk2dTextMesh = transform.GetComponent<tk2dTextMesh>();

        if (tk2dTextMesh != null)
            tk2dTextMesh.SortingOrder += orderToAdd;

        foreach (Transform childTransform in transform)
            AddToSortingOrderRecursively(childTransform, orderToAdd, changeSprites, changeLabels);
    }

    public static List<Transform> GetAllChildrenRecursively(this Transform transform)
    {
        List<Transform> result = new List<Transform>();

        foreach (Transform child in transform)
        {
            result.Add(child);
            result.AddRange(child.GetAllChildrenRecursively());
        }

        return result;
    }
}
