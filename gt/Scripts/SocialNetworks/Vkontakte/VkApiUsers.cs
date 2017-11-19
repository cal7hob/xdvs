using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Vkontakte
{
	public class VkApiUsers : VkApiBase
	{        
        public VkRequest get(Dictionary<string, string> parameters)
        {
            return prepareRequest("get", parameters);
        }

        protected override string getMethodsGroup ()
        {
            return "users";
        }
	}

}