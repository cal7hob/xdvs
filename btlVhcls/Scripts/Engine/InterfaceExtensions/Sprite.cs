using UnityEngine;
using System.Collections;

namespace InterfaceExtensions
{
    public class Sprite : InterfaceElementBase
    {
        public tk2dBaseSprite sprite;

        private void Awake()
        {
            if (!sprite)
                sprite = gameObject.GetComponent<tk2dBaseSprite>();
        }

        public override Color GetColor()
        {
            return sprite ? sprite.color : Color.white;
        }

        public override void SetColor(Color color)
        {
            if(sprite)
                sprite.color = color;
        }

        public override string GetText()
        {
            return sprite ? sprite.CurrentSprite.name : "";
        }

        public override void SetText(string text)
        {
            if (sprite && !string.IsNullOrEmpty(text))
                sprite.SetSprite(text);
        }

        public override Vector3 GetSize()
        {
            Vector3 size = base.GetSize();
            if (sprite is tk2dSlicedSprite)
            {
                tk2dSlicedSprite sliced = (tk2dSlicedSprite)sprite;
                size = new Vector3(sliced.dimensions.x, sliced.dimensions.y, size.z);
            }
            else
                Debug.LogErrorFormat("Unsupported Sprite Type! You must assign needed action to {0}.", sprite.GetType());
            return size;
        }

        public override void SetSize(Vector3 size)
        {
            if (sprite is tk2dSlicedSprite)
            {
                tk2dSlicedSprite sliced = (tk2dSlicedSprite)sprite;
                sliced.dimensions = new Vector2(size.x, size.y);
            }
            else
                Debug.LogErrorFormat("Unsupported Sprite Type! You must assign needed action to {0}.", sprite.GetType());
        }
    }
}
