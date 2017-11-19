using UnityEngine;

namespace Pool
{
    public abstract class PoolObject : MonoBehaviour
    {
        private string path;

        public abstract void OnTakenFromPool();
        public abstract void OnReturnedToPool();
        public abstract void OnPreWarm();

        public void SetPoolKey(string path)
        {
            this.path = path;
        }

        public void SetOrientation(Vector3 position, Quaternion rotation)
        {
            transform.position = position;
            transform.rotation = rotation;
        }

        protected void ReturnObject()
        {
            PoolManager.ReturnObject(this, path);
        }
    }
}
