using System.Collections.Generic;
using Tanks.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using JSONObject = System.Collections.Generic.Dictionary<string, object>;

public abstract class ScoresPage : MonoBehaviour
{
    public bool scrollToHighlightedItem;

    [SerializeField] private tk2dUIScrollableArea scrollArea;

    [SerializeField] protected ScoresItem scoresItemPrefab;
    [SerializeField] protected float contentLength;
    [SerializeField] protected float itemHeight;
    [SerializeField] protected ScoresItem highlightedItem;

    protected Dictionary<int, ScoresItem> pageItems;

    protected ScoresMenuBehaviour scoresMenuBehaviour;

    public ScoresItem ScoresItemPrefab
    {
        get { return scoresItemPrefab; }
        protected set { scoresItemPrefab = value; }
    }

    public string PlaceKey { get; set; }

    public Dictionary<int, ScoresItem> PageItems { get { return pageItems; } }
    public float ItemHeight { get { return itemHeight; } }
    public ScoresItem HighlightedItem { get { return highlightedItem; } }
    public float ContentLength { get { return contentLength; } }

    public static bool IsScrolling { get; private set; }

    private void Update()
    {
        if (!scrollToHighlightedItem)
            return;

        scrollToHighlightedItem = false;

        if (highlightedItem != null)
            scrollArea.Value = highlightedItem.ScrollPosition;
    }

    public static T Create<T>(string key,
        tk2dUIScrollableArea parent,
        ScoresItem scoresItemPrefab,
         ScoresMenuBehaviour scoresMenuBehaviour) where T : ScoresPage
    {
        var o = new GameObject("Content_" + key);
        o.transform.parent = parent.transform;
        o.transform.localPosition = Vector3.zero;

        ScoresPage p = o.AddComponent<T>();
        p.scrollArea = parent;
        p.scoresItemPrefab = scoresItemPrefab;
        p.scoresMenuBehaviour = scoresMenuBehaviour;

        p.Init();
        return p as T;
    }

    public abstract void AddItem(JSONObject data);

    public virtual ScoresItem Reposition()
    {
        //var ordered = m_list.OrderByDescending (user => user.Value.userScore);
        contentLength = itemHeight * (pageItems.Count + 1) + (ScoresController.Instance.spaceBetweenItems * (pageItems.Count-1));
        var index = 0;

        foreach (var scoresItem in pageItems.Values)
        {
            scoresItem.transform.localPosition =
                new Vector3(0, -itemHeight * index - (ScoresController.Instance.spaceBetweenItems * index), 0);

            // Для FriendsScoresPage, т. к. для друзей не присылается Place, а становится известен здесь
            if (scoresItem.Place == 0)
                scoresItem.Place = index + 1;

            var halfVisibleArea = scrollArea.VisibleAreaLength / 2f;
            var itemCenter = (-scoresItem.transform.localPosition.y) + (itemHeight / 2f);

            scoresItem.ScrollPosition =
                (itemCenter - halfVisibleArea) / (contentLength - scrollArea.VisibleAreaLength);

            if (scoresItem.IsHighlightedItem)
            {
                highlightedItem = scoresItem;

                scrollToHighlightedItem = true;
            }

            index++;
        }

        //if (gameObject.GetActive()) //Не вызывался ScoresController.PanelChanged
        //    scrollArea.ContentLength = contentLength;

        return highlightedItem;
    }

    public virtual void UpdatePlayer(Player player) { }

    public virtual void Clear()
    {
        pageItems.Clear();

        for (var i = transform.childCount - 1; i >= 0; --i)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
    }

    protected virtual void Init()
    {
        pageItems = new Dictionary<int, ScoresItem>();

        // tk2dUILayout is not cached as scoresItemPrefab is not instantiated at this time.
        itemHeight = (scoresItemPrefab.GetComponent<tk2dUILayout>().GetMaxBounds()
            - scoresItemPrefab.GetComponent<tk2dUILayout>().GetMinBounds()).y;
        Clear();
    }

    private void Start()
    {
        scrollArea.backgroundUIItem.OnDown += OnScrollDown;
        scrollArea.backgroundUIItem.OnRelease += OnScrollRelease;
    }

    protected virtual void OnSceneUnloaded(Scene scene)
    {
        scrollArea.backgroundUIItem.OnDown -= OnScrollDown;
        scrollArea.backgroundUIItem.OnRelease -= OnScrollRelease;
    }

    private void OnScrollDown()
    {
        IsScrolling = true;
    }

    private void OnScrollRelease()
    {
        IsScrolling = false;
    }
}
