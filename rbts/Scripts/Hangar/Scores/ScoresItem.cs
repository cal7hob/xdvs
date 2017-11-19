using UnityEngine;

public abstract class ScoresItem : MonoBehaviour
{
    [SerializeField] protected int place;
    [SerializeField] protected float scrollPosition;

    [SerializeField] protected Color colorNamePlace = Color.white;
    [SerializeField] protected Color colorName = Color.white;

    [SerializeField] protected ScoresItemCollision collisionHandler;
    public ScoresItemCollision CollisionHandler { get { return collisionHandler; } }

    [SerializeField] protected GameObject deltaParent;
    public GameObject DeltaParent { get { return deltaParent; } }

    [SerializeField] protected tk2dTextMesh lblDelta;
    public tk2dTextMesh LblDelta { get { return lblDelta; } }

    public tk2dUIItem UiItem { get; protected set; }

    protected tk2dUILayout layout;
    private new string name;

    public float ScrollPosition
    {
        get { return Mathf.Clamp01(scrollPosition); }
        set { scrollPosition = value; }
    }

    public virtual int Place
    {
        get { return place; }
        set { place = value; }
    }

    public virtual bool IsHighlightedItem { get; set; }

    protected virtual void Awake()
    {
        UiItem = GetComponent<tk2dUIItem>();
        layout = GetComponent<tk2dUILayout>();
    }

    public static T Create<T>(ScoresPage parent, T scoresItemPrefab) where T : ScoresItem
    {
        var scoresItem = Instantiate(scoresItemPrefab);

        scoresItem.transform.parent = parent.transform;
        scoresItem.transform.localPosition = Vector3.zero;
        scoresItem.transform.localScale = Vector3.one;

        return scoresItem;
    }

    public virtual void UpdateNameLabel(string name)
    {
#if UNITY_EDITOR
        this.name = name;
        UpdateGameObjectName();
#endif
    }

    private void UpdateGameObjectName()
    {
#if UNITY_EDITOR
        if (gameObject == null)
            return;

        gameObject.name = place + ". " + name;
#endif
    }
}
