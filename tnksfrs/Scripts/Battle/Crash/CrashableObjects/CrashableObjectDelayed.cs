using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XD
{
    public interface ICrashableObject : ISender
    {

    }

    public class CrashableObjectDelayed : MonoBehaviour, ISubscriber
    {
        [SerializeField]
        private float       delay = 1f;
        [SerializeField]
        private Transform[] disableObjects = null;
        [SerializeField]
        private Transform[] enableObjects = null;

        private Coroutine   enableRoutine = null;

        #region ISubscriber       
        public string Description
        {
            get
            {
                return "[CrashableObjectBase] " + name;
            }

            set
            {
                name = value;
            }
        }

        public void Reaction(Message message, params object[] parameters)
        {
            switch (message)
            {
                case Message.ObjectCrashed:
                    if (enableRoutine == null)
                    {
                        enableRoutine = StaticType.RoutineManager.Instance<IRoutineManager>().StartStaticRoutine(this, EnableRoutine(delay, disableObjects, enableObjects));
                    }
                    break;
            }
        }
        #endregion

        private void Start()
        {
            GetComponent<ICrashableObject>().AddSubscriber(this);
        }

        private void OnDestroy()
        {
            if (enableRoutine != null && StaticType.RoutineManager.Instance<IRoutineManager>() != null)
            {
                StaticType.RoutineManager.Instance<IRoutineManager>().StopStaticRoutine(enableRoutine);
            }
        }

        private IEnumerator EnableRoutine(float t, Transform[] disableObjects, Transform[] enableObjects)
        {
            yield return new WaitForSeconds(t);
            for (int i = 0; i < disableObjects.Length; i++)
            {
                disableObjects[i].gameObject.SetActive(false);
            }

            for (int i = 0; i < enableObjects.Length; i++)
            {
                enableObjects[i].gameObject.SetActive(true);
                enableObjects[i].SetParent(null);
            }
        }
    }
}