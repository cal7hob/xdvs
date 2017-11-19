using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RectGunsight : MonoBehaviour
{
    private tk2dSlicedSprite sprite;
    private Camera mainCamera;
    private Camera camera2D;
    private float minRectX, minRectY;

    void Awake()
    {
        sprite = GetComponent<tk2dSlicedSprite>();
        mainCamera = Camera.main;
        camera2D = tk2dCamera.Instance.ScreenCamera;

        Vector2 normSize = sprite.GetUntrimmedBounds().size;
        minRectX = (sprite.borderLeft + sprite.borderRight) * normSize.x;
        minRectY = (sprite.borderTop + sprite.borderBottom) * normSize.y;
    }

    public void Hide()
    {
        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
            transform.localPosition = Vector3.down * 10000f;
        }
    }

    public void SetBounds(Bounds worldBounds)
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
        
        // Calculating rect center
        Vector3 viewportPos = mainCamera.WorldToViewportPoint(worldBounds.center);

        // Calculating rect size
        Vector3 worldMin = worldBounds.min;
        Vector3 worldMax = worldBounds.max;

        Vector2 min = Vector2.one;
        Vector2 max = Vector2.zero;
        Vector3 currentTested = new Vector3();

        CheckForMinMax(worldMin, ref min, ref max);
        CheckForMinMax(worldMax, ref min, ref max);

        currentTested.Set(worldMin.x, worldMin.y, worldMax.z);
        CheckForMinMax(currentTested, ref min, ref max);
        currentTested.Set(worldMin.x, worldMax.y, worldMin.z);
        CheckForMinMax(currentTested, ref min, ref max);
        currentTested.Set(worldMin.x, worldMax.y, worldMax.z);
        CheckForMinMax(currentTested, ref min, ref max);
        currentTested.Set(worldMax.x, worldMin.y, worldMin.z);
        CheckForMinMax(currentTested, ref min, ref max);
        currentTested.Set(worldMax.x, worldMin.y, worldMax.z);
        CheckForMinMax(currentTested, ref min, ref max);
        currentTested.Set(worldMax.x, worldMax.y, worldMin.z);
        CheckForMinMax(currentTested, ref min, ref max);
        
        Vector2 viewportSize = max - min;
        
        transform.position = camera2D.ViewportToWorldPoint(viewportPos);
        sprite.dimensions = new Vector2(Mathf.Clamp(viewportSize.x * 1920f, minRectX, 1920f), Mathf.Clamp(viewportSize.y * 1080f, minRectY, 1080f));
    }

    private void CheckForMinMax(Vector3 tested, ref Vector2 min, ref Vector2 max)
    {
        tested = mainCamera.WorldToViewportPoint(tested);
        
        // X
        if (tested.x < min.x)
        {
            min.x = tested.x;
        }
        else if (tested.x > max.x)
        {
            max.x = tested.x;
        }

        // Y
        if (tested.y < min.y)
        {
            min.y = tested.y;
        }
        else if (tested.y > max.y)
        {
            max.y = tested.y;
        }
    }
}
