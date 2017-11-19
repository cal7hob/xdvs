using UnityEngine;

public static class tk2dBaseSpriteExtensions
{
    public static void SetAlpha(this tk2dBaseSprite source, float value)
    {
        source.color
            = new Color(
                r:  source.color.r,
                g:  source.color.g,
                b:  source.color.b,
                a:  value);
    }

    public static void SetAlpha(this tk2dTextMesh source, float value)
    {
        source.color
            = new Color(
                r:  source.color.r,
                g:  source.color.g,
                b:  source.color.b,
                a:  value);
    }
}
