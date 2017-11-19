
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using Vkontakte;

public class FriendToInviteList : MonoBehaviour
{
    public FriendToInvite FriendPrefab;
    public tk2dUIScrollableArea ScrollableArea;

    public float startOffset = 0f;
    public float gap = 8f;

    private List<FriendToInvite> list = new List<FriendToInvite>();

    public void Add(VkUser friend)
    {
        CreateItem().Init(friend);
    }

    public void Clear()
    {
        foreach (var friendToInvite in list)
        {
            Destroy(friendToInvite.gameObject);
        }
        list.Clear();
    }
    public void Reposition()
    {
        float itemHeight = FriendPrefab.ItemHeight;
        float contentLength = startOffset + list.Count * (itemHeight + gap);
        int index = 0;
        foreach (var item in list)
        {
            item.transform.localPosition = new Vector3(0, - startOffset - index * (itemHeight + gap), 0);
            index++;
        }

        ScrollableArea.ContentLength = contentLength;
    }
    FriendToInvite CreateItem()
    {
        var item = Instantiate(FriendPrefab);
        list.Add(item);
        item.transform.parent = gameObject.transform;
        item.transform.localPosition = Vector3.zero;
        item.transform.localScale = new Vector3(1, 1, 1);
        return item;
    }
}
