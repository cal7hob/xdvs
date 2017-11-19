using System;
using System.Collections.Generic;
using Shooting;
using StateMachines;
using UnityEngine;

public enum ShootingStates
{
    noShoot,
    manual,
    automatic
}

public class ShootingController : MonoBehaviour, IStateMachineSlave
{
    protected StateMachine<State<ShootingController>, ShootingController> shootingStateMachine;
    protected VehicleController currentCreature;

    public VehicleController CurrentCreature { get { return currentCreature; } }
    public StateMachine<State<ShootingController>, ShootingController> ShootingStateMachine { get { return shootingStateMachine; } }

    public bool FirePrimaryBtn { get { return currentCreature.FirePrimaryBtn; } }
    public bool FirePrimaryBtnDown { get { return currentCreature.FirePrimaryBtnDown; } }
    public bool DoubleTap { get { return currentCreature.DoubleTap; } }

    void Awake()
    {
        Initialize();   
    }

    private void Initialize()
    {
        currentCreature = GetComponent<VehicleController>();
        shootingStateMachine = new StateMachine<State<ShootingController>, ShootingController>
            (this, new Dictionary<Enum, State<ShootingController>>
            {
                { ShootingStates.noShoot, new NoShootState(this) },
                { ShootingStates.manual, new ManualShootState(this) },
                { ShootingStates.automatic, new AutomaticShootingState(this) }
            });
    }

    public void Shoot()
    {
        if (currentCreature.IsMine)
        {
            currentCreature.turretController.Fire();

           /* Dispatcher.Send(
            id: EventId.StartBurstFire,
            info: new EventInfo_II(currentCreature.data.playerId, (int)currentCreature.PrimaryShellInfo.type),
            target: Dispatcher.EventTargetType.ToOthers);*/
        }
    }

    void Update()
    {
        shootingStateMachine.Update();
    }

}
