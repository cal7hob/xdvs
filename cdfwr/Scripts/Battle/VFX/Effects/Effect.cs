using System.Collections;
using Pool;

namespace VFX
{
    public abstract class Effect : PoolObject
    {
        public abstract void Play();
        public abstract void Stop();
        public abstract bool IsPlaying { get; }

        public override void OnTakenFromPool()
        {
            StartCoroutine(Playing());
        }

        public override void OnReturnedToPool()
        {
            Stop();
        }

        public override void OnPreWarm()
        {
            ReturnObject();
        }

        public IEnumerator Playing()
        {
            Play();

            while (IsPlaying)
            {
                yield return null;
            }

            ReturnObject();
        }
    }
}
