using UnityEngine;

public class GroundCameraMF : GroundCamera
{
    [Header("Смягчение движения")]
    [Tooltip("Скорость движения камеры по оси Y")]
    public float verticalSpeed = 0.7f;

    [Tooltip("Минимальная скорость поворота камеры по оси X.")]
    public float lookAtVerticalSpeedMin = 0.3f;

    [Tooltip("Максимальная скорость поворота камеры по оси X.")]
    public float lookAtVerticalSpeedMax = 5.0f;

    [Tooltip("Максимальная дельта положения лукпоинта по оси Y " +
             "(чем больше это значение, тем меньше рельеф будет влиять на поворот камеры).")]
    public float lookPointYMaxDelta = 3.0f;

    private float currentLookPointY;
    private float deltaLookPointY;

    public override void CamRegularMotion()
    {
        base.CamRegularMotion();
        LookAt();
    }

    protected override void MoveAroundObstacles()
    {
        hitInfo.point = Vector3.MoveTowards(hitInfo.point, vehicleInView.ShotPoint.position, correctCamDistance);
        Vector3 newPosition = Vector3.MoveTowards(transform.position, hitInfo.point, camTowardsSpeed * Time.deltaTime);
        ApplySmoothMotion(newPosition);
    }

    protected override void NormalMove()
    {
        Vector3 newPosition = Vector3.SmoothDamp(transform.position, camPos, ref camSmoothVelocity, 7f / vehicleInView.MaxSpeed);
        ApplySmoothMotion(newPosition);
    }

    private void ApplySmoothMotion(Vector3 newPosition)
    {
        float deltaYPosition = Mathf.Abs(newPosition.y - transform.position.y);
        float newYPosition = Mathf.MoveTowards(transform.position.y, newPosition.y, deltaYPosition * verticalSpeed);
        transform.position = new Vector3(newPosition.x, newYPosition, newPosition.z);
    }

    private void LookAt()
    {
        deltaLookPointY = Mathf.Abs(currentLookPointY - lookPointTransform.position.y);

        float currentlookAtVerticalSpeed = Mathf.Lerp(lookAtVerticalSpeedMin, lookAtVerticalSpeedMax, deltaLookPointY / lookPointYMaxDelta);

        currentLookPointY = Mathf.MoveTowards(currentLookPointY, lookPointTransform.position.y, currentlookAtVerticalSpeed * Time.deltaTime);

        Vector3 newLookPoint = new Vector3(lookPointTransform.position.x, currentLookPointY, lookPointTransform.position.z);

        transform.LookAt(newLookPoint);
    }

    protected override void SetLookPoint(Transform point)
    {
        lookPointTransform = point;
        currentLookPointY = point.position.y;
    }
}
