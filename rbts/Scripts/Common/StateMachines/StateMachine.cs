using System;
using UnityEngine;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace StateMachines
{
    public class StateMachine<T> where T : IState
    {
        private Dictionary<int, T> states;

        private IState currentState;
        public IState CurrentState { get { return currentState;} }

        public StateMachine(IStateMachineControlled owner)
        {
            Dictionary<int, IState> statesDic = owner.GetStateCache();
            states = new Dictionary<int, T>(statesDic.Count);

            foreach (var someState in statesDic)
            {
                states.Add(someState.Key, (T)someState.Value);
            }
        }

        public void SetState(int stateId)
        {
            T newState;
            if (!states.TryGetValue(stateId, out newState))
            {
                throw new Exception(string.Format("Unknown state id ({0})", stateId));
            }

            if (currentState != null)
            {
                if (currentState.Equals(newState))
                {
                    Debug.LogErrorFormat("Trying to change state from {0} to same one ({1})", currentState.Name, newState.Name);
                    return;
                }

                currentState.OnExit(newState);
            }

            newState.OnEnter(currentState);
            currentState = newState;
        }

        public void Update()
        {
            if (currentState != null)
            {
                currentState.Update();
            }
        }
    }
}