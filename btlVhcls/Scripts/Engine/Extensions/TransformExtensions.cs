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

    public static void AddToSortingOrderRecursively(this Transform transform, int _orderToAdd, bool _changeSprites, bool _changeLabels, bool _changeBoxColliders)
    {
        if (transform == null)
            return;

        if(_changeSprites)
        {
            var tk2dBaseSprite = transform.GetComponent<tk2dBaseSprite>();

            if (tk2dBaseSprite != null)
               tk2dBaseSprite.SortingOrder += _orderToAdd;
        }
        
        if(_changeLabels)
        {
            var tk2dTextMesh = transform.GetComponent<tk2dTextMesh>();

            if (tk2dTextMesh != null)
                tk2dTextMesh.SortingOrder += _orderToAdd;
        }

        if (_changeBoxColliders)
        {
            var boxCollider = transform.GetComponent<BoxCollider>();

            if (boxCollider != null)
                boxCollider.center = new Vector3(boxCollider.center.x, boxCollider.center.y, boxCollider.center.z + _orderToAdd);
        }

        foreach (Transform childTransform in transform)
            AddToSortingOrderRecursively(childTransform, _orderToAdd, _changeSprites, _changeLabels, _changeBoxColliders);
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

    public static bool RemoveComponent<T> (this Transform transform) where T : Component {
        T c = transform.GetComponent<T> ();
        if (c == null) {
            return false;
        }

        Object.DestroyImmediate (c);
        return true;
    }
}
