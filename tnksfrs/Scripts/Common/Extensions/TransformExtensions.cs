using UnityEngine;

public static class TransformExtensions
{
    public static void DestroyChildren(this Transform source)
    {
        foreach (Transform child in source)
            Object.Destroy(child.gameObject);

        source.DetachChildren();
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
}
