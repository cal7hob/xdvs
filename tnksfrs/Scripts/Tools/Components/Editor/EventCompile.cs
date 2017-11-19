using UnityEditor;

public class EventCompile
{
    public delegate void Event(bool on);
    private static event Event event_;
    private static bool isCompiling = false;
    private static int init = 0;

    /// <summary>
    /// Start compile - true and error compile - false
    /// </summary>
    public static event Event compile
    {
        add
        {
            if (init == 0) EditorApplication.update += Update;
            init++;
            event_ += value;
        }

        remove
        {
            init--;
            event_ -= value;
            if (init == 0) EditorApplication.update -= Update;
        }
    }

    private static void Update()
    {
        if (EditorApplication.isCompiling != isCompiling)
        {
            isCompiling = EditorApplication.isCompiling;
            event_(isCompiling);
            return;
        }
    }

    /*public class Test : AssetModificationProcessor
    {
        public static string[] OnWillSaveAssets(string[] paths)
        {
            CL.Log();
            Send();
            return paths;
        }
    }*/
}