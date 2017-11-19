using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Vkontakte
{
	public class VkApiGroups : VkApiBase
	{
        public VkRequest isMember(Dictionary<string, string> parameters)
        {
            return prepareRequest("isMember", parameters);
        }
        
        public VkRequest join(Dictionary<string, string> parameters)
        {
            return prepareRequest("join", parameters);
        }
        
        public VkRequest getById(Dictionary<string, string> parameters)
        {
            return prepareRequest("getById", parameters);
        }

        protected override string getMethodsGroup ()
        {
            return "groups";
        }
	}

}