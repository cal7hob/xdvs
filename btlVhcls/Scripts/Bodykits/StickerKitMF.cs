using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class StickerKitMF
{
    private int id;
    private readonly List<Transform> transforms = new List<Transform>(); 

    public static List<StickerKitMF> ParseList(Transform rootTransform)
    {
        List<StickerKitMF> result = new List<StickerKitMF>();
        List<Transform> allChildren = rootTransform.GetAllChildrenRecursively();

        foreach (Transform child in allChildren)
        {
            int id;
            string idString = Regex.Match(child.name, @"[Ss]tickers?_(\d+)$").Groups[1].ToString();

            if (int.TryParse(idString, out id))
                AddSticker(result, id, child);
        }

        return result;
    }

    private static void AddSticker(List<StickerKitMF> stickerKits, int id, Transform sticker)
    {
        foreach (StickerKitMF stickerKit in stickerKits)
        {
            if (stickerKit.id == id)
            {
                stickerKit.transforms.Add(sticker);
                return;
            }
        }

        StickerKitMF newStickerKit = new StickerKitMF();

        newStickerKit.id = id;
        newStickerKit.transforms.Add(sticker);

        stickerKits.Add(newStickerKit);
    }

    public void TryActivate(Decal decal)
    {
        foreach (Transform transform in transforms)
            transform.gameObject.SetActive(decal != null && decal.id == id);
    }

    public void TryActivate(int stickerId)
    {
        foreach (Transform transform in transforms)
            transform.gameObject.SetActive(stickerId != 0 && stickerId == id);
    }
}
