using System;
using UnityEngine;
using System.Collections.Generic;

public class EffectPoolDispatcher : MonoBehaviour
{
	/*** class Effect declaration ***/
	public class Effect
	{
		public Transform transform = null;
		private readonly ParticleSystem particleSystem;

		public Effect(GameObject effectObject, Transform parent)
		{
			transform = (Instantiate(effectObject, -Vector3.up * 10000, Quaternion.identity) as GameObject).transform;
			particleSystem = transform.GetComponent<ParticleSystem>();
			if (!particleSystem)
				particleSystem = transform.GetComponentInChildren<ParticleSystem>();
			if(particleSystem == null)
			{
				DT.LogError(transform.gameObject, "There is no ParticleSystem component on effect");
				return;
			}

			transform.parent = GetEffectsParent();
			if(particleSystem.isPlaying)
				particleSystem.Stop();
		}
		public bool isFree
		{
			get { return !particleSystem.isPlaying; }
		}

		public GameObject GetPlayedEffect(Vector3 _position, Quaternion _rotation)
		{
			transform.position = _position;
			transform.rotation = _rotation;
			transform.parent = GetEffectsParent();
			particleSystem.Play(true);
			return transform.gameObject;
		}

		/*		public void Clear()
				{
					Destroy(transform.gameObject);
				}*/
	}
	/*** end of class Effect declaration ***/


	/*** Declaration of EffectPool class ***/
	[Serializable]
	public class EffectPool
	{
		public GameObject effectObject = null;
		public int startPoolSize = 1;
		public int maxPoolSize = 10;
		private int currentPoolSize;
		private Effect currentEffect;
		private GameObject instantiated;
		private List<Effect> effects;
		private Transform parent;

		public EffectPool(GameObject _effectObject, int _startPoolSize, int _maxPoolSize, Effect initialEffect, Transform _parent)
		{
			effectObject = _effectObject;
			startPoolSize = _startPoolSize;
			maxPoolSize = _maxPoolSize;
			currentPoolSize = 1;
			effects = new List<Effect>(maxPoolSize) {initialEffect};
			parent = _parent;
		}

		public void Init()
		{
			effects = new List<Effect>(maxPoolSize);
			for(int i = 0; i < startPoolSize; i++)
			{
				effects.Add(new Effect(effectObject, parent));
			}

			currentPoolSize = startPoolSize;
		}

		public GameObject GetEffect(Vector3 _position, Quaternion _rotation)
		{
			for(int i = 0; i < currentPoolSize; i++)
			{
				if (effects[i].isFree)
				{
					return effects[i].GetPlayedEffect(_position, _rotation);
				}
			}

			if(currentPoolSize < maxPoolSize)
			{
				effects.Add(new Effect(effectObject, parent));
				currentPoolSize++;
				return effects[currentPoolSize - 1].GetPlayedEffect(_position, _rotation);
			}

			instantiated = Instantiate(effectObject, _position, _rotation) as GameObject;
			instantiated.GetComponent<ParticleSystem>().Play();
			return instantiated;
		}

		public void Clear()
		{
			effects.Clear();
		}
	}
	/*** end of EffectPool class declaration***/

	// Class EffectPoolDispatcher
	//public EffectPool[] initEffectPools;
	private Dictionary<GameObject, EffectPool> effectPoolsDict = null;
	private static EffectPoolDispatcher singleton = null;
	

	void Awake()
	{
		if(singleton == null)
			singleton = this;
		else
		{
			DT.Log(gameObject, "There are more than one EffectPoolDispatcher on the scene");
			enabled = false;
		}
		
		effectPoolsDict = new Dictionary<GameObject, EffectPool>(/* initEffectPools.Length */);
        //foreach(EffectPool pool in initEffectPools)
        //{
        //    pool.Init();
        //    if (effectPoolsDict.ContainsKey(pool.effectObject))
        //    {
        //        DT.LogError(pool.effectObject, "Dispatcher already contains pool for object '{0}'", pool.effectObject.name);
        //        continue;
        //    }
			
        //    effectPoolsDict.Add(pool.effectObject, pool);
        //}
	}

	void OnDestroy()
	{
		if(singleton == null)
			return;

		singleton = null;
		//		currentEffectPool = null;
		foreach(EffectPool effectPoolLogic in effectPoolsDict.Values)
			effectPoolLogic.Clear();
	}

	public static GameObject GetFromPool(GameObject _effect, Vector3 _position, Quaternion _rotation, bool useEffectMover = false, Transform moverTarget = null)
	{
	    if (!singleton)
	        return null;

        if (!singleton.effectPoolsDict.ContainsKey(_effect))
		{
			singleton.effectPoolsDict.Add(_effect, new EffectPool(_effect, 1, 50, new Effect(_effect, singleton.transform), singleton.transform));
		}

		GameObject effect = singleton.effectPoolsDict[_effect].GetEffect(_position, _rotation);
		if (effect.transform.parent != singleton.transform)
			effect.transform.SetParent(singleton.transform);

		if (useEffectMover)
		{
			EffectMover mover = effect.GetComponent<EffectMover>();
			if (!mover)
			{
				Debug.LogErrorFormat(effect, "There is no EffectMover component attached to effect ({0}). Added. This will affect performance. Please fix effect prefab.", effect.name);
				mover = effect.AddComponent<EffectMover>();
			}
		    mover.SetTarget(moverTarget);
		}

		return effect;
	}

	public static Transform GetEffectsParent()
	{
            return singleton != null ? singleton.transform : null;
	}
}