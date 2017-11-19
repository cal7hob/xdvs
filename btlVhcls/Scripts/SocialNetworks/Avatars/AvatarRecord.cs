using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SocialNetworks.Avatars
{
    public interface IAvatarRecord
    {
        tk2dSpriteCollectionData Collection { get; }
        int Index { get; }
        string Uid { get; }
        SocialPlatform Platform { get; }
    }

    class AvatarRecord : IAvatarRecord
    {
        public tk2dSpriteCollectionData Collection { get; set; }
        public int Index { get; set; }
        public string Uid { get; set; }
        public SocialPlatform Platform { get; set; }
    }
}
