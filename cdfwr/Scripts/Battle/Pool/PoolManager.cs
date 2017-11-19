using System.Collections.Generic;
using UnityEngine;

namespace Pool
{
    public class PoolManager : MonoBehaviour
    {
        private static Dictionary<string, Pool> pools;

        void Awake()
        {
            pools = new Dictionary<string, Pool>(5);
        }

        void OnDestroy()
        {
            pools = null;
        }

        public static T GetObject<T>(string path) where T : PoolObject
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("PoolManager: given path is null");
                return null;
            }

            Pool pool;
            if (!pools.TryGetValue(path, out pool))
            {
                pool = new Pool(path);
                pools.Add(path, pool);
            }

            var poolObject = pool.GetObject<T>();
            return poolObject;
        }

        public static void ReturnObject(PoolObject poolObject, string poolKey)
        {
            Pool pool;
            if (!pools.TryGetValue(poolKey, out pool))
            {
                Debug.LogErrorFormat("Pool Manager: there is no pool at given key: {0}", poolKey);
                return;
            }

            pool.ReturnObject(poolObject);
        }

        public static void PreWarm<T>(string path, int objectCount) where T : PoolObject
        {
            if (pools.ContainsKey(path))
            {
                return;
            }

            var objects = new List<T>(objectCount);

            for (int i = 0; i < objectCount; i++)
            {
                objects.Add(GetObject<T>(path));
            }

            foreach (var o in objects)
            {
                o.OnPreWarm();
            }
        }

        public static void ClearAllPools()
        {
            pools.Clear();
        }
    }
}
