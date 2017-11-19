using System.Collections.Generic;
using UnityEngine;
using Tanks.Models;

public class TabControl : MonoBehaviour
{
    public Tab tabsPrefab;
    public bool alignTabsByCenter = true;

    public Dictionary<ChatRoom, Tab> tabs = new Dictionary<ChatRoom, Tab>();

    public delegate void TabChangedDelegate(string key);
    public TabChangedDelegate OnTabChanged;

    public void AddTab(int index, Tanks.Models.Room room)
    {
    }

    public void RemoveTab(ChatRoom roomType)
    {
        if (!tabs.ContainsKey(roomType))
        {
            DT.LogWarning("Cant delete {0} tab, it doesn't exist!", roomType);
            return;
        }
            
        
        Destroy(tabs[roomType].gameObject);

        SetupTabsContainerPosition();

        tabs.Remove(roomType);
    }

    private void AddTabToContainer(Tab tab, int index)
    {
        SetupTabsContainerPosition();
    }

    private void SetupTabsContainerPosition()
    {
        // Setting tabsContainer's localPosition
        if(alignTabsByCenter)
        {
            //Центровка панели вкладок
        }

        
    }

    /*private void OnTabSelect(tk2dUIToggleButtonGroup group)
    {
        if (group.SelectedToggleButton != null)
            OnTabChanged(group.SelectedToggleButton.name);
    }*/

    public void SwitchTo(ChatRoom roomType)
    {
       
    }

    private void Awake()
    {
    }
}
