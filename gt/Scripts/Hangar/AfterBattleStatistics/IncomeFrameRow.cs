using UnityEngine;

public class IncomeFrameRow : MonoBehaviour
{
    public tk2dSprite sprite;
    public tk2dTextMesh incomeValue;
    public tk2dTextMesh explanation;

    public void Init(AfterBattleIncomeFrame.Data data)
    {
        incomeValue.text = data.val.ToString("N0", GameData.instance.cultureInfo.NumberFormat);
        if(!string.IsNullOrEmpty(data.spriteName))
            sprite.SetSprite(data.spriteName);

        explanation.gameObject.SetActive(false);
        if (!string.IsNullOrEmpty(data.explanation))
        {
            explanation.gameObject.SetActive(true);
            explanation.text = Localizer.GetText(data.explanation);
        }
    }
}
