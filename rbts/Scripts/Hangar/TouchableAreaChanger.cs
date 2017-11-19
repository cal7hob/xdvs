using UnityEngine;

public class TouchableAreaChanger : MonoBehaviour
{

    private void OnEnable()
    {
        Messenger.Send(EventId.TouchableAreaChanged, null);
    }

    private void OnDisable()
    {
        Messenger.Send(EventId.TouchableAreaChanged, null);
    }
}
