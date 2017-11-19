using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Силовой щит вокруг транспорта
// Для идеального отображения должен иметь скейл и размеры (1; 1; 1).
public class ShieldItem : BattleItem
{
    protected override void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        base.OnPhotonInstantiate(info);

        Bounds vehBounds = GetVehBounds();
        transform.position = vehBounds.center;
        transform.localScale = vehBounds.size;
        transform.SetParent(owner.transform);

        Invoke("Disappearing", consumableInfo.duration);
    }

    private Bounds GetVehBounds()
    {
        Bounds totalBounds = new Bounds(transform.position, Vector3.zero);
        Renderer[] renderers = owner.GetComponentsInChildren<Renderer>();

        foreach (Renderer rend in renderers)
        {
            totalBounds = MiscTools.SumBounds(totalBounds, rend.bounds);
        }

        return totalBounds;
    }

    private void Disappearing()
    {
        Destroy(gameObject);
    }
}
