using UnityEngine;

namespace Bots
{
    public class SoldierBotController : SoldierController
    {
        protected SoldierBotAI soldierBotAI;

        public override float XAxisControl
        {
            get { return soldierBotAI.BotXAxisControl; }
        }

        public override float YAxisControl
        {
            get { return soldierBotAI.BotYAxisControl; }
        }

        public override float TurretAxisControl
        {
            get { return soldierBotAI.BotTurretAxisControl; }
        }

        public override bool FirePrimaryBtn { get { return soldierBotAI.IsFireBtnPressed; } }

        public override bool IsAvailable
        {
            get { return base.IsAvailable; }
            set
            {
                base.IsAvailable = value;

                if (soldierBotAI != null)
                {
                    soldierBotAI.OnChangeAvailability(value);
                }
            }
        }

        protected override void OnPhotonInstantiate(PhotonMessageInfo info)
        {
            base.OnPhotonInstantiate(info);

            soldierBotAI = GetComponent<SoldierBotAI>();
            soldierBotAI.OnBotInstantieted();
        }

        protected override void SetShootingMode()
        {
            shootingController.ShootingStateMachine.SetState(ShootingStates.automatic);
        }

        protected override void Gunsight() // что GunSight() ?
        {
            CheckIfSomeBodyIsInGunSight();
        }

        protected void CheckIfSomeBodyIsInGunSight()
        {
            Debug.DrawRay(weapon.position, weapon.forward * 15);

            if (Physics.Raycast(weapon.position, weapon.forward, out gunsightHit, 200, HitMask))
            {
                OnWeaponAimed();
                return;
            }

            if (hasAimed)
            {
                Dispatcher.Send(EventId.TargetAimed, new EventInfo_IIB(data.playerId, data.playerId, false));
                hasAimed = false;
                TargetAimed = false;
            }

            hasHit = false; 
        }

        protected override void FixedUpdate()
        {
        }

        protected override void Update()
        {
            MovePlayer();
            UpdateEffects();
            Gunsight();
        }

#if UNITY_EDITOR
        public override void UpdateBotPrefabs(VehicleController nativeController)
        {
            var soldierController = nativeController as SoldierController;

            if (soldierController == null)
            {
                return;
            }

            id = soldierController.id;
            tankGroup = soldierController.tankGroup;
            data = soldierController.data;
            cameraPoint = soldierController.cameraPoint;
            lookPoint = soldierController.lookPoint;
            cameraEndPoint = soldierController.cameraEndPoint;
            shotPrefabPath = soldierController.shotPrefabPath;
            hitPrefabPath = soldierController.hitPrefabPath;
            terrainHitPrefabPath = soldierController.terrainHitPrefabPath;
            explosionPrefabPath = soldierController.explosionPrefabPath;
            shellPrefabPath = soldierController.shellPrefabPath;
            shootEffectPoints = soldierController.shootEffectPoints;
            engineSound = soldierController.engineSound;
            turretRotationSound = soldierController.turretRotationSound;
            shotSound = soldierController.shotSound;
            blowSound = soldierController.blowSound;
            explosionSound = soldierController.explosionSound;
            respawnSound = soldierController.respawnSound;
            maxSpeed = soldierController.maxSpeed;
            centerOfMass = soldierController.centerOfMass;
            continuousFire = soldierController.continuousFire;
            shotCorrection = soldierController.shotCorrection;
            turretRotationSpeedQualifier = soldierController.turretRotationSpeedQualifier;
            rotationSpeedQualifier = soldierController.rotationSpeedQualifier;
            animator = soldierController.animator;
            weaponSpawner = soldierController.WeaponsSpawner;
            turret = soldierController.Turret;
            ikController = soldierController.IkController;
            gunAiming = soldierController.gunAiming;
            shootAnimation = soldierController.shootAnimation;
            shotPoint = soldierController.shotPoint;

            weaponSpawner.AssignSoldierController(this);
            IkController.AssignSoldierController(this);

            gameObject.AddComponent<SoldierBotAI>();

            //var soundControllerAR = GetComponent<SoundControllerSoldier>();
            //DestroyImmediate(soundControllerAR, true);
        }
#endif
    }
}
