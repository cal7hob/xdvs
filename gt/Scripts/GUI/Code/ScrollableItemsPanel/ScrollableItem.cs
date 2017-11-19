using UnityEngine;

public abstract class ScrollableItem : MonoBehaviour
{
    public abstract Vector2 Size { get; }

    public abstract void Initialize(params object[] parameters);

    public virtual void DestroySelf() { }

    public virtual void SetPosition(Vector3 position)
    {
        transform.localPosition = position;
    }
}