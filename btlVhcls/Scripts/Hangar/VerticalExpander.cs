using UnityEngine;

public class VerticalExpander : MonoBehaviour
{
    [SerializeField] protected tk2dUILayout mainLayout;
    [SerializeField] protected Renderer bottomSpriteRenderer;
    [Header("Спрайты, dimensions.y которых округляется после растягивания Layout-ом")]
    [SerializeField] protected tk2dSlicedSprite[] spritesToExpand;

    private int m_lastCamHeight;

    void VerticalAlign()
    {
        if (GameData.CurSceneTk2dGuiCamera == null)
            return;

        float layoutBottomPosition = bottomSpriteRenderer == null ? 0 : bottomSpriteRenderer.bounds.size.y;

        var delta = (layoutBottomPosition + GameData.CurSceneTk2dGuiCamera.ScreenExtents.yMin) - mainLayout.GetMinBounds().y;
        //Debug.LogErrorFormat("CurrentTk2dCamera.ScreenExtents.yMin = {0}, mainLayout.GetMinBounds().y = {1}, Delta = {2}", CurrentTk2dCamera.ScreenExtents.yMin, mainLayout.GetMinBounds().y, delta);
        mainLayout.Reshape(new Vector3(0, delta, 0), Vector3.zero, true);

        if(spritesToExpand != null)
            for (int i = 0; i < spritesToExpand.Length; i++)
                if (spritesToExpand[i] != null)
                    spritesToExpand[i].dimensions = new Vector2(spritesToExpand[i].dimensions.x, Mathf.RoundToInt(spritesToExpand[i].dimensions.y));
    }

    protected virtual void LateUpdate()
    {
        if (!gameObject.activeInHierarchy || GameData.CurSceneTk2dGuiCamera == null)
            return;

        if((int)GameData.CurSceneTk2dGuiCamera.ScreenExtents.yMin != m_lastCamHeight)
        {
            m_lastCamHeight = (int)GameData.CurSceneTk2dGuiCamera.ScreenExtents.yMin;
            VerticalAlign();
        }
    }
}
