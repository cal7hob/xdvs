using UnityEngine;

public abstract class MenuBehaviour : MonoBehaviour
{
    public Tanks.ContextMenu contextMenu;

    protected Renderer sprBgRenderer;

    private Rect contextMenuRect;
    private Bounds contextMenuBounds;

    protected virtual void Awake()
    {
        sprBgRenderer = contextMenu.sprBg.GetComponent<Renderer>();
    }

    private void Update()
    {
        HideOnTouchOutsideBounds();
    }

    private void HideOnTouchOutsideBounds()
    {
        if (!contextMenu.wrapper.activeSelf) return;

#if UNITY_WEBPLAYER || UNITY_STANDALONE || UNITY_EDITOR || UNITY_WEBGL || UNITY_WSA
        if (!Input.GetMouseButtonDown(0)) return;      
        if (contextMenuRect.Contains(Input.mousePosition)) return;
#else
        if (Input.touchCount <= 0) return;
        if (contextMenuRect.Contains(Input.GetTouch(0).position)) return;
#endif
        contextMenu.HideContextMenu();
    }

    protected virtual void SetContextMenuPosition(tk2dUIItem clickedUiItem, tk2dUILayout clickedItemsLayout)
    {
        GetContextMenuRect();
    }

    protected void ShowContextMenu(tk2dUIItem clickedUiItem, tk2dUILayout clickedItemsLayout)
    {
        if (contextMenu.Count == 0)
            return;

        // We won't have bounds if it's not active
        contextMenu.wrapper.SetActive(true); 
        SetContextMenuPosition(clickedUiItem, clickedItemsLayout);
    }

    protected void GetContextMenuRect()
    {
        contextMenuBounds = sprBgRenderer.bounds;
        contextMenuRect = new Rect()
        {
            xMin = HangarController.Instance.GuiCamera.WorldToScreenPoint(contextMenuBounds.min).x,
            xMax = HangarController.Instance.GuiCamera.WorldToScreenPoint(contextMenuBounds.max).x,
            yMin = HangarController.Instance.GuiCamera.WorldToScreenPoint(contextMenuBounds.min).y,
            yMax = HangarController.Instance.GuiCamera.WorldToScreenPoint(contextMenuBounds.max).y,
        };
    }
}
