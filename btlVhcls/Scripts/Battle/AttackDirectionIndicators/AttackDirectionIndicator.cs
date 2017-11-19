using UnityEngine;
using System.Collections;

public class AttackDirectionIndicator : MonoBehaviour
{
	public tk2dSprite indicator;
	public GameObject wrapper;
	public float fadeOutSpeed = 1.5f;

	private VehicleController attacker;
	private Color indicatorColor;

	void Awake()
	{
		Dispatcher.Subscribe(EventId.TankDamageApplied, DetermineAtackDirectrion);
		indicatorColor = indicator.color;
	}

	void OnDestroy()
	{
		Dispatcher.Unsubscribe(EventId.TankDamageApplied, DetermineAtackDirectrion);
	}

	private void DetermineAtackDirectrion(EventId id, EventInfo ei)
	{
        if (BattleController.MyVehicle == null)
            return;

        EventInfo_U info = (EventInfo_U)ei;

        if ((int)info[0] != BattleController.MyPlayerId)
            return;

	    if (!BattleController.allVehicles.TryGetValue((int)info[2], out attacker))
            return;

		Vector3 localAttackerCoords = BattleCamera.Instance.transform.InverseTransformPoint(attacker.transform.position);

        Vector3 shotDirection = Vector3.ProjectOnPlane(localAttackerCoords, Vector3.up);

		shotDirection.y = shotDirection.z;
		shotDirection.z = 0;

		wrapper.transform.up = shotDirection;

		StartCoroutine(IndicatorFadeOut());
	}

	private IEnumerator IndicatorFadeOut()
	{
		float alpha = 1;

		while (alpha > 0)
		{
			alpha = Mathf.Lerp(alpha, 0, fadeOutSpeed * Time.fixedDeltaTime);
			indicatorColor.a = alpha;
			indicator.color = indicatorColor;
			yield return new WaitForEndOfFrame();
		}
	}
}
