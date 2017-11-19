using UnityEngine;

namespace XD
{
    public abstract class SubStore : MonoBehaviour, ISubStore
    {
        public virtual string Description
        {
            get; set;
        }

        public virtual void Reaction(Message message, params object[] parameters)
        {
            
        }

        public virtual void Init(IStore store)
        {
            
        }
    }
}