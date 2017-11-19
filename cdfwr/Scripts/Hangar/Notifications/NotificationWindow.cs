using UnityEngine;
using XDevs.Notifications.Models;

public class NotificationWindow : MonoBehaviour
{
    [SerializeField] private tk2dTextMesh lblHeader;
    [SerializeField] private tk2dTextMesh lblText;
    [SerializeField] private GameObject sprBackground;
    [SerializeField] private tk2dUIScrollableArea scrollableArea;
    [SerializeField] private int spaceBetweenButtons = 25;

    //private tk2dSpriteCollection sprBackgroundCollection;
    //[SerializeField] private tk2dSpriteFromTexture spriteFromTexture;

    [SerializeField] private NotificationWindowButton[] windowButtons;

    [SerializeField] private UniAlignerBase buttonsUniAligner;

    [SerializeField] private Notification notification;
    //private GameObject sprBackground;
    public static NotificationWindow Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        //MessagingManager.Instance.OnNotificationsChanged += NotificationsChangedHandler;
    }

    private void OnDestroy()
    {
        if (HangarCameraController.Instance != null && gameObject != null)
            HangarCameraController.Instance.nonCamRotationWindows.Remove(gameObject);
        //NotificationsManager.Instance.OnNotificationsChanged -= NotificationsChangedHandler;
        RemoveSpriteCollection();
        Instance = null;
    }

    private void OnDisable()
    {
         RemoveSpriteCollection();
    }

    private void RemoveSpriteCollection()
    {
        if (sprBackground != null)
        {
            var sprBackgroundComponent = sprBackground.GetComponent<tk2dSprite>();

            if (sprBackgroundComponent != null && sprBackgroundComponent.Collection != null)
                Destroy(sprBackgroundComponent.Collection.gameObject);
        }
    }

    public void FillData(Notification notification, System.Action clickedAction)
    {
        this.notification = notification;

        lblHeader.text = notification.Header;
        lblText.text = notification.Text;
        lblText.Commit();

        #region Сопровождающее изображение

        RemoveSpriteCollection();

        Destroy(sprBackground);

        buttonsUniAligner.Clear();

        if (notification.Texture != null)
        {
            //sprBackground = tk2dSprite.CreateFromTexture(message.Texture, tk2dSpriteCollectionSize.ForTk2dCamera(tk2dCamera.Instance),
            //    new Rect(
            //        scrollableArea.contentContainer.transform.localPosition.x,
            //        scrollableArea.contentContainer.transform.localPosition.y,
            //        message.Texture.width, message.Texture.height),
            //    Vector2.down);

            sprBackground = tk2dSprite.CreateFromTexture(notification.Texture, tk2dSpriteCollectionSize.ForTk2dCamera(tk2dCamera.Instance),
                new Rect(
                    0,
                    0,
                    notification.Texture.width, notification.Texture.height),
                Vector2.down);

            sprBackground.name = "sprBackground";
            sprBackground.GetComponent<tk2dSprite>().SortingOrder = 101;
            sprBackground.layer = LayerMask.NameToLayer("2D");

            sprBackground.transform.parent = scrollableArea.contentContainer.transform;

            sprBackground.transform.localPosition = new Vector2(scrollableArea.BackgroundLayoutItem.bMax.x / 2
                - notification.Texture.width / 2, 0);

            lblText.transform.localPosition = new Vector2(lblText.transform.localPosition.x, -notification.Texture.height);
        }

        #endregion

        scrollableArea.Value = 0;
        scrollableArea.ContentLength = scrollableArea.MeasureContentLength();

        foreach (var windowButton in windowButtons)
        {
            windowButton.gameObject.SetActive(false);
        }

        for(var i = 0; i < notification.Buttons.Count; i ++)
        {
            foreach (var windowButton in windowButtons)
            {
                if (windowButton.Button != notification.Buttons[i].Button)
                    continue;

                windowButton.Setup(notification.Buttons[i], notification.Id, clickedAction);

                windowButton.gameObject.SetActive(true);

                if (windowButton.GetComponent<tk2dUILayout>() != null)
                {
                    buttonsUniAligner.AddItem(windowButton.GetComponent<tk2dUILayout>(), 0,
                        i == notification.Buttons.Count - 1 ? 0 : spaceBetweenButtons);
                }
                else
                {
                    buttonsUniAligner.AddItem(windowButton.GetComponent<Renderer>(), 0,
                        i == notification.Buttons.Count - 1 ? 0 : spaceBetweenButtons);
                }
            }
        }

        buttonsUniAligner.Align();
    }

    public static NotificationWindow Instantiate(NotificationWindow prefab, Transform parent)
    {
        var messageWindow = Instantiate(prefab);
        messageWindow.gameObject.name = "NotificationWindow";

        //var pos = messageWindow.transform.localPosition; // saving before parent's change
        //messageWindow.transform.parent = parent;
        //messageWindow.transform.localPosition = pos; // restoring after parent's change

        messageWindow.transform.parent = parent;
        messageWindow.transform.localPosition = Vector3.zero;

        HangarCameraController.Instance.nonCamRotationWindows.Add(messageWindow.gameObject);

        return messageWindow;
    }
}
