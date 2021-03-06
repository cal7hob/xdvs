public class ScoresMenuBehaviour : MenuBehaviour
{
    private tk2dUILayout scoresScrollAreaUILayout;

    protected override void Awake()
    {
        base.Awake();

        scoresScrollAreaUILayout = ScoresController.Instance.scrollableArea.GetComponent<tk2dUILayout>();
    }

    protected override void SetContextMenuPosition(tk2dUIItem clickedUiItem, tk2dUILayout clickedItemsLayout)
    {
        contextMenu.transform.position = clickedUiItem.transform.position;

        var menuPos = clickedUiItem.transform.position;

        var topPanelsBottomBorderYPos = scoresScrollAreaUILayout.GetMaxBounds().y;
        var bottomPanelsTopBorderYPos = ScoresController.Instance.mainLayout.GetMinBounds().y;

        // Prevent ScoresMenu from going under the TOP panel
        if (sprBgRenderer.bounds.max.y > topPanelsBottomBorderYPos)
            menuPos.y = topPanelsBottomBorderYPos;

        // Prevent ScoresMenu from going under the BOTTOM panel
        if (sprBgRenderer.bounds.min.y < bottomPanelsTopBorderYPos)
            menuPos.y = bottomPanelsTopBorderYPos + sprBgRenderer.bounds.size.y;

        contextMenu.transform.position = menuPos;
        base.SetContextMenuPosition(clickedUiItem, clickedItemsLayout);
    }
}
