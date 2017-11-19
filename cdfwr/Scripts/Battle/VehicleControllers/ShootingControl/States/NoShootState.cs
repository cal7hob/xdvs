using StateMachines;
using UnityEngine;

namespace Shooting
{
    public class NoShootState : State<ShootingController>
    {
        public NoShootState(ShootingController slave) : base(slave)
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
        }
    }
}
