using UnityEngine;

public class Test : MonoBehaviour
{
    public AnimationCurve curve;

    public void TestFromEditor()
    {
/*        Keyframe[] frames =
        {
            new Keyframe(0, 0f),
            new Keyframe(0.5f, 1f),
            new Keyframe(1, 0f),
        };
        curve = new AnimationCurve(frames);
        curve.postWrapMode = WrapMode.Loop;

        float sum = 0f;

        float time = Time.realtimeSinceStartup;
        for (int i = 0; i < 10000000; ++i)
        {
            sum += Mathf.Sin(i / 100f);
        }

        Debug.Log("curve time = " + (Time.realtimeSinceStartup - time));

        sum = 0f;
        time = Time.realtimeSinceStartup;
        for (int i = 0; i < 10000000; ++i)
        {
            sum += curve.Evaluate(i / 100f);
        }

        Debug.Log("sin time = " + (Time.realtimeSinceStartup - time));*/
    }
}
