using StateMachines;

public class BattleDrone_Appearing : BattleDroneState
{
    public BattleDrone_Appearing(BattleDrone owner) : base(owner) {}

    public override string Name
    {
        get { return "Appearing"; }
    }

    public override void OnEnter(IState prevState)
    {
        battleDrone.StartCoroutine(battleDrone.Appearing());
    }
}
