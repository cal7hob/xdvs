using UnityEngine;
using System.Collections;

namespace InterfaceExtensions
{
    public class TextLabel : InterfaceElementBase
    {
        public tk2dTextMesh textMesh;

        private void Awake()
        {
            if (!textMesh)
                textMesh = gameObject.GetComponent<tk2dTextMesh>();
        }

        public override Color GetColor()
        {
            return textMesh ? textMesh.color : Color.white;
        }

        public override void SetColor(Color color)
        {
            if(textMesh)
                textMesh.color = color;
        }

        public override string GetText()
        {
            return textMesh ? textMesh.text : "";
        }

        public override void SetText(string text)
        {
            if (textMesh)
                textMesh.text = text;
        }
    }
}
