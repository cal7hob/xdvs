using System;
using UnityEngine;
using System.Collections;

namespace Pool
{
	/// <summary>
	/// Базовый класс для всех объектов, которые планируется использовать с универсальным пулом.
	/// На объекте в ресурсах должно быть не более одного подобного компонента.
	/// </summary>
	public abstract class PoolObject : MonoBehaviour
	{
        private IPool pool;

		public void SetReturnPool(IPool pool)
		{
		    this.pool = pool;
		}

        /// <summary>
        /// Происходит, когда объект повторно взят из пула (не только что инстанцирован)
        /// </summary>
        public virtual void OnGetFromPool() { }

    	public virtual void ReturnObject()
		{
            gameObject.SetActive(false);
            pool.AcceptObject(this);
		}

	    private void OnDestroy()
	    {
	        pool.OnPoolObjectDestroy(this);
	    }

        public virtual float StealPriority { get { return 0f; } }
	}
}
