using UnityEngine;

class ResolutionDependentLayoutResizer : MonoBehaviour
{
    [SerializeField] private tk2dUILayout[] layoutsToResize;

    private int oldScreenHeight;

    void Awake()
    {
        Dispatcher.Subscribe(EventId.ResolutionChanged, OnResolutionChanged);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.ResolutionChanged, OnResolutionChanged);
    }

    void Start()
    {
        oldScreenHeight = Mathf.RoundToInt(tk2dCamera.Instance.NativeScreenExtents.height);
        OnResolutionChanged();
    }

    private void OnResolutionChanged(EventId id = 0, EventInfo info = null)
    {
        var deltaH = oldScreenHeight - Mathf.RoundToInt(tk2dCamera.Instance.ScreenExtents.height);
        oldScreenHeight = Mathf.RoundToInt(tk2dCamera.Instance.ScreenExtents.height);

        for (int i = 0; i < layoutsToResize.Length; i++)
        {
            var layout = layoutsToResize[i];

            if (layout != null)
            {
                layout.Reshape(new Vector3(0, deltaH, 0), Vector3.zero, true);
            }
            else
            {
                Debug.LogErrorFormat(this, "{0}, OnResolutionChanged, layout == null", this);
            }
        }
    }
}
