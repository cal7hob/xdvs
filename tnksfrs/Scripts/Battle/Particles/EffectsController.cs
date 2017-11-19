using UnityEngine;

namespace XD
{
    public class EffectsController : MonoBehaviour, ISubscriber
    {
        [SerializeField]
        private float           smokeByLowHealthTreshold = 0.4f;

        private IUnitBehaviour  unitBehavior = null;
        private bool            smokeIsActive = false;

        public string Description
        {
            get;
            set;
        }

        private void Start()
        {
            unitBehavior = GetComponent<IUnitBehaviour>();
            unitBehavior.AddSubscriber(this);
        }

        public void Reaction(Message message, params object[] parameters)
        {
            switch (message)
            {
                case Message.Hit:
                    SetDamageEffects(parameters.Get<Clamper>());
                    break;
            }
        }

        private void SetDamageEffects(Clamper hp)
        {
            if (!unitBehavior.IsAvailable)
            {
                return;
            }

            if (smokeIsActive)
            {
                return;
            }

            if (hp.Percent <= smokeByLowHealthTreshold)
            {
                unitBehavior.SendMessageToFX(Message.EffectRequest, EffectTarget.EngineSmoke, true);
                smokeIsActive = true;
            }
        }
    }
}