using UnityEngine;

public class VerticalExpander : MonoBehaviour
{
    [SerializeField] protected tk2dUILayout mainLayout;
    [SerializeField] protected Renderer bottomSpriteRenderer;
    [Header("Спрайты, dimensions.y которых округляется после растягивания Layout-ом")]
    [SerializeField] protected tk2dSlicedSprite[] spritesToExpand;

    private int m_lastCamHeight;
    private tk2dCamera CurrentTk2dCamera
    {
        get
        {
            if (HangarController.Instance != null)
                return HangarController.Instance.Tk2dGuiCamera;
            if (BattleGUI.Instance != null)
                return BattleGUI.Instance.Tk2dGuiCamera;
            return null;
        }
    }

    void VerticalAlign()
    {
        if (CurrentTk2dCamera == null)
            return;

        float layoutBottomPosition = bottomSpriteRenderer == null ? 0 : bottomSpriteRenderer.bounds.size.y;

        var delta = (layoutBottomPosition + CurrentTk2dCamera.ScreenExtents.yMin) - mainLayout.GetMinBounds().y;
        //Debug.LogErrorFormat("CurrentTk2dCamera.ScreenExtents.yMin = {0}, mainLayout.GetMinBounds().y = {1}, Delta = {2}", CurrentTk2dCamera.ScreenExtents.yMin, mainLayout.GetMinBounds().y, delta);
        mainLayout.Reshape(new Vector3(0, delta, 0), Vector3.zero, true);

        if(spritesToExpand != null)
            for (int i = 0; i < spritesToExpand.Length; i++)
                if (spritesToExpand[i] != null)
                    spritesToExpand[i].dimensions = new Vector2(spritesToExpand[i].dimensions.x, Mathf.RoundToInt(spritesToExpand[i].dimensions.y));
    }

    protected virtual void LateUpdate()
    {
        if (!gameObject.activeInHierarchy || CurrentTk2dCamera == null)
            return;

        if((int)CurrentTk2dCamera.ScreenExtents.yMin != m_lastCamHeight)
        {
            m_lastCamHeight = (int)CurrentTk2dCamera.ScreenExtents.yMin;
            VerticalAlign();
        }
    }
}
