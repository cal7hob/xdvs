using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SocialNetworks.Avatars
{
    class RegistryRecord
    {
        public int index;
        public string name;

        public int x;
        public int y;

        public bool loaded = false;
        public string uid = null;
        public SocialPlatform platform = SocialPlatform.Undefined;

        WeakReference extRec;

        public Queue<Action<bool, IAvatarRecord>> subscribers = new Queue<Action<bool, IAvatarRecord>>();

        public RegistryRecord()
        {
            extRec = new WeakReference(null);
        }

        public bool IsUsed ()
        {
            return (extRec != null) && (extRec.Target != null);
        }

        public IAvatarRecord GetAvatarRecord ()
        {
            AvatarRecord rec = extRec.Target as AvatarRecord;
            if (rec == null) {
                rec = CreateRecord();
                extRec.Target = rec;
            }
            return rec;
        }

        private AvatarRecord CreateRecord()
        {
            var rec = new AvatarRecord ();
            rec.Index = index;
            rec.Uid = uid;
            rec.Platform = platform;
            return rec;
        }
    }
}
