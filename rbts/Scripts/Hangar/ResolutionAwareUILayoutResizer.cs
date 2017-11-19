using UnityEngine;

public class ResolutionAwareUILayoutResizer : MonoBehaviour
{
    [SerializeField]
    private tk2dUILayout[] layoutsToReshape;
    [SerializeField]
    private bool debug;

    private int lastCamHeight;

    private void Awake()
    {
        Messenger.Subscribe(EventId.ResolutionChanged, ResolutionChangedEventHandler);
    }

    private void Start()
    {
        lastCamHeight = Mathf.RoundToInt(GameData.CurSceneTk2dGuiCamera.NativeScreenExtents.height);
        ResolutionChangedEventHandler(EventId.Manual, null);
    }

    private void OnDestroy()
    {
        Messenger.Unsubscribe(EventId.ResolutionChanged, ResolutionChangedEventHandler);
    }

    private void ResolutionChangedEventHandler(EventId id, EventInfo info)
    {
        if (debug)
            Debug.LogErrorFormat(this, "{0}, ResolutionChangedEventHandler", this);

        if (Mathf.RoundToInt(GameData.CurSceneTk2dGuiCamera.ScreenExtents.height) != lastCamHeight)
        {
            var delta = lastCamHeight - Mathf.RoundToInt(GameData.CurSceneTk2dGuiCamera.ScreenExtents.height);

            if (debug)
                Debug.LogErrorFormat(this, "{0}, ResolutionChangedEventHandler, reshaping with delta {1}", this, delta);

            foreach (var layout in layoutsToReshape)
            {
                if (layout != null)
                {
                    // Для того, чтобы при Reshape() изменялся tk2dUIScrollableArea.VisibleAreaLength,
                    // нужно накинуть родительский tk2dUILayout на tk2dUIScrollableArea.BackgroundLayoutItem.
                    layout.Reshape(new Vector3(0, delta, 0), Vector3.zero, true);
                }
                else
                {
                    Debug.LogErrorFormat(this, "{0}, ResolutionChangedEventHandler, layout == null", this);
                }
            }

            lastCamHeight = Mathf.RoundToInt(GameData.CurSceneTk2dGuiCamera.ScreenExtents.height);
        }
    }
}
