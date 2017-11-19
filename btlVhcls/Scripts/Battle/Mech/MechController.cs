using UnityEngine;

public class MechController : MonoBehaviour
{
    public float walkSpeed = 1.0f;
    public float turnSpeed = 1.0f;
    public float bodyRotationSpeed = 100.0f;
    public Transform body;
    public Animator animator;

    protected virtual bool FireButtonPressed
    {
        get { return XDevs.Input.GetButtonDown("Fire Primary"); }
    }

    protected virtual float TurretAxisControl
    {
        get { return XDevs.Input.GetAxis("Turret Rotation"); }
    }

    protected virtual float XAxisControl
    {
        get { return XDevs.Input.GetAxis("Turn Left/Right"); }
    }

    protected virtual float YAxisControl
    {
        get { return XDevs.Input.GetAxis("Move Forward/Backward"); }
    }

    void Update()
    {
        Animate();
        BodyRotation();
        DebugControl();
    }

    private void Animate()
    {
        animator.SetFloat("Turn", XAxisControl * turnSpeed);
        animator.SetFloat("Walk", YAxisControl * walkSpeed);
    }

    private void BodyRotation()
    {
        var deltaForRotation = TurretAxisControl;

        if (HelpTools.Approximately(deltaForRotation, 0))
            return;

        body.Rotate(0, deltaForRotation * bodyRotationSpeed * Time.deltaTime, 0, Space.Self);
    }

    private void DebugControl()
    {
        if (Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            walkSpeed += 0.05f;
            turnSpeed += 0.05f;
            bodyRotationSpeed += 0.05f;
        }

        if (Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            walkSpeed -= 0.05f;
            turnSpeed -= 0.05f;
            bodyRotationSpeed -= 0.05f;
        }
    }
}
