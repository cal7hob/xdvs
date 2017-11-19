using UnityEngine;
using System.Collections;
namespace Vkontakte
{
    public class VkApi 
    {
        public static VkApiUsers users()
        {
            return new VkApiUsers();
        }

        public static VkApiFriends friends()
        {
            return new VkApiFriends();
        }
        
        public static VkApiGroups groups()
        {
            return new VkApiGroups();
        }
        
        public static VkApiApps apps()
        {
            return new VkApiApps();
        }
    }
}