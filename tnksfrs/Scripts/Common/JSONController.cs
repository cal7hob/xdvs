using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class JSONController
{
    //Путь к каталогу куда сохраняются JSON файлы
    private static string folderJSONData = Application.persistentDataPath + Path.DirectorySeparatorChar +"Saves" + Path.DirectorySeparatorChar;

    /// <summary>
    /// Сохраняем расходку в JSON файл
    /// </summary>
    /// <param name="tankID"></param>
    /// <param name="value"></param>
    public static void Save(int nameFile, string value)
    {

        if (!Directory.Exists(folderJSONData))
        {
            //Если каталога нет
            DirectoryInfo directoryInfo = Directory.CreateDirectory(folderJSONData);
            if (!directoryInfo.Exists)
            {
                //Debug.Log("Create dir fail Path: " + folderJSONData);
            }
            else
            {
                //Debug.Log("Create dir fail Path: " + folderJSONData);
            }
        }
        File.WriteAllText(folderJSONData + nameFile.ToString() +".txt", value);
    }

    /// <summary>
    /// Удаляет целиком каталог с JSON файлами.
    /// </summary>
    public static void DeleteJsonFolder()
    {
        if (Directory.Exists(folderJSONData))
        {
            //Если каталог найден удаляем его
            Directory.Delete(folderJSONData, true);
        }
    }
}
