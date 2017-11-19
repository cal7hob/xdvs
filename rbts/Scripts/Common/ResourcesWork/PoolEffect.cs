using System;
using UnityEngine;
using System.Collections;
using Pool;

namespace Pool
{
    public class PoolEffect : PoolObject
    {
        private new ParticleSystem particleSystem;

        public ParticleSystem ParticleSystem
        {
            get
            {
                if (particleSystem == null)
                    particleSystem = GetComponentInChildren<ParticleSystem>();

                return particleSystem;
            }
        }

        protected virtual void Awake()
        {
            Activate();
        }

        public override void OnGetFromPool()
        {
            Activate();
        }

        void Update()
        {
            if (ParticleSystem.IsAlive(false))
                return;

            Disactivate();
        }

        public void Disactivate()
        {
            ReturnObject();
        }

        public override void ReturnObject()
        {
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            base.ReturnObject();
        }

        private void Activate()
        {
            gameObject.SetActive(true);
            ParticleSystem.Play(true);
        }
    }


}
