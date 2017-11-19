using System.Collections;
using UnityEngine;

public class TweenShake : TweenBase
{
    public enum AnimationType
    {
        Perlin,
        Random,
        Periodic,
    }

    public AnimationType animationType = AnimationType.Perlin;

    /// <summary>
    /// How long the tween (shaking) should last in seconds. If set to 0 no tween is used, happens instantly.
    /// </summary>
    public float duration = 0.5f;

    public float speed = 5;
    public float magnitude = 10;

    private Vector3 startPosition; //original position
    private float elapsed;

    public override void OnInit()
    {
        startPosition = transform.localPosition;
    }

    public override void OnSetToInitialState()
    {
        transform.localPosition = startPosition;
    }

    protected override IEnumerator Animate()
    {
        tweenInProgress = true;
        elapsed = 0;
        float damper = 0;
        float x = 0;
        float y = 0;
        float randomStart = Random.Range(-1000.0f, 1000.0f);
        float randomStartX = Random.Range(-1000.0f, 1000.0f);
        float randomStartY = Random.Range(-1000.0f, 1000.0f);

        #region Механизм анимации

        while (tweenInProgress && elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float percentComplete = elapsed / duration;

            switch (animationType)
            {
                case AnimationType.Perlin:
                    // We want to reduce the shake from full power to 0 starting half way through
                    damper = 1.0f - Mathf.Clamp(2.0f * percentComplete - 1.0f, 0.0f, 1.0f);

                    // Calculate the noise parameter starting randomly and going as fast as speed allows
                    float alpha = randomStart + speed * percentComplete;

                    // map noise to [-1, 1]
                    x = Mathf.PerlinNoise(alpha, 0.0f) * 2.0f - 1.0f;
                    y = Mathf.PerlinNoise(0.0f, alpha) * 2.0f - 1.0f;

                    break;

                case AnimationType.Random:
                    damper = 1.0f - Mathf.Clamp(4.0f * percentComplete - 3.0f, 0.0f, 1.0f);

                    // map noise to [-1, 1]
                    x = Random.value * 2.0f - 1.0f;
                    y = Random.value * 2.0f - 1.0f;

                    break;

                case AnimationType.Periodic:
                    damper = 1.0f - Mathf.Clamp(4.0f * percentComplete - 3.0f, 0.0f, 1.0f);

                    // map noise to [-1, 1]
                    x = Mathf.Sin(randomStartX + percentComplete * speed);
                    y = Mathf.Cos(randomStartY + percentComplete * speed);

                    break;
            }

            x *= magnitude * damper;
            y *= magnitude * damper;

            transform.localPosition = startPosition + new Vector3(x, y, startPosition.z);

            yield return null;
        }

        #endregion

        if (setToInitialStateAtStop)
            OnSetToInitialState();

        tweenInProgress = false;
    }

    protected override bool IsParametersValid()
    {
        return duration > 0;
    }
}
