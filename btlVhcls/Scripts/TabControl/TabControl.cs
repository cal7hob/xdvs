using System.Collections.Generic;
using UnityEngine;
using Tanks.Models;

public class TabControl : MonoBehaviour
{
    public tk2dUILayoutContainerSizer tabsContainer;
    public tk2dUIToggleButtonGroup toggleButtonGroup;
    public List<tk2dUIToggleButton> toggleBtns = new List<tk2dUIToggleButton>();
    public Tab tabsPrefab;
    public bool alignTabsByCenter = true;

    public Dictionary<ChatRoom, Tab> tabs = new Dictionary<ChatRoom, Tab>();

    public delegate void TabChangedDelegate(string key);
    public TabChangedDelegate OnTabChanged;

    private tk2dUIScrollableArea scrollableArea;

    public void AddTab(int index, Tanks.Models.Room room)
    {
        var tab = Tab.Create(room, tabsContainer, tabsPrefab);

        tabs.Add(room.type, tab);
        toggleBtns.Add(tab.toggleButton);
        toggleButtonGroup.AddNewToggleButtons(toggleBtns.ToArray());

        AddTabToContainer(tab, index);
    }

    public void RemoveTab(ChatRoom roomType)
    {
        if (!tabs.ContainsKey(roomType))
        {
            DT.LogWarning("Cant delete {0} tab, it doesn't exist!", roomType);
            return;
        }
            
        toggleBtns.Remove(tabs[roomType].toggleButton);
        toggleButtonGroup.AddNewToggleButtons(toggleBtns.ToArray());
        
        tabsContainer.RemoveLayout(tabs[roomType].layout);

        Destroy(tabs[roomType].gameObject);

        SetupTabsContainerPosition();

        tabs.Remove(roomType);
    }

    private void AddTabToContainer(Tab tab, int index)
    {
        tabsContainer.AddLayoutAtIndex(tab.layout, tk2dUILayoutItem.FixedSizeLayoutItem(), index);

        SetupTabsContainerPosition();
    }

    private void SetupTabsContainerPosition()
    {
        tabsContainer.Refresh();

        // Setting tabsContainer's localPosition
        scrollableArea.ContentLength = tabsContainer.ItemCount > 0 ? scrollableArea.MeasureContentLength() : 0;

        if(alignTabsByCenter)
        {
            //Центровка панели вкладок
            if (scrollableArea.ContentLength <= scrollableArea.VisibleAreaLength)
            {
                tabsContainer.transform.localPosition =
                    new Vector3((scrollableArea.VisibleAreaLength - scrollableArea.ContentLength) / 2, tabsContainer.transform.localPosition.y, tabsContainer.transform.localPosition.z);
            }
        }

        
    }

    private void OnTabSelect(tk2dUIToggleButtonGroup group)
    {
        if (group.SelectedToggleButton != null)
            OnTabChanged(group.SelectedToggleButton.name);
    }

    public void SwitchTo(ChatRoom roomType)
    {
        foreach (var toggleBtn in toggleButtonGroup.ToggleBtns)
        {
            if (toggleBtn == null) continue;

            if (toggleBtn.name == roomType.ToString())
                toggleBtn.IsOn = true;
        }
    }

    private void Awake()
    {
        scrollableArea = GetComponent<tk2dUIScrollableArea>();
        toggleButtonGroup.OnChange += OnTabSelect;
    }
}
