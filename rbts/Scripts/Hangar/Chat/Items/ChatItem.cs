using Tanks.Models;
using UnityEngine;

public abstract class ChatItem : MonoBehaviour
{
    [SerializeField] protected tk2dTextMesh nameLabel;

    [SerializeField] private tk2dTextMesh lblUserLevel;
    [SerializeField] private Avatar avatar;

    [SerializeField] private int maxNickMeshXBound = 313;

    protected Player player;
    protected tk2dUILayout layout;
    protected ChatPage parentChatPage;

    protected virtual void Awake()
    {
        layout = GetComponent<tk2dUILayout>();
    }

    public static T Create<T>(ChatPage parentChatPage, T chatItemPrefab) where T : ChatItem
    {
        if (chatItemPrefab == null)
        {
            Debug.LogError("Chat item prefab is null!");
            return null;
        }

        var parentContainerSizer = parentChatPage.containerSizer;

        if (parentContainerSizer.transform.childCount >= ChatManager.Instance.chatController.messagesToShow)
        {
            //Debug.LogWarning("containerSizer.transform.childCount >= ChatManager.Instance.chatController.messagesToShow: " + (containerSizer.transform.childCount >= ChatManager.Instance.chatController.messagesToShow));

            var toRemove = parentContainerSizer.transform.GetChild(0).gameObject;
            parentContainerSizer.RemoveLayout(toRemove.GetComponent<tk2dUILayout>());
            parentContainerSizer.Refresh();
            Destroy(toRemove);
        }

        parentChatPage.SaveOldContentLength();

        var chatItem = Instantiate(chatItemPrefab);

        chatItem.parentChatPage = parentChatPage;

        parentContainerSizer.AddLayoutAtIndex(chatItem.layout,
            tk2dUILayoutItem.FixedSizeLayoutItem(), 0);

        // Setting chatItem's localPosition as the parent changed and
        // parent-relative localPosition was modified.
        chatItem.transform.localPosition = new Vector3(
            chatItem.transform.localPosition.x,
            chatItem.transform.localPosition.y,
            parentChatPage.transform.localPosition.z);

        return chatItem;
    }

    protected void Init(Player player)
    {
        this.player = player;
#if UNITY_EDITOR
        gameObject.name = player.NickName;
#endif

        if (!string.IsNullOrEmpty(player.NickName))
        {
            var temp = player.NickName;

            while (nameLabel.GetEstimatedMeshBoundsForString(temp).size.x > maxNickMeshXBound)
            {
                temp = temp.Substring(0, temp.Length - 1);
            }

            nameLabel.text = temp;
        }

        avatar.Init(player);
        avatar.DownloadAvatar(2f);

        // Смотри ProfileInfo.CalcExperience()
        lblUserLevel.text = Mathf.Clamp(ProfileInfo.LevelForExperience(player.Score), 1, 50).ToString();
    }
}