namespace StateMachines
{
    public abstract class State<TStateMachineSlave> where TStateMachineSlave : IStateMachineSlave
    {
        protected TStateMachineSlave slave;

        protected State(TStateMachineSlave slave)
        {
            this.slave = slave;
        }

        public abstract void BeforeStateChange();
        public abstract void OnStateChanged();
        public abstract void Update();
    }
}
