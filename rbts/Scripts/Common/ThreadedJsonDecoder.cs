using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Http
{
#if !UNITY_WSA
    public class ThreadedJsonDecoder : MonoBehaviour
    {

        public static ThreadedJsonDecoder Instance ()
        {
            if (null == m_instance) {
                GameObject o = new GameObject ();
                o.name = "ThreadedJsonDecoder";
                o.transform.position = Vector3.zero;

                o.AddComponent<ThreadedJsonDecoder> ();
            }
            return m_instance;
        }

        private static ThreadedJsonDecoder m_instance;

        void Awake ()
        {
            DontDestroyOnLoad (gameObject);
            m_instance = this;
        }

        static public void Decode (string json, Action<object, string> callback)
        {
            Instance ().StartCoroutine (Instance ().DecodingCoroutine (json, callback));
        }


        private IEnumerator DecodingCoroutine (string json, Action<object, string> callback)
        {
            var parameters = new Dictionary<string, object> (3);
            parameters["json"] = json;
            var worker = new Thread (Decoder);
            worker.Start (parameters);
            while (worker.IsAlive)
                yield return new WaitForSeconds (0.1f);
            callback (parameters["data"], parameters["error"] as string);
        }



        static private void Decoder (object parameters)
        {
            var dict = parameters as Dictionary<string, object>;
            string json = dict["json"] as string;
            object data;
            try {
                data = Facebook.MiniJSON.Json.Deserialize (json);
                dict["data"] = data;
                dict["error"] = null;
            }
            catch (Exception e) {
                dict["data"] = null;
                dict["error"] = e.Message;
            }
        }
    }
#endif
}