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

    public static bool SafeSetSprite(this tk2dBaseSprite source, string spriteName, string fallbackSpriteName = null)
    {
        if (string.IsNullOrEmpty(spriteName) && !string.IsNullOrEmpty(fallbackSpriteName))
            return source.SafeSetSprite(fallbackSpriteName);

        return !string.IsNullOrEmpty(spriteName) && source.SetSprite(spriteName);
    }

    /// <summary>
    /// У спрайта должен быть установлен скейл 2, т.к. оперируем с размером спрайта в 1x атласе.
    /// На вход даем спрайт с максимальной допустимой областью спрайта.
    /// Смотрим пропорции в атласе и меняем размер текущего спрайта, чтоб он влез в текущую область
    /// Определяем максимально возможную область в нужных пропорциях. Если не нужно растягивать ( stretchToMax == false) и
    /// спрайт меньше максимальной области - устанавливаем размер из атласа
    /// </summary>
    public static void ResizeSlicedSpriteAccordingToTextureProportions(this tk2dSlicedSprite sprite, bool stretchToMax = false)
    {
        Vector2 maxSize = sprite.dimensions;
        Vector2 spriteSizeInAtlas = sprite.CurrentSprite.GetUntrimmedBounds().size;//size of 1x texture

        //Debug.LogErrorFormat("spriteSizeInAtlas = {0}:{1}", spriteSizeInAtlas.x, spriteSizeInAtlas.y);

        float heightToWidth = spriteSizeInAtlas.y / spriteSizeInAtlas.x;
        Vector2 bigProportionalSize;
        if (maxSize.x > maxSize.y)
            bigProportionalSize = new Vector2(maxSize.x, maxSize.x * heightToWidth);
        else
            bigProportionalSize = new Vector2(maxSize.y / heightToWidth, maxSize.y);

        Vector2 maxAllowedProportionalSize;
        if (bigProportionalSize.x > maxSize.x)
            maxAllowedProportionalSize = new Vector2(maxSize.x, maxSize.x * heightToWidth);
        else if (bigProportionalSize.y > maxSize.y)
            maxAllowedProportionalSize = new Vector2(maxSize.y / heightToWidth, maxSize.y);
        else
            maxAllowedProportionalSize = bigProportionalSize;

        if (!stretchToMax && spriteSizeInAtlas.x < maxAllowedProportionalSize.x && spriteSizeInAtlas.y < maxAllowedProportionalSize.y)//Если спрайт меньше максимальной области и не нужно растягивать
            sprite.dimensions = spriteSizeInAtlas;
        else
            sprite.dimensions = maxAllowedProportionalSize;
    }

    public static void ResizeSlicedSpriteAccordingToTextureProportions(this tk2dSlicedSprite sprite, Vector2 maxSize, bool stretchToMax = false)
    {
        sprite.dimensions = maxSize;
        ResizeSlicedSpriteAccordingToTextureProportions(sprite, stretchToMax);
    }

    public static Rect GetRect(this tk2dUILayout layout)
    {
        if (!layout || GameData.CurSceneGuiCamera == null)
        {
            return Rect.zero;
        }

        return new Rect()
        {
            xMin = GameData.CurSceneGuiCamera.WorldToScreenPoint(layout.GetMinBounds()).x,
            yMin = GameData.CurSceneGuiCamera.WorldToScreenPoint(layout.GetMinBounds()).y,
            xMax = GameData.CurSceneGuiCamera.WorldToScreenPoint(layout.GetMaxBounds()).x,
            yMax = GameData.CurSceneGuiCamera.WorldToScreenPoint(layout.GetMaxBounds()).y,
        };
    }
}
