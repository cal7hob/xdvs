using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Vkontakte
{
	public class VkApiApps : VkApiBase
	{
        public VkRequest getFriendsList(Dictionary<string, string> parameters)
        {
            return prepareRequest("getFriendsList", parameters);
        }

        public VkRequest sendRequest(Dictionary<string,string> parameters)
        {
            return prepareRequest("sendRequest", parameters);
        }

        protected override string getMethodsGroup()
        {
            return "apps";
        }
	}

}