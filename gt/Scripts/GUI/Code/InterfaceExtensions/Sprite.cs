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
    }
}
