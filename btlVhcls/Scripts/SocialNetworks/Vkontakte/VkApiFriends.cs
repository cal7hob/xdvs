using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Vkontakte
{
	public class VkApiFriends : VkApiBase
	{
        
        public VkRequest getAppUsers(Dictionary<string, string> parameters)
        {
            return prepareRequest("getAppUsers", parameters);
        }
        
        public VkRequest getOnline(Dictionary<string, string> parameters)
        {
            return prepareRequest("getOnline", parameters);
        }

        protected override string getMethodsGroup ()
        {
            return "friends";
        }
	}

}