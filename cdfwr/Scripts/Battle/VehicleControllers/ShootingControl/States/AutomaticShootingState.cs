using StateMachines;
using UnityEngine;

namespace Shooting
{
    public class AutomaticShootingState : State<ShootingController>
    {
        public AutomaticShootingState(ShootingController slave) : base(slave)
        {
        }

        public override void BeforeStateChange()
        {
        }

        public override void OnStateChanged()
        {
        }

        public override void Update()
        {
            if (slave.CurrentCreature.TargetAimed || slave.FirePrimaryBtn)
            {
                slave.Shoot();
            }
        }
    }
}
