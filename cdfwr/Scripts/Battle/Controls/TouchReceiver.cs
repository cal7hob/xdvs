using UnityEngine;

public class TouchReceiver : MonoBehaviour
{
    private static bool battleBtnPressed;
    private static bool isGUIOnScreen;
    private static int swipeFingerId = -1;

#if UNITY_EDITOR
    private static Vector3 previousMousePosition;
#endif

    public static Vector2 DeltaTouchPosition { get; private set; }

    void Awake()
    {
        Dispatcher.Subscribe(EventId.BattleBtnPressed, OnBattleBtnPressed);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.BattleBtnPressed, OnBattleBtnPressed);

        battleBtnPressed = false;
    }

    void LateUpdate()
    {
        CheckTouches();

#if UNITY_EDITOR
        CheckMouseMove();
#endif

        battleBtnPressed = false;
    }
#if UNITY_EDITOR
    private static void CheckMouseMove()
    {
        if (isGUIOnScreen)
        {
            return;
        }

        if (Input.GetMouseButton(0))
        {
            DeltaTouchPosition = HelpTools.GetScreenRelPos(Input.mousePosition - previousMousePosition);
        }

        previousMousePosition = Input.mousePosition;
    }
#endif
    private static void CheckTouches()
    {
        if (isGUIOnScreen)
        {
            return;
        }

        for (int i = 0; i < Input.touchCount; i++)
        {
            var touch = Input.touches[i];

            if (touch.phase == TouchPhase.Began && !battleBtnPressed)
            {

                swipeFingerId = touch.fingerId;
            }

            if ((touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary) &&
                touch.fingerId == swipeFingerId)
            {
                DeltaTouchPosition = HelpTools.GetScreenRelPos(touch.deltaPosition);
            }

            if (touch.phase == TouchPhase.Ended && touch.fingerId == swipeFingerId)
            {
                Reset();
            }
        }
    }

    private static void Reset()
    {
        DeltaTouchPosition = Vector2.zero;
        swipeFingerId = -1;
    }

    private static void OnBattleBtnPressed(EventId id, EventInfo info)
    {
        battleBtnPressed = true;
    }
}