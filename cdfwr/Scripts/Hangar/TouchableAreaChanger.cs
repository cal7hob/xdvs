using UnityEngine;

public class TouchableAreaChanger : MonoBehaviour
{

    private void OnEnable()
    {
        Dispatcher.Send(EventId.TouchableAreaChanged, null);
    }

    private void OnDisable()
    {
        Dispatcher.Send(EventId.TouchableAreaChanged, null);
    }
}
