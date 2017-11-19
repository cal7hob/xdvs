using UnityEngine;

public class HelicopterAnimationController : MonoBehaviour
{
    public Rotor[] rotors;

    private HelicopterController helicopterController;

    void Start()
    {
        helicopterController = GetComponent<HelicopterController>();
    }

    public void Receive()
    {
        SpinRotors(helicopterController.AccelerationProgress);
    }

    public void SpinRotors(float value)
    {
        foreach (Rotor rotor in rotors)
            rotor.Rotate(value);
    }
}
