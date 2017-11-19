using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using XD;


[ExecuteInEditMode]
[CustomEditor(typeof(ShellItemHolder))]
public class ShellItemHolderEditor : Editor
{
    private ShellItemHolder db;
    private ShellItem shellItem;
    private GameObject prefab;

    [SerializeField]
    private List<bool> visability;


    private Vector2 startDrawPos;
    private Vector2 scrollPosition;

    private float singleBlockHigh = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
    private float lineHeight = EditorGUIUtility.singleLineHeight;


    private Vector2 blockSize = new Vector2(400, 100);
    private Vector2 smallButtonSize = new Vector2(30, EditorGUIUtility.singleLineHeight);

    private float defLabelWidth = 100f;
    private float defTextFieldWidth = 200f;


    void Awake()
    {
        db = (ShellItemHolder)target;

        visability = new List<bool>(db.content.Count);
        for (int i = 0; i < visability.Count; i++)
        {
            visability[i] = false;
        }
        RefreshDict();
    }

    void RefreshDict() 
    {
        if (db.dictById == null) 
        {
            db.dictById = new Dictionary<int, ShellItem>();
        }

        db.dictById.Clear();
        db.names.Clear();

        for (int i = 0; i < db.content.Count; i++) 
        {
            db.names.Add(db.content[i].name);
            db.dictById.Add(db.content[i].id, db.content[i]);
            EditorUtility.SetDirty(db);
        }
 
    }

    public override void OnInspectorGUI()
    {
        scrollPosition = GUI.BeginScrollView(new Rect(20, 20, EditorGUIUtility.currentViewWidth - 20, Screen.height - 150), scrollPosition, new Rect(0, 10, 400, startDrawPos.y));
        if (db.content.Count < 1)
        {
            AddShellItem();
        }
       
        startDrawPos = new Vector2(20,60);

        DisplayShellList(ref startDrawPos);

        if (GUI.Button(new Rect(startDrawPos, new Vector2(200, 40)), "AddShellItem")) 
        {
            AddShellItem();
        }
        startDrawPos.y += 50;
        
        GUI.EndScrollView();
    }

    private void AddShellItem() 
    {
        if (db.freeIds.Count > 0)
        {
            db.content.Add(new ShellItem(db.freeIds[0]));
            db.freeIds.RemoveAt(0);
        }
        else
        {
            db.content.Add(new ShellItem(db.maxId));
            db.maxId++;
        }
        EditorUtility.SetDirty(target);
    }

    private void DisplayShellList(ref Vector2 startDrawPos)
    {
        if (db.content.Count == 0)
        {
            return;
        }
        if (db.content.Count != db.dictById.Count || db.content.Count != db.names.Count) 
        {
            RefreshDict();
        }
        //Debug.Log(" Количество снаярдов на момент начала отрисовки добавления" + db.content.Count);
        for (int i = 0; i < db.content.Count; i++)
        {
           
            if(db.content.Count <= i)
            {
                break;
            } 
            if (visability.Count < db.content.Count) 
            {
                visability.Add(false);
            }
            
            Vector2 localPos = Vector2.zero;
            shellItem = db.content[i];

            if(shellItem.id < 1)
            {
                shellItem.id = db.maxId;
                db.maxId++;
                EditorUtility.SetDirty(db);
            }

            visability[i] = EditorGUI.Foldout(new Rect(startDrawPos + localPos, new Vector2(defTextFieldWidth, lineHeight)), visability[i], shellItem.name);
            localPos.x += 210;
            if (GUI.Button(new Rect(startDrawPos + localPos, smallButtonSize), "UP"))
            {
                MoveItemUp(i); 
            }

            localPos.x += smallButtonSize.x + 10;
            if (GUI.Button(new Rect(startDrawPos + localPos, smallButtonSize), "DN"))
            {
               MoveItemDown(i);
            }

            localPos.x += smallButtonSize.x + 70;
            if (GUI.Button(new Rect(startDrawPos + localPos, smallButtonSize), "-"))
            {
                DeleteItem(i);
                break;
            }

            localPos.x = 0;
            startDrawPos.y += singleBlockHigh;

            if (!visability[i]) 
            {
                continue;
            }
            
            shellItem.name = GUI.TextField(new Rect(startDrawPos + localPos, new Vector2(defTextFieldWidth, lineHeight)), shellItem.name);//
            
            if(shellItem.name != db.names[i])
            {
                db.names[i] = shellItem.name;
                EditorUtility.SetDirty(db);
            }


            localPos.x = 30;
            localPos.y += singleBlockHigh;
            EditorGUI.LabelField(new Rect(startDrawPos.x + localPos.x, startDrawPos.y + localPos.y, defLabelWidth, lineHeight), "type");
            shellItem.type = (GunShellInfo.ShellType)EditorGUI.EnumPopup(new Rect(startDrawPos.x + localPos.x + defLabelWidth + 10, startDrawPos.y + localPos.y, defTextFieldWidth, lineHeight), shellItem.type);
            localPos.y += singleBlockHigh;
            EditorGUI.LabelField(new Rect(startDrawPos.x + localPos.x, startDrawPos.y + localPos.y, defLabelWidth, lineHeight), "prefab");
           // shellItem.prefab = (GameObject)EditorGUI.ObjectField(new Rect(startAreaPos.x + localPos.x + defLabelWidth + 10, startAreaPos.y + localPos.y, defTextFieldWidth, lineHeight), shellItem.prefab, typeof(GameObject));
            shellItem.prefabName = GUI.TextField(new Rect(startDrawPos.x + localPos.x + defLabelWidth + 10, startDrawPos.y + localPos.y, defTextFieldWidth, lineHeight), shellItem.prefabName);//
            localPos.y += singleBlockHigh;
            EditorGUI.LabelField(new Rect(startDrawPos.x + localPos.x, startDrawPos.y + localPos.y, defLabelWidth, lineHeight), "speed");
            shellItem.speed = EditorGUI.FloatField(new Rect(startDrawPos.x + localPos.x + defLabelWidth + 10, startDrawPos.y + localPos.y, defTextFieldWidth, lineHeight), shellItem.speed);
            localPos.y += singleBlockHigh;
            EditorGUI.LabelField(new Rect(startDrawPos.x + localPos.x, startDrawPos.y + localPos.y, defLabelWidth, lineHeight), "maxDistance");
            shellItem.maxDistance = EditorGUI.FloatField(new Rect(startDrawPos.x + localPos.x + defLabelWidth + 10, startDrawPos.y + localPos.y, defTextFieldWidth, lineHeight), shellItem.maxDistance);
            localPos.y += singleBlockHigh;
            EditorGUI.LabelField(new Rect(startDrawPos.x + localPos.x, startDrawPos.y + localPos.y, defLabelWidth, lineHeight), "radius");
            shellItem.radius = EditorGUI.FloatField(new Rect(startDrawPos.x + localPos.x + defLabelWidth + 10, startDrawPos.y + localPos.y, defTextFieldWidth, lineHeight), shellItem.radius);
            localPos.y += singleBlockHigh;

            #region ShotSound
            
            shellItem.multipleShotSound = EditorGUI.ToggleLeft(new Rect(startDrawPos.x + localPos.x, startDrawPos.y + localPos.y, defLabelWidth*2, lineHeight), "multipleShotSound", shellItem.multipleShotSound);

            if (shellItem.multipleShotSound) 
            {
                if (GUI.Button(new Rect(startDrawPos.x + localPos.x + defLabelWidth*2 + 10, startDrawPos.y + localPos.y, smallButtonSize.x, smallButtonSize.y), "+")) 
                {
                    shellItem.shotSounds.Add(new AudioClip());
                    EditorUtility.SetDirty(target);
                }

                localPos.y += singleBlockHigh;
                float xPos = 0; 
                for (int j = 0; j < shellItem.shotSounds.Count; j++ )
                {
                    xPos = startDrawPos.x + localPos.x + 20;
                    EditorGUI.LabelField(new Rect(xPos, startDrawPos.y + localPos.y, 40, lineHeight), "№"+(j+1));
                    xPos += 50;

                    shellItem.shotSounds[j] = (AudioClip)EditorGUI.ObjectField(new Rect(xPos, startDrawPos.y + localPos.y, defTextFieldWidth, lineHeight), shellItem.shotSounds[j], typeof(AudioClip));
                    xPos += defTextFieldWidth + 10;
                    if (GUI.Button(new Rect(xPos, startDrawPos.y + localPos.y, smallButtonSize.x, smallButtonSize.y), "-"))
                    {
                        shellItem.shotSounds.RemoveAt(j);
                    }
                    localPos.y += singleBlockHigh;
                }
            }
            else
            {
                localPos.y += singleBlockHigh;
                EditorGUI.LabelField(new Rect(startDrawPos.x + localPos.x + 20, startDrawPos.y + localPos.y, defLabelWidth, lineHeight), "shotSound");
                shellItem.shotSound = (AudioClip)EditorGUI.ObjectField(new Rect(startDrawPos.x + localPos.x + defLabelWidth + 10, startDrawPos.y + localPos.y, defTextFieldWidth, lineHeight), shellItem.shotSound, typeof(AudioClip));
                localPos.y += singleBlockHigh;
            }
            #endregion

            #region BlowSound

            shellItem.multipleBlowSound = EditorGUI.ToggleLeft(new Rect(startDrawPos.x + localPos.x, startDrawPos.y + localPos.y, defLabelWidth*2, lineHeight), "multipleBlowSound", shellItem.multipleBlowSound);
            
            if (shellItem.multipleBlowSound)
            {
                if (GUI.Button(new Rect(startDrawPos.x + localPos.x + defLabelWidth*2 + 10, startDrawPos.y + localPos.y, smallButtonSize.x, smallButtonSize.y), "+"))
                {
                    shellItem.blowSounds.Add(new AudioClip());
                    EditorUtility.SetDirty(target);
                }

                localPos.y += singleBlockHigh;
                float xPos = 0;
                for (int j = 0; j < shellItem.blowSounds.Count; j++)
                {
                    xPos = startDrawPos.x + localPos.x + 20;
                    EditorGUI.LabelField(new Rect(xPos, startDrawPos.y + localPos.y, 40, lineHeight), "№" + (j + 1));
                    xPos += 50;

                    shellItem.blowSounds[j] = (AudioClip)EditorGUI.ObjectField(new Rect(xPos, startDrawPos.y + localPos.y, defTextFieldWidth, lineHeight), shellItem.blowSounds[j], typeof(AudioClip));
                    xPos += defTextFieldWidth + 10;
                    if (GUI.Button(new Rect(xPos, startDrawPos.y + localPos.y, smallButtonSize.x, smallButtonSize.y), "-"))
                    {
                        shellItem.blowSounds.RemoveAt(j);
                    }
                    localPos.y += singleBlockHigh;
                }
            }
            else
            {
                localPos.y += singleBlockHigh;
                EditorGUI.LabelField(new Rect(startDrawPos.x + localPos.x + 20, startDrawPos.y + localPos.y, defLabelWidth, lineHeight), "blowSound");
                shellItem.blowSound = (AudioClip)EditorGUI.ObjectField(new Rect(startDrawPos.x + localPos.x + defLabelWidth + 10, startDrawPos.y + localPos.y, defTextFieldWidth, lineHeight), shellItem.blowSound, typeof(AudioClip));
                localPos.y += singleBlockHigh;
            }
            #endregion

            EditorGUI.LabelField(new Rect(startDrawPos.x + localPos.x, startDrawPos.y + localPos.y, defLabelWidth, lineHeight), "hitPrefab");
            shellItem.hitPrefab = (GameObject)EditorGUI.ObjectField(new Rect(startDrawPos.x + localPos.x + defLabelWidth + 10, startDrawPos.y + localPos.y, defTextFieldWidth, lineHeight), shellItem.hitPrefab, typeof(GameObject));
            localPos.y += singleBlockHigh;
            EditorGUI.LabelField(new Rect(startDrawPos.x + localPos.x, startDrawPos.y + localPos.y, defLabelWidth, lineHeight), "terrainHitPrefab");
            shellItem.terrainHitPrefab = (GameObject)EditorGUI.ObjectField(new Rect(startDrawPos.x + localPos.x + defLabelWidth + 10, startDrawPos.y + localPos.y, defTextFieldWidth, lineHeight), shellItem.terrainHitPrefab, typeof(GameObject));
            localPos.y += singleBlockHigh;
            shellItem.continuousFire = EditorGUI.Toggle(new Rect(startDrawPos.x + localPos.x, startDrawPos.y + localPos.y, defLabelWidth * 2, lineHeight), "continuousFire", shellItem.continuousFire);
            localPos.y += singleBlockHigh;
            shellItem.isPrimary = EditorGUI.Toggle(new Rect(startDrawPos.x + localPos.x, startDrawPos.y + localPos.y, defLabelWidth * 2, lineHeight), "isPrimary", shellItem.isPrimary);
            localPos.y += singleBlockHigh;

            startDrawPos.y += (localPos.y + singleBlockHigh);
            
        }
        startDrawPos.y += singleBlockHigh;
        EditorUtility.SetDirty(target);
    }

    private void MoveItemDown(int i) 
    {
        if (i < db.content.Count)
        {
            bool vis = visability[i];
            visability.RemoveAt(i);
            visability.Insert(i + 1, vis);

            db.content.RemoveAt(i);
            db.content.Insert(i + 1, shellItem);
            db.names.RemoveAt(i);
            db.names.Insert(i + 1, shellItem.name);
        }
    }

    private void MoveItemUp(int i) 
    {
        if (i > 0)
        {
            bool vis = visability[i];
            visability.RemoveAt(i);
            visability.Insert(i - 1, vis);

            db.content.RemoveAt(i);
            db.content.Insert(i - 1, shellItem);
            db.names.RemoveAt(i);
            db.names.Insert(i - 1, shellItem.name);
        }
    }

    private void DeleteItem(int i) 
    {
        db.freeIds.Add(shellItem.id);
        visability.RemoveAt(i);        
        db.dictById.Remove(shellItem.id);
        db.content.Remove(shellItem);
        db.names.RemoveAt(i);
        EditorUtility.SetDirty(target);
    }

}

public class CreateShellItemAsset
{
    [MenuItem("Custom/Create shell items list")]
    public static void CreateShellItemDB()
    {
        List<ShellItem> content = new List<ShellItem>();
        ShellItemHolder asset = new ShellItemHolder(content);  //scriptable object 
        AssetDatabase.CreateAsset(asset, "Assets/Resources/Consumables/NewShellItemsDatabase.asset");
        AssetDatabase.SaveAssets();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
    }
}
