using UnityEngine;
using System.Collections;

public class ShowHideGUIPage : MonoBehaviour
{

    public Vector3 defaultPagePosition;

    void Awake()
    {
        Dispatcher.Subscribe(EventId.AfterLocalizationLoad, DisActivateWindow);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.AfterLocalizationLoad, DisActivateWindow);
    }

    private void DisActivateWindow(EventId id, EventInfo info)
    {
		MoveToDefaultPosition();
        gameObject.SetActive(false);
    }

    public void MoveToDefaultPosition()
    {
        transform.localPosition = defaultPagePosition;
    }

	public void MoveToDefaultPositionAndShow()
	{
		transform.localPosition = defaultPagePosition;
		gameObject.SetActive(true);
	}

    public void Hide()
    {
        //transform.localPosition = Vector3.right*5000;
        gameObject.SetActive(false);
    }
}
