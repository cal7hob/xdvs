using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SocialNetworks.Avatars;

namespace SocialNetworks.Avatars
{
    [ExecuteInEditMode]
    public class SpriteAvatar : MonoBehaviour {

        public SocialPlatform platform;
        public string uid;

        private IAvatarRecord avatarRec; // ќбъ€влена дл€ удержани€ ссылки на объект аватара
        private bool isDestroyed = false;

        tk2dBaseSprite m_sprite;
        tk2dBaseSprite Sprite {
            get {
                if (m_sprite == null) {
                    m_sprite = GetComponent<tk2dBaseSprite> ();
                    if (m_sprite == null) {
                        Debug.Log("SpriteAvatar - Missing sprite object. Creating.");
                        m_sprite = gameObject.AddComponent<tk2dSprite> ();
                    }
                }
                return m_sprite;
            }
        }

        protected void Awake ()
        {
            //var c = Sprite.Collection;
        }

        protected void OnDestroy() {
            isDestroyed = true;
        }

        protected void Start ()
        {
            LoadAvatar ();
        }

        public void LoadAvatar (SocialPlatform platform, string uid)
        {
            this.platform = platform;
            this.uid = uid;
            LoadAvatar ();
        }

        public void LoadAvatar ()
        {
            if ( (platform != SocialPlatform.Undefined) && (!string.IsNullOrEmpty(uid)) ) {
                Registry.Instance.GetAvatar (platform, uid,  (bool res, IAvatarRecord rec) => {
                    if (!res) {
                        return;
                    }

                    if (isDestroyed) {
                        return;
                    }

                    avatarRec = rec;

                    Sprite.Collection = avatarRec.Collection;
                    Sprite.SetSprite (avatarRec.Index);
                });
            }
        }

    }
}
