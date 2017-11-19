using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Pool
{
    public interface IPool
    {
        void AcceptObject(PoolObject poolObject);
        void OnPoolObjectDestroy(PoolObject poolObject);
        bool IsReady { get; }
        void MarkAsNecessary(int customerHash);
        bool Release(int customerHash);

    }

    public class DPool<T> : IPool where T:PoolObject
    {
        private HashSet<int> customersHashes;

        private readonly string path;
        private T asset;
        private readonly Queue<T> innerObjects;
        private ResourceRequest asyncRequest;
        private readonly int maxSize;
        private readonly List<PoolObject> connectedObjects = new List<PoolObject>();
        private bool released;
        public bool IsReady { get { return asyncRequest == null || asyncRequest.isDone; } }

        public DPool(string path, int maxSize = -1, bool asyncResLoading = false)
        {
            this.path = path;
            this.maxSize = maxSize;
            innerObjects = new Queue<T>(4);
            LoadAsset(asyncResLoading);
        }

        public void MarkAsNecessary(int customerHash)
        {
            if (customersHashes == null)
            {
                customersHashes = new HashSet<int>();
            }

            customersHashes.Add(customerHash);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="customerHash">Hashcode of object that no more needs pool</param>
        /// <returns>TRUE if pool is not necessary anymore</returns>
        public bool Release(int customerHash)
        {
            if (customersHashes == null || !customersHashes.Remove(customerHash))
            {
                Debug.LogError("Object tries to release pool without prerequisite marking as custom");
                return false;
            }

            if (customersHashes.Count == 0)
            {
                Clear();
                released = true;
                return true;
            }

            return false;
        }

        private void LoadAsset(bool asynchronously)
        {
            string resPath = string.Format("{0}/{1}", GameManager.CurrentResourcesFolder, path);

            if (!asynchronously)
            {
                asset = (T)Resources.Load<PoolObject>(resPath);
            }
            else
            {
                asyncRequest = Resources.LoadAsync<PoolObject>(resPath);
            }
        }

        private void Clear()
        {
            asset = null;

            while (innerObjects.Count > 0)
            {
                PoolObject obj = innerObjects.Dequeue();
                Object.Destroy(obj.gameObject);
            }
        }

        public T GetObject(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            T obj;
            if (innerObjects.Count == 0)
            {
                if (maxSize > 0 && connectedObjects.Count == maxSize)
                {
                    StealObject();
                }
                else
                {
                    AddObject();
                }
            }

            obj = innerObjects.Dequeue();
            obj.OnGetFromPool();
            obj.transform.position = position;
            obj.transform.rotation = rotation;

            if (parent != null)
            {
                obj.transform.SetParent(parent);
            }

            return obj;
        }

        private void AddObject()
        {
            if (asyncRequest != null)
            {
                if (asyncRequest.isDone)
                {
                    asset = (T)asyncRequest.asset;
                    asyncRequest = null;
                }
                else
                {
                    return;
                }
            }

            T obj = Object.Instantiate(asset);
            obj.SetReturnPool(this);
            if (maxSize > 0)
            {
                connectedObjects.Add(obj);
            }

            innerObjects.Enqueue(obj);
        }

        private void StealObject()
        {
            PoolObject poolObject = connectedObjects[0];
            float maxPriority = poolObject.StealPriority;

            for (int i = 1; i < connectedObjects.Count; i++)
            {
                float priority = connectedObjects[i].StealPriority;
                if (connectedObjects[i].StealPriority > maxPriority)
                {
                    maxPriority = priority;
                    poolObject = connectedObjects[i];
                }
            }

            poolObject.ReturnObject();
        }

        public void AcceptObject(PoolObject obj)
        {
            if (!released)
            {
                Transform managerTr = PoolManager.Instance.transform;
                innerObjects.Enqueue((T)obj);
                if (obj.transform.parent != managerTr)
                {
                    obj.transform.SetParent(managerTr);
                }
                return;
            }

            Object.Destroy(obj.gameObject);
        }
        
        public override string ToString()
        {
            return string.Format("DPool ({0})", path);
        }

        public void OnPoolObjectDestroy(PoolObject poolObject)
        {
            connectedObjects.Remove(poolObject);
        }
    }
}