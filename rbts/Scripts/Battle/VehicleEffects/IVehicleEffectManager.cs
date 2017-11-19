using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IVehicleEffectManager
{
    void AddEffect(VehicleEffectData effect);
    void CancelAllEffects();
    void Update();
}
