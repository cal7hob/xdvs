using UnityEngine;

public class CentredThrottleLevel : ThrottleLevel
{
    private const float FACTOR = 2.0f;

    protected override float MaxFillerSize
    {
        get { return sprBackground.dimensions.y / FACTOR; }
    }

    protected override void ResetThumbPosition()
    {
        Vector3 currentThumbPosition = thumbSprite.transform.localPosition;
        currentThumbPosition.y = MaxFillerSize;
        thumbPosition = currentThumbPosition;
    }

    protected override void SetupFiller()
    {
        fillerSize = fillerSprite.dimensions;

        fillerSize.y = (thumbSprite.transform.localPosition.y / FACTOR) * Mathf.Sign(Value);

        fillerSprite.dimensions = fillerSize;
    }

    protected override void UpdateFiller()
    {
        targetFillerDimensions = (fillerCollider.bounds.size.y / FACTOR) * Value;

        fillerSize.y
            = Mathf.Clamp(
                value:  targetFillerDimensions * AccelerationProgress,
                min:    -MaxFillerSize,
                max:    MaxFillerSize);

        fillerSprite.dimensions = fillerSize;
    }

    protected override void UpdateValue()
    {
        float newThumbPosition = thumbSprite.transform.localPosition.y - MinThumbPosition;
        float halfMaxThumbPosition = MaxFillerSize;

        Value = Mathf.Clamp((newThumbPosition - halfMaxThumbPosition) / halfMaxThumbPosition, -1, 1);
    }

    protected override void CalcThumbAndBarReleased()
    {
        thumbPosition.y
            = Mathf.MoveTowards(
                current:    thumbPosition.y,
                target:     MaxFillerSize,
                maxDelta:   Time.deltaTime * THUMB_SLIDING_SPEED);
    }
}
