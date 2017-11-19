using StateMachines;
using UnityEngine;

namespace Shooting
{
    public class ManualShootState : State<ShootingController>
    {
        public ManualShootState(ShootingController slave) : base(slave)
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
            if (slave.FirePrimaryBtn)
            {
                slave.Shoot();
            }
        }
    }
}
