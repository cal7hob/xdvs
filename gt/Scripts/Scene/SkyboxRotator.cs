using UnityEngine;

public class SkyboxRotator : MonoBehaviour
{
    public float rotationSpeed = 1.0f;

    private float rotationAmount;
    private Material skyboxMaterial;

    void Awake() { skyboxMaterial = RenderSettings.skybox; }

    void FixedUpdate()
    {
        rotationAmount += Time.deltaTime * rotationSpeed;

        rotationAmount = rotationAmount > 360.0f ? 0.0f : rotationAmount;

        skyboxMaterial.SetFloat("_Rotation", rotationAmount);
    }
}
