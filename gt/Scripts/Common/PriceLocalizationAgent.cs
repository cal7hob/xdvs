using System;
using UnityEngine;
using System.Collections;

public class PriceLocalizationAgent : MonoBehaviour
{

    public static string GetLocalizedString(string itemId, tk2dTextMesh textMesh, string lblName = null)
    {
        //  Debug.Log(itemId + " itemid" + textMesh.text + " txt" + lblName + " lblname");
        string key = !string.IsNullOrEmpty(lblName) ? lblName : textMesh.name;
        // Debug.LogError(key);
        string result = Localizer.ContainsKey(key) ? Localizer.GetText(key) : "";
        // Debug.Log("result " + result);

        if (SocialSettings.IsWebPlatform && !(textMesh.name.Contains("lblVipOfferTime")))
        {
            var item = SocialSettings.GetSocialService().GetPriceById(itemId);
            result = item.PriceString;
        }
        else if (IapManager.IsInitialized())
        {
            try
            {
                //   Debug.Log("iap init , try " );
                var item = IapManager.StoreController.products.WithID(itemId);
                if ((item != null) && (!string.IsNullOrEmpty(item.metadata.localizedPriceString))
                    && !item.metadata.localizedPrice.Equals(0.0m)
                    && !item.metadata.localizedPrice.Equals(0.01m)) // Пропускаем дефолтную цену если попалась, т.к. у нас таких нет
                {
                    //   Debug.LogFormat(" itemId = {0}, localizedTitle = {1}, localizedDescription = {2}, localizedPriceString = {3}, isoCurrencyCode = {4}, storeSpecificId = {5}, localizedPrice = {6}",
                    //  itemId, item.metadata.localizedTitle, item.metadata.localizedDescription, item.metadata.localizedPriceString, item.metadata.isoCurrencyCode, item.definition.storeSpecificId,
                    //  item.metadata.localizedPrice);
                    result = item.metadata.localizedPriceString;
#if UNITY_WSA
                    if (result.Contains ("₽"))//Костыль для рублей - из-за ошибки в унибиллере
                    {
                        //DT3.LogError("contains rouble symbol!");
                        result = result.Replace ("₽", "  RUB");//2 пробела - потому что в тунварсе один пробел выглядит как и нет его
                    }
#endif
                    if (!IsAllCharsInFont(textMesh.font, result))
                    {
                        result = item.metadata.isoCurrencyCode + " " + item.metadata.localizedPrice.ToString("N2", GameData.instance.cultureInfo.NumberFormat);
                        //Debug.LogError("Format price in system region info "+result);
                    }
                }
            }
            catch
            {

            }
        }
        //  Debug.Log("final result " + result);
        return result;
    }

    private static bool IsAllCharsInFont(tk2dFontData font, string str)
    {
        foreach (var ch in str)
        {
            if (!font.charDict.ContainsKey((int)ch))
            {
                return false;
            }
        }
        return true;
    }
}
