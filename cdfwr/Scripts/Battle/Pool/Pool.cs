using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Pool
{
    public class Pool
    {
        private string path;
        private PoolObject asset;
        private Queue<PoolObject> objects;

        public Pool(string path)
        {
            this.path = path;
            asset = Resources.Load<PoolObject>(string.Format("{0}/{1}", GameManager.CurrentResourcesFolder, path));
            objects = new Queue<PoolObject>(4);
        }

        public T GetObject<T>() where T : PoolObject
        {
            T obj;
            if (objects.Count > 0)
            {
                obj = (T) objects.Dequeue();
            }
            else
            {
                obj = (T) Object.Instantiate(asset);
                obj.SetPoolKey(path);
            }

            obj.OnTakenFromPool();

            return obj;
        }

        public void ReturnObject(PoolObject poolObject)
        {
            objects.Enqueue(poolObject);
            poolObject.OnReturnedToPool();
        }
    }
}
