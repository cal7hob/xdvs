using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum DamageSource
{
    VehicleDoes,
    HenchmanDoes
}

public interface IDamageInflicter
{
    int OwnerId { get; }
    bool IsLocal { get; }
    DamageSource DamageSource { get; }
}