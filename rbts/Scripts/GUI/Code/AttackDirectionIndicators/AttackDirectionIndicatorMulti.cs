using System.Collections;
using UnityEngine;

public class AttackDirectionIndicatorMulti : MonoBehaviour
{
    public tk2dSprite sprIndicator;
    public tk2dTextMesh lblDamage;
    public Transform wrapper;
    public float duration = 1.5f;
    public float fadeOutDuration = 0.5f;

    private int damage;
    private Color indicatorColor;
    private Color labelColor;
    private VehicleController attacker;

    void Awake()
    {
        indicatorColor = sprIndicator.color;
        labelColor = lblDamage.color;

        sprIndicator.gameObject.SetActive(false);
        lblDamage.gameObject.SetActive(false);

        Messenger.Subscribe(EventId.BeforeReconnecting, OnReconnect);
    }

    void OnDestroy()
    {
        Messenger.Unsubscribe(EventId.BeforeReconnecting, OnReconnect);
    }

    void Update()
    {
        if (attacker != null)
            SetValues();
    }

    public void Indicate(VehicleController attacker, int damage)
    {
        this.attacker = attacker;
        this.damage = damage;

        SetValues();

        StartCoroutine(Fading());
    }

    public bool CheckAvailability(VehicleController attacker)
    {
        return this.attacker == null || this.attacker == attacker;
    }

    private static Vector3 CalcShotDirection(VehicleController attacker)
    {
        return CalcShotDirection(attacker.transform.position);
    }

    private static Vector3 CalcShotDirection(Vector3 attackerPosition)
    {
        Transform checkingTransform = BattleController.MyVehicle.Turret ?? BattleController.MyVehicle.transform;

        Vector3 localAttackerCoords = checkingTransform.InverseTransformPoint(attackerPosition);

        Vector3 shotDirection = Vector3.ProjectOnPlane(localAttackerCoords, Vector3.up);

        shotDirection.y = shotDirection.z;
        shotDirection.z = 0;

        return shotDirection;
    }

    private void SetValues()
    {
        Vector3 shotDirection = CalcShotDirection(this.attacker);

        wrapper.up = shotDirection;

        lblDamage.text = damage.ToString();
        lblDamage.transform.up = Vector3.up;
    }

    private void SetAlpha(float value)
    {
        indicatorColor.a = value;
        labelColor.a = value;

        sprIndicator.color = indicatorColor;
        lblDamage.color = labelColor;
    }

    private IEnumerator Fading()
    {
        if (!sprIndicator.gameObject.activeSelf)
            sprIndicator.gameObject.SetActive(true);

        if (!lblDamage.gameObject.activeSelf)
            lblDamage.gameObject.SetActive(true);

        float alpha = 1;

        SetAlpha(1);

        float estimated = duration;

        while (estimated > 0)
        {
            estimated -= Time.deltaTime;
            yield return null;
        }

        while (alpha > 0)
        {
            alpha = Mathf.MoveTowards(alpha, 0, Time.deltaTime / fadeOutDuration);

            SetAlpha(alpha);

            yield return null;
        }

        attacker = null;

        sprIndicator.gameObject.SetActive(false);
        lblDamage.gameObject.SetActive(false);
    }

    private void OnReconnect(EventId id, EventInfo ei)
    {
        attacker = null;
    }
}
