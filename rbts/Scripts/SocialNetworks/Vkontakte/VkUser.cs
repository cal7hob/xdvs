
using System;
namespace Vkontakte
{
    public class VkUser
    {
        public string Id { get; private set; }
        public string FirstName { get; private set; }
        public string LastName { get; private set; }
        public string AvatarUrl { get; private set; }
        
        public VkUser() { }
        public VkUser(string id, string firstName, string lastName, string avatarUrl)
        {
            Id = id;
            FirstName = firstName;
            LastName = lastName;
            AvatarUrl = avatarUrl;
        }
    }
}


