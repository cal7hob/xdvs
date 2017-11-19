using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VideoRecordMenuBehaviour : MenuBehaviour
{
    [SerializeField]
    private tk2dUILayout tk2dUILayout;

    public new void ShowContextMenu(tk2dUIItem clickedUiItem, tk2dUILayout clickedItemsLayout)
    {
        base.ShowContextMenu(clickedUiItem, clickedItemsLayout);
    }

    public void HideContextMenu()
    {
        contextMenu.HideContextMenu();
    }

    protected override void SetContextMenuPosition(tk2dUIItem clickedUiItem, tk2dUILayout clickedItemsLayout)
    {
        if (GameData.IsHangarScene)
        {
            contextMenu.transform.position = clickedUiItem.transform.position;

            var menuPos = clickedUiItem.transform.position;

            var topPanelsBottomBorderYPos = tk2dUILayout.GetMaxBounds().y;
            var bottomPanelsTopBorderYPos = tk2dUILayout.GetMinBounds().y;

            if (sprBgRenderer.bounds.max.y > topPanelsBottomBorderYPos)
                menuPos.y = topPanelsBottomBorderYPos;

            if (sprBgRenderer.bounds.min.y < bottomPanelsTopBorderYPos)
                menuPos.y = bottomPanelsTopBorderYPos + sprBgRenderer.bounds.size.y;

            contextMenu.transform.position = menuPos;
        }
        base.SetContextMenuPosition(clickedUiItem, clickedItemsLayout);
    }

}
