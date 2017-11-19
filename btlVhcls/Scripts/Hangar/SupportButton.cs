using Http;
using UnityEngine;

public class SupportButton : MonoBehaviour
{
    [SerializeField] private tk2dTextMesh lblUnreadCounter;
    [SerializeField] private TweenBase unreadCounterTweenBase;
    [SerializeField] private GameObject unreadCounterGameObject;

    private tk2dUIItem cachedFeedbackButton;
    private int unreadMessages;
    private bool isSiteOpened;

    private int UnreadMessages
    {
        get { return unreadMessages; }
        set
        {
            if (unreadMessages == value)
                return;

            unreadMessages = value;
            lblUnreadCounter.text = value.ToString();

            if (value > 0)
            {
                unreadCounterGameObject.SetActive(true);
                InvokeRepeating("AnimateLabel", 0, 3);
            }
            else
            {
                unreadCounterGameObject.SetActive(false);
                CancelInvoke();
            }
        }
    }

    private void Awake()
    {
        Dispatcher.Subscribe(EventId.AfterHangarInit, ApplyUnreadMessages);
        Dispatcher.Subscribe(EventId.ProfileInfoLoadedFromServer, ApplyUnreadMessages);

        cachedFeedbackButton = GetComponent<tk2dUIItem>();

        cachedFeedbackButton.OnClick += Clicked;

        unreadCounterGameObject.SetActive(false);
        lblUnreadCounter.text = "0";
    }

    private void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.AfterHangarInit, ApplyUnreadMessages);
        Dispatcher.Unsubscribe(EventId.ProfileInfoLoadedFromServer, ApplyUnreadMessages);
        cachedFeedbackButton.OnClick -= Clicked;
    }

    private void OnApplicationFocus(bool focused)
    {
        if (focused && isSiteOpened)
        {
            isSiteOpened = false;
        }
    }

    private void ApplyUnreadMessages(EventId id, EventInfo ei)
    {
        UnreadMessages = ProfileInfo.unreadMessages;
    }

    private void AnimateLabel()
    {
        if (unreadCounterTweenBase != null)
        {
            unreadCounterTweenBase.Play();
        }
    }

    private void Clicked()
    {
        isSiteOpened = true;
        Manager.OpenURL(Manager.ROUTE_SUPPORT);
    }
}
