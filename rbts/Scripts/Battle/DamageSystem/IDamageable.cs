using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDamageable
{
    int Health { get; }
    void TakeDamage(int damage, IDamageInflicter damageInflicter, Vector3 position);
    /// <summary>
    /// Останавливает ли попавший снаряд
    /// </summary>
    bool Solid { get; }
    Bounds Bounds { get; }
}
