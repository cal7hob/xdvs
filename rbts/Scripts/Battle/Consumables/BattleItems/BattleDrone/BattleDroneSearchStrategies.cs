using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BattleDroneEnemySearching
{
    public abstract class SearchStrategy
    {
        protected BattleDrone owner;

        protected SearchStrategy(BattleDrone owner)
        {
            this.owner = owner;
        }

        public abstract float GetSuitability(VehicleController vehicle);
    }

    public class NearestEnemy : SearchStrategy
    {
        public NearestEnemy(BattleDrone owner) : base(owner) { }

        public override float GetSuitability(VehicleController vehicle)
        {
            return -Vector3.SqrMagnitude(owner.transform.position - vehicle.transform.position);
        }
    }

    public class MostDamagedEnemy : SearchStrategy
    {
        public MostDamagedEnemy(BattleDrone owner) : base(owner) { }

        public override float GetSuitability(VehicleController vehicle)
        {
            return vehicle.Armor;
        }
    }

    public class LeastDamagedEnemy : SearchStrategy
    {
        public LeastDamagedEnemy(BattleDrone owner) : base(owner) { }

        public override float GetSuitability(VehicleController vehicle)
        {
            return vehicle.Armor;
        }
    }

    public class StrongestEnemy : SearchStrategy
    {
        public StrongestEnemy(BattleDrone owner) : base(owner) { }

        public override float GetSuitability(VehicleController vehicle)
        {
            return vehicle.Attack;
        }
    }


}