using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Assets.Scripts.GUI.Layouts
{
    public abstract class Layout : MonoBehaviour
    {
        public abstract class AlignItem: IAlignerItem
        {
            public Renderer objectToAlign;
            public GameObject GetGameObject()
            {
                return objectToAlign.gameObject;
            }
        }

        public Renderer alignOn;

        // Не убирать! иначе возникает видимое перемещение объектов. Илья 31.08.2015
        void Start()
        {
            Align();
        }

        void OnEnable()
        {
            Messenger.Subscribe(EventId.OnLanguageChange, OnLanguageChanged, 4);

            // Нужно было из-за бага в Unity, сейчас работает без ожидания конца кадра. (Баг никуда не делся)
            StartCoroutine(AlignCoroutine());
            //Align();
        }

        private IEnumerator AlignCoroutine()
        {
            yield return new WaitForEndOfFrame();
            Align();
        }

        void OnDisable()
        {
            Messenger.Unsubscribe(EventId.OnLanguageChange, OnLanguageChanged);
        }

        void OnLanguageChanged(EventId evId, EventInfo ev)
        {
            Align();
        }

        public abstract void Align();
        public abstract void SnapTo(float snapPos, Renderer obj);
    }
}
