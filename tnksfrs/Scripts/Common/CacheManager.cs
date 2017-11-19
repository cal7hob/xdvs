using System;
using System.Collections.Generic;
using UnityEngine;

public static class CacheManager
{
    private const int DEFAULT_STORAGE_DURATION = 24;
    private const string EXPIRATION_DATA_KEY = "expirationData";

    /// <summary>
    /// Добавление данных на временное хранение в UnityEngine.PlayerPrefs.
    /// </summary>
    /// <param name="key">Ключ.</param>
    /// <param name="data">Массив байтов.</param>
    /// <param name="storageDuration">Срок хранения кэша (в часах).</param>
    public static void Add(string key, byte[] data, int storageDuration = DEFAULT_STORAGE_DURATION)
    {
        if (PlayerPrefs.HasKey(key))
        {
            Debug.LogErrorFormat(
                "CacheManager tries to overwrite existing PlayerPrefs key \"{0}\"! "
                    + "You may have to fix duplicate keys, or CacheManager.Check() key before adding!",
                key);

            return;
        }

        try
        {
            PlayerPrefs.SetString(key, Convert.ToBase64String(data));

            Dictionary<string, object> expirationData = new Dictionary<string, object>();

            JsonPrefs expirationDataJson = PlayerPrefs.HasKey(EXPIRATION_DATA_KEY)
                ? new JsonPrefs(PlayerPrefs.GetString(EXPIRATION_DATA_KEY))
                : new JsonPrefs(new object());

            foreach (string expirationDataKey in expirationDataJson.ChildKeys())
                expirationData.Add(expirationDataKey, expirationDataJson.ValueDouble(expirationDataKey));

            expirationData[key] = GameData.DateTimeToUnixTimeStamp(DateTime.Now) + (storageDuration * 3600);

            PlayerPrefs.SetString(EXPIRATION_DATA_KEY, new JsonPrefs(expirationData).ToString());

            PlayerPrefs.Save();
        }
        catch (PlayerPrefsException err)
        {
            Debug.LogError("Exceeded storage limit (only usually issue on the Web Player) error: " + err);
        }
    }

    /// <summary>
    /// Удаление из кэша данных, срок хранения которых истёк.
    /// </summary>
    public static void ClearOutdatedCache()
    {
        if (!PlayerPrefs.HasKey(EXPIRATION_DATA_KEY))
            return;

        JsonPrefs expirationDataJson = new JsonPrefs(PlayerPrefs.GetString(EXPIRATION_DATA_KEY));

        foreach (string expirationDataKey in expirationDataJson.ChildKeys())
            if (!Check(expirationDataKey))
                Delete(expirationDataKey);
    }

    /// <summary>
    /// Принудительное удаление данных из кэша.
    /// </summary>
    /// <param name="key">Ключ.</param>
    public static void Delete(string key)
    {
        PlayerPrefs.DeleteKey(key);

        Dictionary<string, object> expirationData = new Dictionary<string, object>();

        JsonPrefs expirationDataJson = new JsonPrefs(PlayerPrefs.GetString(EXPIRATION_DATA_KEY));

        foreach (string expirationDataKey in expirationDataJson.ChildKeys())
            if(expirationDataKey != key)
                expirationData.Add(
                    expirationDataKey,
                    expirationDataJson.ValueDouble(expirationDataKey));

        PlayerPrefs.SetString(EXPIRATION_DATA_KEY, new JsonPrefs(expirationData).ToString());

        PlayerPrefs.Save();
    }

    /// <summary>
    /// Проверка на наличие и актуальность кэша по ключу.
    /// </summary>
    /// <param name="key">Ключ.</param>
    /// <returns>Возвращает false, если кэш по ключу отсутствует или просрочен.</returns>
    public static bool Check(string key)
    {
        if (!PlayerPrefs.HasKey(EXPIRATION_DATA_KEY))
        {
            if(PlayerPrefs.HasKey(key))
                PlayerPrefs.DeleteKey(key);

            return false;
        } 

        JsonPrefs expirationDataJson = new JsonPrefs(PlayerPrefs.GetString(EXPIRATION_DATA_KEY));

        if (expirationDataJson.ValueDouble(key) > GameData.DateTimeToUnixTimeStamp(DateTime.Now))
            return true;

        Delete(key);

        return false;
    }

    /// <summary>
    /// Получение данных из временного хранилища в UnityEngine.PlayerPrefs.
    /// </summary>
    /// <param name="key">Ключ.</param>
    /// <returns>Данные в массиве байтов.</returns>
    public static byte[] Get(string key)
    {
        return Convert.FromBase64String(PlayerPrefs.GetString(key));
    }
}
