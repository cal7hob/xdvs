using UnityEngine;
using System.Collections;
using System.Collections.Generic;
namespace Vkontakte
{
    public abstract class VkApiBase
    {
        protected abstract string getMethodsGroup();
        protected VkRequest prepareRequest(string methodName, Dictionary<string, string> parameters)
        {
            return new VkRequest(string.Format("{0}.{1}", getMethodsGroup(), methodName), parameters);
        }
    }
}
