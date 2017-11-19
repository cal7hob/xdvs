using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

//[CanEditMultipleObjects]
[ExecuteInEditMode]
[CustomEditor(typeof(SoundVehicleBlender))]
public class SoundBlenderEditor : Editor
{
    public static Texture2D lineTex;
 

    private SoundVehicleBlender blender;

    private SerializedObject targetObj;
    private SerializedProperty moveForwardList;



	float lineHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
    float buttonWidth = 20f;
    float floatFieldWidth = 40f;
    float labelFieldWidth = 200f;
    float fullHeight = 0f;
    float startX;
    Vector2 contentRectSize;

    private void OnEnable()
    {
        blender = target as SoundVehicleBlender;
        targetObj = new SerializedObject(target);
    }

    float curXFreePos;
    Rect fullRect;
    Rect curLineRect;

    public override void OnInspectorGUI()
    {
        fullRect = EditorGUILayout.GetControlRect();
        curXFreePos = fullRect.x;
        curLineRect = new Rect(fullRect.x, fullRect.y, EditorGUIUtility.currentViewWidth, EditorGUIUtility.singleLineHeight);
        nextLine();
        blender.usePitch = EditorGUI.ToggleLeft(GetFixRect(labelFieldWidth), "Use Pitch", blender.usePitch);
        nextLine();
        SoundItemListShow(blender.moveForwardSounds, "MoveForwardSounds", ref blender.moveForwardColor);
        nextLine();
        EditorGUI.LabelField(GetFixRect(labelFieldWidth), "IdleSounds");
        blender.idleColor = EditorGUI.ColorField(GetFixRect(floatFieldWidth), blender.idleColor);
        nextLine();
        SoundItemShow(blender.idleSound);
        nextLine();
        nextLine();
        SoundItemListShow(blender.moveBackwardSounds, "MoveBackwardSounds", ref blender.moveBackwardColor);
        nextLine();
        fullHeight = lineHeight;
        contentRectSize.y += lineHeight;
        //Drawing
        Rect curvesRect = DrawCurvesField();
        DrawSound(blender.moveForwardSounds, curvesRect, blender.moveForwardColor);
        DrawSound(blender.idleSound, curvesRect, blender.idleColor);
        DrawSound(blender.moveBackwardSounds, curvesRect, blender.moveBackwardColor);

        if (GUI.changed)
        {
            EditorUtility.SetDirty(blender);
        }      
    }

    public void SoundItemListShow(List<VehicleSoundItem> list_, string name_,ref Color color_) 
    {
        EditorGUI.LabelField(GetFixRect(labelFieldWidth), name_);
        color_ = EditorGUI.ColorField(GetFixRect(floatFieldWidth), color_);
        if (GUI.Button(GetFixRect(buttonWidth), "+"))
        {
            list_.Add(new VehicleSoundItem());
            return;
        }
        nextLine();
        verticalSpace();
        for (int i = 0; i < list_.Count; i++)
        {
            SoundItemShow(list_[i]);
            if (GUI.Button(GetFixRect(buttonWidth), "-"))
            {
                list_.RemoveAt(i);
                return;
            }
            ItemPitchShow(list_[i]);
            nextLine();
        }
    }
    public void ItemPitchShow(VehicleSoundItem item) 
    { 
        if (blender.usePitch)
        {
            nextLine();
            GetFixRect(labelFieldWidth);
            EditorGUI.LabelField(GetFixRect(35f + EditorGUIUtility.standardVerticalSpacing), "Pitch");
            item.pitchMin = EditorGUI.FloatField(GetFixRect(floatFieldWidth), item.pitchMin);
            item.pitchNormMin = EditorGUI.FloatField(GetFixRect(floatFieldWidth), item.pitchNormMin);
            item.pitchNormMax = EditorGUI.FloatField(GetFixRect(floatFieldWidth), item.pitchNormMax);
            item.pitchMax = EditorGUI.FloatField(GetFixRect(floatFieldWidth), item.pitchMax);
            verticalSpace();
            verticalSpace();
        }
    }
    public void SoundItemShow(VehicleSoundItem item) 
    {
        GetFixRect(25f);
        item.clip = EditorGUI.ObjectField(GetFixRect(labelFieldWidth), "", item.clip, typeof(AudioClip)) as AudioClip;
        GetFixRect(10f);
        item.min = EditorGUI.FloatField(GetFixRect(floatFieldWidth), item.min);
        item.normMin = EditorGUI.FloatField(GetFixRect(floatFieldWidth), item.normMin);
        item.normMax = EditorGUI.FloatField(GetFixRect(floatFieldWidth), item.normMax);
        item.max = EditorGUI.FloatField(GetFixRect(floatFieldWidth), item.max);
    }

    private Rect GetRect()
    {
        Rect res = new Rect(curXFreePos, curLineRect.y, curLineRect.width - curXFreePos, EditorGUIUtility.singleLineHeight);
        curXFreePos = curLineRect.width;
        return res;
    }
    private Rect GetRect(float persentSize = -1)
    {
        float size = persentSize*curLineRect.width;
        return GetFixRect(size);
    }
    private Rect GetFixRect(float size) 
    {
        if (size < 0) 
        {
            size = -size;
        }
        Rect res = new Rect(curXFreePos + EditorGUIUtility.standardVerticalSpacing, curLineRect.y, size, EditorGUIUtility.singleLineHeight);
        curXFreePos += (size + EditorGUIUtility.standardVerticalSpacing);
        return res;
    }
    private Rect GetFixRect(float size, int lines)
    {
        if (size < 0)
        {
            size = -size;
        }
        Rect res = new Rect(curXFreePos + EditorGUIUtility.standardVerticalSpacing, curLineRect.y, size, (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * lines - EditorGUIUtility.standardVerticalSpacing);
        curXFreePos += (size + EditorGUIUtility.standardVerticalSpacing);
        return res;
    }

    private void nextLine(int lines)
    {
        for (int i = 0; i < lines; i++) 
        {
            nextLine();
        }
    }
    private void nextLine() 
    {
        EditorGUILayout.LabelField("");
        curXFreePos = fullRect.x;
        fullRect.height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        curLineRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
    }

    private void verticalSpace()
    {
        EditorGUILayout.Space();
        curXFreePos = fullRect.x;
        fullRect.height += EditorGUIUtility.standardVerticalSpacing;
        curLineRect.y += EditorGUIUtility.standardVerticalSpacing;
    }

    //-----Drawing------------

    public void DrawSound(List<VehicleSoundItem> sounds_, Rect rect_, Color color_)
    {
        Color clr = color_;
        for (int i = 0; i < sounds_.Count; i++) 
        {
            clr = color_;
            clr.r += 0.15f * i;
            clr.g -= 0.15f * i;
            DrawSound(sounds_[i], rect_, clr);
        }
    }
    public void DrawSound(VehicleSoundItem sound_, Rect rect_, Color color_) 
    {
        //Drawing.DrawLine(rect_, new Vector2(sound_.min, 0), new Vector2(sound_.normMin, 1), Color.yellow, 2);
        DrawLine(rect_, color_, new Vector2(sound_.min, 0), new Vector2(sound_.normMin, 1));
        DrawLine(rect_, color_, new Vector2(sound_.normMin, 1), new Vector2(sound_.normMax, 1));
        DrawLine(rect_, color_, new Vector2(sound_.normMax, 1), new Vector2(sound_.max, 0));
    }
    public void DrawLine(Rect rect_, Color color_, Vector2 pointA, Vector2 pointB) 
    {
        Matrix4x4 matrix = GUI.matrix;
        
        // Generate a single pixel texture if it doesn't exist
        if (!lineTex)
        {
            lineTex = new Texture2D(1, 1);
        }
       // pointA = new Vector2(rect_.x, rect_.y);
       // pointB = new Vector2(rect_.x + rect_.width, rect_.y + rect_.height);

        pointA.y = 1 - pointA.y;
        pointB.y = 1 - pointB.y;
        //pointA.x += 0.5f;
        //pointB.x += 0.5f;

        Color savedColor = GUI.color;
        GUI.color = color_;

        //Debug.Log(pointA.x + ", " + pointA.y + "   " + pointB.x + ", " + pointB.y);
        pointA.x *= rect_.width/2;
        pointB.x *= rect_.width/2;
        pointA.y *= rect_.height;
        pointB.y *= rect_.height;

        pointA += rect_.position;
        pointB += rect_.position;

        pointA.x += rect_.width / 2;
        pointB.x += rect_.width / 2;

        float angle = Vector3.Angle(pointB - pointA, Vector2.right);

        if (pointA.y > pointB.y)
        {
            angle = -angle;
        }
        if ((pointB - pointA).magnitude < 0.01f) 
        {
            return;
        }
        //Debug.Log("* " + color_);
        GUIUtility.ScaleAroundPivot(new Vector2((pointB - pointA).magnitude, 2), pointA);
        GUIUtility.RotateAroundPivot(angle, pointA);
        GUI.DrawTexture(new Rect(pointA.x, pointA.y, 1, 1), lineTex);

        // We're done.  Restore the GUI matrix and GUI color to whatever they were before.
        GUI.matrix = matrix;
        GUI.color = savedColor;
    }
    public Rect DrawCurvesField() 
    {
        GetFixRect(25f);
        Rect field = GetFixRect((350f + 5 * EditorGUIUtility.standardVerticalSpacing), 3);
        EditorGUI.DrawRect(field, Color.grey);
        nextLine(3);
        return field;
    }

}
