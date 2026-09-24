using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

static class ClickTool
{
    [DllImport("user32.dll")] static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] static extern void mouse_event(uint f, uint dx, uint dy, uint data, int ex);
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr h);

    [STAThread]
    static int Main(string[] a)
    {
        if (a.Length < 2) { return 2; }
        SetProcessDPIAware();

        if (a[0] == "click" && a.Length >= 3)
        {
            ulong hwnd;
            if (a.Length >= 4 && ulong.TryParse(a[3], out hwnd))
            {
                SetForegroundWindow((IntPtr)hwnd);
                Thread.Sleep(300);
            }
            int x = int.Parse(a[1]), y = int.Parse(a[2]);
            SetCursorPos(x, y);
            Thread.Sleep(250);
            mouse_event(2, 0, 0, 0, 0);
            Thread.Sleep(90);
            mouse_event(4, 0, 0, 0, 0);
            Thread.Sleep(400);
            return 0;
        }

        if (a[0] == "type" && a.Length >= 2)
        {
            foreach (var ch in a[1])
            {
                string s = ch.ToString();
                if ("+^%~(){}[]".IndexOf(ch) >= 0) s = "{" + s + "}";
                SendKeys.SendWait(s);
                Thread.Sleep(30);
            }
            Thread.Sleep(200);
            return 0;
        }

        if (a[0] == "key" && a.Length >= 2)
        {
            SendKeys.SendWait(a[1]);
            Thread.Sleep(200);
            return 0;
        }

        return 2;
    }
}
