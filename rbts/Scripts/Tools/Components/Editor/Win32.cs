using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using UnityEditor;
using UnityEngine;

public class Win32
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Point
    {
        public Point(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public int x;
        public int y;

        public override string ToString()
        {
            return x + " " + y;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CursorInfo
    {
        public Int32 cbSize;        // Specifies the size, in bytes, of the structure. 
        // The caller must set this to Marshal.SizeOf(typeof(CURSORINFO)).
        public Int32 flags;         // Specifies the cursor state. This parameter can be one of the following values:
        //    0                             The cursor is hidden.
        //    CURSOR_SHOWING    0x00000001  The cursor is showing.
        //    CURSOR_SUPPRESSED 0x00000002  Windows 8: The cursor is suppressed. This flag indicates that the system is not drawing the cursor because the user is providing input through touch or pen instead of the mouse.
        public IntPtr hCursor;          // Handle to the cursor. 
        public Point ptScreenPos;       // A POINT structure that receives the screen coordinates of the cursor. 
    }

/*    //Mouse
    [DllImport("User32.Dll")]//[return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetCursorPos(out Point lpMousePoint);

    [DllImport("User32.Dll")]
    public static extern long SetCursorPos(int x, int y);

    [DllImport("User32.Dll")]
    public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);
    
    [DllImport("user32.dll")]
    public static extern bool GetCursorInfo(out CursorInfo pci);

    [DllImport("user32.dll")]
    public static extern int ShowCursor(bool bShow);*/

    /*private const uint OCR_NORMAL = 32512;
    public static void SetSystemCursor(string file) { SetSystemCursor(LoadCursorFromFile(file), OCR_NORMAL); } //@"C:/Windows/Cursors/aero_arrow.cur"

    [DllImport("User32.Dll")]
    public static extern bool ClientToScreen(IntPtr hWnd, ref Point point);

    [DllImport("user32.dll")]
    public static extern IntPtr LoadCursorFromFile(string lpFileName);

    [DllImport("user32.dll")]
    public static extern bool SetSystemCursor(IntPtr hcur, uint id);*/

    //Keyboard
/*    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hwnd, IntPtr proccess);

    [DllImport("user32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, EntryPoint = "GetKeyboardLayout", SetLastError = true, ThrowOnUnmappableChar = false)]
    public static extern IntPtr GetKeyboardLayout(uint thread);

    [DllImport("user32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, EntryPoint = "LoadKeyboardLayout", SetLastError = true, ThrowOnUnmappableChar = false)]
    public static extern uint LoadKeyboardLayout(StringBuilder pwszKLID, uint flags);

    [DllImport("user32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, EntryPoint = "ActivateKeyboardLayout", SetLastError = true, ThrowOnUnmappableChar = false)]
    public static extern uint ActivateKeyboardLayout(uint hkl, uint Flags);*/

    //Window
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);

    [DllImport("user32.dll")]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)] // not work win 7
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    //[DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
    //public static extern IntPtr FindWindowByCaption(IntPtr zeroOnly, string lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", EntryPoint = "SendMessage", CharSet = CharSet.Auto)]
    public static extern bool SendMessage(IntPtr hWnd, uint Msg, int wParam, StringBuilder lParam);

    /*[DllImport("user32.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
    public static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern IntPtr SetActiveWindow(IntPtr hWnd);*/
}

/*
public class Mouse
{
    private static class MouseConst
    {
        public const int MOUSEEVENTF_LEFTDOWN = 0x02;
        public const int MOUSEEVENTF_LEFTUP = 0x04;

        public const int MOUSEEVENTF_MIDDLEDOWN = 0x20;
        public const int MOUSEEVENTF_MIDDLEUP = 0x40;

        public const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        public const int MOUSEEVENTF_RIGHTUP = 0x10;
    }
    
    private static Win32.Point point = new Win32.Point();
    private static DateTime startTimer;

    public static void LeftButtonDown()
    {
        Win32.Point p = new Win32.Point();
        Win32.GetCursorPos(out p);
        Win32.mouse_event(MouseConst.MOUSEEVENTF_LEFTDOWN, p.x, p.y, 0, 0);
    }

    public static void LeftButtonUp()
    {
        Win32.Point p = new Win32.Point();
        Win32.GetCursorPos(out p);
        Win32.mouse_event(MouseConst.MOUSEEVENTF_LEFTUP, p.x, p.y, 0, 0);
    }

    public static void ClickLeftButton()
    {
        Win32.Point p = new Win32.Point();
        Win32.GetCursorPos(out p);
        Win32.mouse_event(MouseConst.MOUSEEVENTF_LEFTDOWN, p.x, p.y, 0, 0);
        Win32.mouse_event(MouseConst.MOUSEEVENTF_LEFTUP, p.x, p.y, 0, 0);
    }

    public static void Click(int x, int y)
    {
        Win32.GetCursorPos(out point);
        Win32.SetCursorPos(x, y);
        ClickLeftButton();
        startTimer = DateTime.Now;
        EditorApplication.update += SetCursorOldPos;
    }

    static void SetCursorOldPos()
    {
        if ((DateTime.Now - startTimer).TotalMilliseconds < 1) return;
        EditorApplication.update -= SetCursorOldPos;
        Win32.SetCursorPos(point.x, point.y);
    }

    public static void SetCursorPos(int x, int y)
    {
        Win32.SetCursorPos(x, y);
    }

    public static void SetCursorPos(Win32.Point point)
    {
        Win32.SetCursorPos(point.x, point.y);
    }

    public static Win32.Point GetCursorPos()
    {
        Win32.Point p = new Win32.Point();
        Win32.GetCursorPos(out p);
        return p;
    }

    public static void Hide()
    {
        Win32.ShowCursor(false);
    }

    public static void Show()
    {
        Win32.ShowCursor(true);
    }

    public static bool hide
    {
        get
        {
            Win32.CursorInfo pci;
            pci.cbSize = Marshal.SizeOf(typeof(Win32.CursorInfo));
            Win32.GetCursorInfo(out pci);
            return pci.flags == 0;
        }

        set
        {
            Win32.ShowCursor(!value); //This API only affects the mouse cursor when it is over windows created by the same thread which calls it.
        }
    }
}

public class Keyboard
{
    public class KeyboardLayout
    {
        private static class KeyboardLayoutFlags
        {
            public const uint KLF_ACTIVATE = 0x00000001;
            public const uint KLF_SETFORPROCESS = 0x00000100;
        }

        public static CultureInfo GetCurrent() //en-US=1033, ru-RU=1049
        {
            try
            {
                return new CultureInfo(Win32.GetKeyboardLayout(Win32.GetWindowThreadProcessId(Win32.GetForegroundWindow(), IntPtr.Zero)).ToInt32() & 0xFFFF);
            }
            catch (Exception _)
            {
                return new CultureInfo(1033); // Assume English if something went wrong.
            }
        }

        public static void SetCurrent(int keyboardLayoutId)
        {
            SetCurrent(CultureInfo.GetCultureInfo(keyboardLayoutId));
        }

        public static void SetCurrent(CultureInfo cultureInfo)
        {
            Win32.ActivateKeyboardLayout(Win32.LoadKeyboardLayout(new StringBuilder(cultureInfo.LCID.ToString("x8")), KeyboardLayoutFlags.KLF_ACTIVATE), KeyboardLayoutFlags.KLF_SETFORPROCESS);
        }
    }
}
*/

public class Window
{
    private const int WM_GETTEXT = 0x000D;
    private const int WM_GETTEXTLENGTH = 0x000E;

    public static IntPtr current
    {
        get
        {
            return Win32.GetForegroundWindow();
        }

        set
        {
            Win32.SetForegroundWindow(value);
        }
    }

    public static string GetCurrentTitle()
    {
        return GetTitle(Win32.GetForegroundWindow());
    }

    public static bool GetCurrentRect(out Rect outRect)
    {
        return GetRect(Win32.GetForegroundWindow(), out outRect);
    }

    public static string GetTitle(IntPtr handle)
    {
        int size = 256; Win32.SendMessage(handle, WM_GETTEXTLENGTH, IntPtr.Zero, IntPtr.Zero).ToInt32();
        if (size > 0)
        {
            StringBuilder title = new StringBuilder(size + 1);
            if (Win32.SendMessage(handle, WM_GETTEXT, size, title))//if (Win32.GetWindowText(handle, title, 256) > 0)
            {
                //Debug.Log(title);
                return title.ToString();
            }
        }
        return null;
    }

    public static bool GetRect(IntPtr handle, out Rect outRect)
    {
        Win32.RECT rect = new Win32.RECT();
        if (Win32.GetWindowRect(handle, ref rect))
        {
            outRect = new Rect(rect.Left, rect.Top, rect.Bottom - rect.Top, rect.Right - rect.Left);
            return true;
        }
        outRect = new Rect();
        return false;
    }
}