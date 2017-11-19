using UnityEngine;

public class FriendsScoresPage : ScoresPagePlayers
{
    [SerializeField] private  GameObject invitePrefab;

    [SerializeField] private GameObject inviteItem;
    [SerializeField] private float inviteItemHeight;

    public bool CheckIfAlreadyFriends(int playerId)
    {
        return pageItems.ContainsKey(playerId);
    }

    public override ScoresItem Reposition()
    {
        highlightedItem = base.Reposition();

        #region Adding Facebook invite button to the friends list
        var inviteItemPosition = pageItems.Count;
#if !UNITY_WSA
        if (Facebook.Unity.FB.IsLoggedIn || SocialSettings.GetSocialService() != null)
        {
#else
		if (SocialSettings.GetSocialService() != null) {
#endif
            if (inviteItem == null)
            {
                inviteItem = Instantiate(invitePrefab);
                inviteItemHeight = (inviteItem.GetComponent<tk2dUILayout>().GetMaxBounds()
                    - inviteItem.GetComponent<tk2dUILayout>().GetMinBounds()).y;
                inviteItem.transform.parent = transform;
                inviteItem.transform.localScale = Vector3.one;
            }

            inviteItem.transform.localPosition =
                new Vector3(0, -itemHeight * inviteItemPosition - (ScoresController.Instance.spaceBetweenItems * inviteItemPosition), 0);

            contentLength += inviteItemHeight + ScoresController.Instance.spaceBetweenItems;
        }
        #endregion

        //if (gameObject.GetActive()) //Не вызывался ScoresController.PanelChanged
        //    scrollArea.ContentLength = contentLength;

        return highlightedItem;
    }

    public override void Clear()
    {
        base.Clear();

        if (inviteItem != null)
        {
            Destroy(inviteItem);
            inviteItem = null;
        }

        pageItems.Clear();
    }

    private void Awake()
    {
        scoresItemPrefab = ScoresController.Instance.playerItemPrefab;
        scoresMenuBehaviour = ScoresController.Instance.PlayerMenuBehaviour;
        Init();
    }
}
