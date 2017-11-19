using System;
using System.Collections.Generic;
using UnityEngine;

namespace StateMachines
{
    public interface IStateMachineSlave 
    {
    }

    public class StateMachine<TState, TStateMachineSlave>  
        where TState : State<TStateMachineSlave>
        where TStateMachineSlave : IStateMachineSlave
    {
        protected TStateMachineSlave slave;
        protected Dictionary<Enum, TState> states;
        protected TState previousState;
        protected TState currentState;

        public TState CurrentState { get { return currentState; } }

        public TState PreviousState { get { return previousState; } }

        public StateMachine(TStateMachineSlave slave, Dictionary<Enum, TState> states)
        {
            this.slave = slave;
            this.states = states;
        }

        public void SetState(Enum newStateEnum)
        {
            TState newState = null;

            if (!states.TryGetValue(newStateEnum, out newState))
            {
                Debug.LogErrorFormat("trying to set non-existing state: {0}", newStateEnum);
                return;
            }

            DoStateChange(newState);
        }

        public void SetState(TState newState)
        {
            if (newState == null)
            {
                Debug.LogErrorFormat("trying to set non-existing state: {0}", newState);
                return;
            }

            DoStateChange(newState);
        }

        private void DoStateChange(TState newState)
        {
            if (currentState != null)
            {
                currentState.BeforeStateChange();
            }

            previousState = currentState;
            currentState = newState;

            currentState.OnStateChanged();
        }

        public void SetNullState()
        {
            previousState = currentState;
            currentState = null;
        }

        public void Update()
        {
            if (!ReferenceEquals(currentState, null))
            {
                currentState.Update();
            }
        }
    }
}
