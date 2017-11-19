using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StateMachines
{
    public interface IState
    {
        void OnEnter(IState prevState);
        void OnExit(IState nextState);
        void Update();
        string Name { get; }
    }

    public interface IStateMachineControlled
    {
        void SetState(int stateId);
        Dictionary<int, IState> GetStateCache();
    }

}