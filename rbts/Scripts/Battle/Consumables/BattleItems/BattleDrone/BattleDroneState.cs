using System.Collections;
using System.Collections.Generic;
using StateMachines;
using UnityEngine;

public abstract class BattleDroneState : IState
{
    public const int APPEARING_ID = 1;
    public const int SEARCHING_ID = 2;
    public const int ATTACK_ID = 3;
    public const int WAITING_TARGET_ID = 4;
    public const int CLONE_REGULAR_ID = 5;
    public const int OFFLINE_ID = 6;


    protected BattleDrone battleDrone;

    protected BattleDroneState(BattleDrone owner)
    {
        battleDrone = owner;
    }

    public abstract string Name { get; }

    public virtual void OnEnter(IState previousState)
    { }

    public virtual void OnExit(IState nextState)
    { }

    public virtual void Update()
    { }

    public override string ToString()
    {
        return Name;
    }
}
