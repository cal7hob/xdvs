using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;


namespace Vkontakte
{
    public class VkRequest
    {
        private string methodName;
        private Dictionary<string, string> methodParameters;
        const int RetriesCount = 5;

        public VkRequest(string method, Dictionary<string,string> parameters)
        {
            methodName = method;
            methodParameters = parameters;
        }

        WWW CreateWWW ()
        {
            string url = string.Format("https://api.vk.com/method/{0}",methodName);
            var form = new WWWForm();
            foreach (var parameter in methodParameters) 
            {
                form.AddField(parameter.Key, parameter.Value);
            }
            form.AddField ("access_token", VKSdk.getAccessToken());
            var www = new WWW (url, form);
            return www;
        }

        public IEnumerator Start(Action<string> onComplete)
        {
            int count = RetriesCount;
            while (count-- > 0)
            {
                var www = CreateWWW();
                yield return www;
                if (string.IsNullOrEmpty(www.error))
                {
                    onComplete(www.text);
                    break;
                }
                yield return new WaitForSeconds(1.0f);
            }
        }
    }

}