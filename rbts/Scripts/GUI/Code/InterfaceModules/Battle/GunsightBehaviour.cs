using System;
using UnityEngine;

public class GunsightBehaviour : MonoBehaviour
{
    [Serializable]
    private class LoadingIndicator
    {
        public Vector2 direction = Vector2.zero;
        public Transform transform = null;
        [HideInInspector] public Vector2 initialPos;
    }

    [SerializeField] public GameObject targetLockGunsight;
    [SerializeField] private LoadingIndicator[] indicators;
    [SerializeField] private float maximumSpread = 40;
    [SerializeField] private bool inverted;

    private float factor;

    private void Awake()
    {
        foreach (var indicator in indicators)
        {
            indicator.initialPos = indicator.transform.localPosition;
        }
    }

    private void Update()
    {
        if (!BattleController.MyVehicle)
            return;

        if (!HelpTools.Approximately(BattleController.MyVehicle.WeaponReloadingProgress, 1, 0.0001f))
        {
            factor = inverted ?
                Mathf.Lerp(maximumSpread, 0, BattleController.MyVehicle.WeaponReloadingProgress)
                : Mathf.Lerp(0, maximumSpread, BattleController.MyVehicle.WeaponReloadingProgress);

            foreach (var indicator in indicators)
            {
                indicator.transform.localPosition =
                    indicator.initialPos + indicator.direction.normalized * factor;
            }
        }
    }
}
