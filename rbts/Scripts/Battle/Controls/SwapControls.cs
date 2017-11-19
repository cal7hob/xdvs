using UnityEngine;

public interface ISwappableControl
{
    bool Swappable { get; }
    void Swap();
}

public class SwapControls : MonoBehaviour
{
    [SerializeField] private tk2dUILayout[] objectsToSwap;
    [SerializeField] private GameObject anchorMiddleCenter;
    private int swapControls;

    private void Start()
    {
        swapControls = PlayerPrefs.GetInt("SwapControls", Settings.DEFAULT_SWAP_CONTROLS_VALUE);

        if (swapControls != 0)
        {
            Swap();
        }
    }

    private void Swap()
    {
        foreach (var obj in objectsToSwap)
        {
            // Опорная точка (pivot) У tk2dUILayout всегда в левом верхнем углу

            var deltaCenter = anchorMiddleCenter.transform.position.x - obj.transform.position.x;
            var objWidth = (obj.GetMaxBounds() - obj.GetMinBounds()).x;

            obj.transform.position += new Vector3(deltaCenter * 2 - objWidth, 0, 0);

            var swappableObject = obj.GetComponent<ISwappableControl>();

            if (swappableObject == null)
                continue;

            if (swappableObject.Swappable)
                swappableObject.Swap();
        }
    }
}
