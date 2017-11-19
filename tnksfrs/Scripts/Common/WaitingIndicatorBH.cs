using UnityEngine;

public class WaitingIndicatorBH : WaitingIndicatorBase
{
    [SerializeField] private float rotateSmoothlyPause = 0.1f;
    [SerializeField] float rotateSmoothlyAngle = -18f;

    private float rotateSmoothlyPauseCounter = 0f;

	protected override void FixedUpdate()
	{
		/*if (wrapper == null || !wrapper.activeSelf || circle == null)
            return;
        rotateSmoothlyPauseCounter += Time.fixedDeltaTime;
        if (rotateSmoothlyPauseCounter >= rotateSmoothlyPause)
        {
            rotateSmoothlyPauseCounter = 0;
            circle.transform.Rotate(0, 0, rotateSmoothlyAngle);
        }*/
    }
}
