using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

namespace Pool
{
    public class PoolManager : MonoBehaviour
	{
        struct PoolWaiting
        {
            public PoolWaiting(IPool pool, System.Action<IPool> onAssetLoaded)
            {
                this.pool = pool;
                this.onAssetLoaded = onAssetLoaded;
            }

            public readonly IPool pool;
            public readonly System.Action<IPool> onAssetLoaded;
        }

        public static PoolManager Instance { get { return instance; } }
        private static PoolManager instance;
		private Dictionary<string, IPool> pools = new Dictionary<string, IPool>(4);
        private List<PoolWaiting> waitedPools = new List<PoolWaiting>();
        private List<PoolWaiting> waitedPoolsToDelete = new List<PoolWaiting>();
		
		void Awake()
		{
			instance = this;
		}

	    void Update()
	    {
	        if (waitedPools.Count == 0)
	            return;

            for (int i = 0; i < waitedPools.Count; ++i)
            {
                if (waitedPools[i].pool.IsReady)
                {
                    waitedPools[i].onAssetLoaded(waitedPools[i].pool);
                    waitedPoolsToDelete.Add(waitedPools[i]);
                }
            }

	        for (int i = 0; i < waitedPoolsToDelete.Count; ++i)
	        {
                waitedPools.Remove(waitedPoolsToDelete[i]);
	        }

            waitedPoolsToDelete.Clear();
	    }
		
		void OnDestroy()
		{
            instance = null;
		}

        public static T GetObject<T>(string path, Vector3 position, Quaternion rotation, Transform parent = null) where T : PoolObject
		{
            if (instance == null)
            {
                CreateManagerInstance();
		    }

            DPool<T> pool = GetPool<T>(path);
			T obj = pool.GetObject(position, rotation, parent);
            if (obj == null)
            {
                Debug.LogError("Trying to get object from pool before complete its creation (asynchronous)");
                return null;
            }

            obj.transform.position = position;
		    obj.transform.rotation = rotation;

            if (obj.transform.parent != instance.transform)
            {
                obj.transform.SetParent(instance.transform);
            }

            return obj;
		}

        // TODO: Demetri Переделать, чтобы приносил в колбеке не пул, а запрошенный объект

        public static void GetPoolAsync<T>(string path, int maxSize = -1, System.Action<IPool> onAssetLoaded = null) where T : PoolObject
        {
            if (instance == null)
            {
                CreateManagerInstance();
            }

            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("PoolManager: Trying to create pool by null or empty path");
                return;
            }

            DPool<T> pool;
            IPool somePool;
            if (!instance.pools.TryGetValue(path, out somePool) || !somePool.IsReady)
            {
                pool = new DPool<T>(path, maxSize, true);
                instance.pools[path] = pool;
                if (onAssetLoaded != null)
                {
                    instance.waitedPools.Add(new PoolWaiting(pool, onAssetLoaded));
                }
                return;
            }

            if (onAssetLoaded != null)
            {
                onAssetLoaded((DPool<T>)somePool);
            }
        }


        public static DPool<T> GetPool<T>(string path, int maxSize = -1) where T : PoolObject
	    {
            if (instance == null)
            {
                CreateManagerInstance();
            }

            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("PoolManager: Trying to create pool by null or empty path");
                return null;
            }

            IPool somePool;
            if (instance.pools.TryGetValue(path, out somePool))
            {
                return (DPool<T>) somePool;
            }

            DPool<T> pool = new DPool<T>(path, maxSize, false);
            instance.pools[path] = pool;
            return pool;
	    }

	    public static void MarkPoolAsCustom(string path, object customer)
	    {
            if (instance == null)
            {
                CreateManagerInstance();
            }

            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("PoolManager: Trying to mark pool as necessary by null or empty path");
                return;
            }

	        IPool pool;
            if (!instance.pools.TryGetValue(path, out pool))
            {
                Debug.LogErrorFormat("PoolManager: Trying to mark unexisted pool ({0}) as necessary", path);
                return;
            }

            pool.MarkAsNecessary(customer.GetHashCode());
	    }

        public static void ReleasePool(string path, object customer)
        {
            if (customer != null)
            {
                ReleasePool(path, customer.GetHashCode());
            }
        }

        public static void ReleasePool(string path, int customerHash)
	    {
            if (instance == null)
            {
                CreateManagerInstance();
            }

            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("PoolManager: Trying to release pool by null or empty path");
                return;
            }

            IPool pool;
            if (!instance.pools.TryGetValue(path, out pool))
            {
                return;
            }

	        if (pool.Release(customerHash))
	        {
	            instance.pools.Remove(path);
	        }
	    }

	    private static void CreateManagerInstance()
	    {
            GameObject poolManager = new GameObject("PoolManager");
	        instance = poolManager.AddComponent<PoolManager>();
            Debug.LogWarning("There was no PoolManager on the scene. Created.");
        }
	}
}