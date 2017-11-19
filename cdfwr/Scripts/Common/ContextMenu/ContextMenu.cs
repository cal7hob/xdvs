using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tanks
{
    public abstract class ContextMenu : MonoBehaviour
    {
        public tk2dSlicedSprite sprBg;
        public float minSprBgHeight = 0; 
        public GameObject sprHover;
        public ContextMenuItem menuItemPrefab;
        public List<ContextMenuItem> menuItems = new List<ContextMenuItem>();
        public GameObject wrapper;
        public int itemHeight = 75;
        public Color textColor = HexToColor.hexToColor("9A9A9AFF");
        public Color hoverTextColor = HexToColor.hexToColor("FFFFFFFF");
        public Color hoverColor = HexToColor.hexToColor("9E9A50FF");
        private Vector2 firstMenuItemPos;

        /// <summary>
        /// Active items count
        /// </summary>
        public int Count { get
            {
                if (menuItems == null)
                    return 0;
                int count = 0;
                for (int i = 0; i < menuItems.Count; i++)
                    if (menuItems[i].gameObject.activeSelf)
                        count++;
                return count;
            }
        }

        private void Awake()
        {
            SetHovers();
            HideContextMenu();
            sprHover.GetComponent<tk2dBaseSprite>().color = hoverColor;
        }

        private void SetHovers()
        {
            foreach (var contextMenuItem in menuItems)
            {
                tk2dUIManager.Instance.UseMultiTouch = false;
                contextMenuItem.uiItem.OnHoverOverUIItem += OnHoverOver;
                contextMenuItem.uiItem.OnHoverOutUIItem += OnHoverOut;
            }
        }

        private void OnHoverOver(tk2dUIItem item)
        {
            
            item.GetComponent<tk2dTextMesh>().color = hoverTextColor;
            sprHover.transform.SetParent(item.transform.parent, false);
            sprHover.SetActive(true);
        }

        private void OnHoverOut(tk2dUIItem item)
        {
            item.GetComponent<tk2dTextMesh>().color = textColor;
            sprHover.SetActive(false);
        }

        public void ResetMenu()
        {
            firstMenuItemPos = sprBg.transform.localPosition;
            sprBg.dimensions = new Vector2(sprBg.dimensions.x, 0);
        }

        public void AddMenuItem(string menuItemName = "NewMenuItem", string lblName = "lblNewItem")
        {
            ContextMenuItem newMenuItem;

            if (menuItems.Count != 0)
            {
                newMenuItem =
                    Instantiate(menuItemPrefab,
                        menuItems.Last().transform.localPosition - Vector3.up * itemHeight, new Quaternion())
                        as ContextMenuItem;
            }
            else
            {
                ResetMenu();
                newMenuItem = Instantiate(menuItemPrefab, firstMenuItemPos, new Quaternion()) as ContextMenuItem;
            }

            if (newMenuItem == null) return;

            newMenuItem.name = menuItemName;
            newMenuItem.textMesh.name = lblName;
            try
            {
                newMenuItem.textMesh.text = Localizer.GetText(menuItemName);
            }
            catch (Exception)
            {
                Debug.LogError(String.Format("localization key {0} not found", lblName));
            }
            newMenuItem.transform.SetParent(wrapper.transform, false);
            AddToBgSize(itemHeight / Mathf.Abs(sprBg.scale.y));
            menuItems.Add(newMenuItem);
        }

        public void HideContextMenu()
        {
            wrapper.SetActive(false);
        }

        public void HideMenuItem(string itemName)
        {
            var index = 0;
            foreach (var item in menuItems)
            {
                if (item.name == itemName)
                {
                    if (!item.gameObject.activeSelf)
                        return;

                    item.gameObject.SetActive(false);
                    break;
                }
                index++;
            }

            for (int i = index; i < menuItems.Count - 1; i++)
            {
                menuItems[i + 1].transform.position += Vector3.up * itemHeight;
            }

            AddToBgSize(-1 * itemHeight / Mathf.Abs(sprBg.scale.y));
            if (Count == 0)
                HideContextMenu();
        }

        private void AddToBgSize(float height)
        {
            float newHeight = sprBg.dimensions.y + height;
            sprBg.dimensions = new Vector2(sprBg.dimensions.x, Mathf.Clamp(newHeight, minSprBgHeight, newHeight));
        }

        public void ShowMenuItem(string itemName)
        {
            int index = 0;
            foreach (ContextMenuItem item in menuItems)
            {
                if (item.name == itemName)
                {
                    if (item.gameObject.activeSelf)
                        return;

                    item.gameObject.SetActive(true);
                    break;
                }
                index++;
            }

            for (int i = index + 1; i < menuItems.Count; i++)
            {
                menuItems[i].transform.position -= Vector3.up * itemHeight;
            }

            AddToBgSize(itemHeight / Mathf.Abs(sprBg.scale.y));
        }
    }
}
