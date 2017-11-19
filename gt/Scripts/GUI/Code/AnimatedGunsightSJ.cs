using UnityEngine;
using System.Collections;

public class AnimatedGunsightSJ : MonoBehaviour
{
    public tk2dSprite innerSprite;
    public tk2dSprite outerSprite;
    public float innerRotationSpeed = 50;
    public float outerRotationSpeed = -50;

    void Update()
    {
        innerSprite.transform.Rotate(0, 0, innerRotationSpeed * Time.deltaTime);
        outerSprite.transform.Rotate(0, 0, outerRotationSpeed * Time.deltaTime);
    }
}
