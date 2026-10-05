using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Automation;
using System.Windows.Forms;

namespace ScreenCaptureUtil;

internal static class Program
{
    [DllImport("user32.dll")] private static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] private static extern bool SetProcessDpiAwarenessContext(IntPtr dpiFlag);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] private static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);
    [DllImport("user32.dll")] private static extern IntPtr GetWindowDC(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
    [DllImport("gdi32.dll")] private static extern bool BitBlt(IntPtr hObject, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hObjectSource, int nXSrc, int nYSrc, uint dwRop);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    [DllImport("user32.dll")] private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);
    [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] private static extern IntPtr SetFocus(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] private static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);
    [DllImport("user32.dll")] private static extern IntPtr WindowFromPoint(POINT Point);

    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint WM_LBUTTONDOWN = 0x0201;
    private const uint WM_LBUTTONUP = 0x0202;

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private static IntPtr FindWindowByTitle(string titlePattern)
    {
        IntPtr found = IntPtr.Zero;
        EnumWindows((h, l) =>
        {
            if (!IsWindowVisible(h)) return true;

            var sb = new StringBuilder(512);
            GetWindowText(h, sb, 512);
            var title = sb.ToString();

            GetWindowRect(h, out RECT r);
            int w = r.Right - r.Left;
            int hg = r.Bottom - r.Top;
            if (w < 100 || hg < 100) return true;

            GetWindowThreadProcessId(h, out uint pid);
            string procName = "";
            try { procName = System.Diagnostics.Process.GetProcessById((int)pid).ProcessName; } catch {}

            bool isCloventProc = procName.Contains("Clovent", StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrEmpty(titlePattern) || titlePattern == "*")
            {
                if (isCloventProc)
                {
                    found = h;
                    return false;
                }
            }
            else if (!string.IsNullOrEmpty(title) && title.IndexOf(titlePattern, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                found = h;
                return false;
            }

            return true;
        }, IntPtr.Zero);
        return found;
    }

    private static bool CaptureHwnd(IntPtr targetHwnd, string outputPath)
    {
        if (targetHwnd == IntPtr.Zero) return false;

        var dir = Path.GetDirectoryName(Path.GetFullPath(outputPath));
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        SetForegroundWindow(targetHwnd);
        Thread.Sleep(250);

        GetWindowRect(targetHwnd, out RECT r);
        int w = r.Right - r.Left;
        int h = r.Bottom - r.Top;
        if (w <= 0 || h <= 0) return false;

        using (var bmp = new Bitmap(w, h))
        {
            using (var g = Graphics.FromImage(bmp))
            {
                IntPtr hdc = g.GetHdc();
                bool ok = PrintWindow(targetHwnd, hdc, 2);
                if (!ok) ok = PrintWindow(targetHwnd, hdc, 0);
                if (!ok)
                {
                    IntPtr wdc = GetWindowDC(targetHwnd);
                    BitBlt(hdc, 0, 0, w, h, wdc, 0, 0, 0x00CC0020);
                    ReleaseDC(targetHwnd, wdc);
                }
                g.ReleaseHdc(hdc);
            }
            bmp.Save(outputPath, ImageFormat.Png);
        }
        Console.WriteLine($"CAPTURED: {outputPath} ({w}x{h}) HWND={targetHwnd}");
        return true;
    }

    private static void ClickControlHwnd(IntPtr hwnd)
    {
        GetWindowRect(hwnd, out RECT r);
        int w = r.Right - r.Left;
        int h = r.Bottom - r.Top;
        IntPtr lParam = (IntPtr)((h / 2 << 16) | (w / 2));
        SendMessage(hwnd, WM_LBUTTONDOWN, (IntPtr)1, lParam);
        Thread.Sleep(60);
        SendMessage(hwnd, WM_LBUTTONUP, IntPtr.Zero, lParam);
        SendMessage(hwnd, 0x00F5 /* BM_CLICK */, IntPtr.Zero, IntPtr.Zero);
        Thread.Sleep(200);
    }

    private static void ClickScreenPoint(int screenX, int screenY, IntPtr defaultHwnd)
    {
        POINT pt = new POINT { X = screenX, Y = screenY };
        IntPtr targetHwnd = WindowFromPoint(pt);
        if (targetHwnd == IntPtr.Zero) targetHwnd = defaultHwnd;

        POINT clientPt = new POINT { X = screenX, Y = screenY };
        ScreenToClient(targetHwnd, ref clientPt);
        IntPtr lParam = (IntPtr)((clientPt.Y << 16) | (clientPt.X & 0xFFFF));

        SetForegroundWindow(targetHwnd);
        Thread.Sleep(50);
        PostMessage(targetHwnd, 0x0200 /* WM_MOUSEMOVE */, IntPtr.Zero, lParam);
        SendMessage(targetHwnd, 0x0200 /* WM_MOUSEMOVE */, IntPtr.Zero, lParam);
        Thread.Sleep(50);
        PostMessage(targetHwnd, WM_LBUTTONDOWN, (IntPtr)1, lParam);
        SendMessage(targetHwnd, WM_LBUTTONDOWN, (IntPtr)1, lParam);
        Thread.Sleep(100);
        PostMessage(targetHwnd, WM_LBUTTONUP, IntPtr.Zero, lParam);
        SendMessage(targetHwnd, WM_LBUTTONUP, IntPtr.Zero, lParam);
        try
        {
            SetCursorPos(screenX, screenY);
            Thread.Sleep(50);
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
            Thread.Sleep(50);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        }
        catch { }
        Console.WriteLine($"CLICK_POINT: screen=({screenX},{screenY}) client=({clientPt.X},{clientPt.Y}) HWND={targetHwnd}");
    }

    private static AutomationElement? FindElementByName(AutomationElement root, string name, string? controlType = null)
    {
        var all = root.FindAll(TreeScope.Descendants, Condition.TrueCondition);
        AutomationElement? substringMatch = null;
        foreach (AutomationElement el in all)
        {
            try
            {
                var elName = el.Current.Name?.Trim();
                var elId = el.Current.AutomationId?.Trim();
                var ctrlProg = el.Current.ControlType.ProgrammaticName;
                if (controlType != null && !ctrlProg.EndsWith(controlType, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.Equals(elName, name.Trim(), StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(elId, name.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return el;
                }

                if (substringMatch == null && elName != null && elName.IndexOf(name.Trim(), StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    substringMatch = el;
                }
            }
            catch {}
        }
        return substringMatch;
    }

    private const uint WM_CHAR = 0x0102;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    [DllImport("user32.dll")] private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);
    [DllImport("user32.dll")] private static extern short VkKeyScan(char ch);

    private static void SendKeySpec(string spec)
    {
        byte vk = 0;
        string s = spec.ToLowerInvariant();
        if (s == "enter" || s == "{enter}") vk = 0x0D;
        else if (s == "tab" || s == "{tab}") vk = 0x09;
        else if (s == "esc" || s == "{esc}") vk = 0x1B;
        else if (s == "backspace" || s == "{backspace}") vk = 0x08;
        else if (s == "space" || s == " ") vk = 0x20;
        else if (s == "down" || s == "{down}") vk = 0x28;
        else if (s == "up" || s == "{up}") vk = 0x26;
        else if (s == "left" || s == "{left}") vk = 0x25;
        else if (s == "right" || s == "{right}") vk = 0x27;
        else if (s == "home" || s == "{home}") vk = 0x24;
        else if (s == "end" || s == "{end}") vk = 0x23;
        else if (s == "pageup" || s == "{pgup}") vk = 0x21;
        else if (s == "pagedown" || s == "{pgdn}") vk = 0x22;
        else if (s == "delete" || s == "{del}") vk = 0x2E;
        else if (s.StartsWith("f") && int.TryParse(s.Trim('{', '}').Substring(1), out int fNum) && fNum >= 1 && fNum <= 12)
        {
            vk = (byte)(0x6F + fNum);
        }

        if (vk != 0)
        {
            keybd_event(vk, 0, 0, 0);
            Thread.Sleep(30);
            keybd_event(vk, 0, KEYEVENTF_KEYUP, 0);
            Thread.Sleep(30);
        }
        else
        {
            SendKeysDirect(spec);
        }
    }

    private static void SendKeysDirect(string text)
    {
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '^' && i + 1 < text.Length)
            {
                i++;
                char c = text[i];
                byte vk = (byte)char.ToUpperInvariant(c);
                keybd_event(0x11 /* VK_CONTROL */, 0, 0, 0);
                Thread.Sleep(20);
                keybd_event(vk, 0, 0, 0);
                Thread.Sleep(20);
                keybd_event(vk, 0, KEYEVENTF_KEYUP, 0);
                Thread.Sleep(20);
                keybd_event(0x11 /* VK_CONTROL */, 0, KEYEVENTF_KEYUP, 0);
                Thread.Sleep(20);
                continue;
            }

            if (text[i] == '{' && text.IndexOf('}', i) > i)
            {
                int end = text.IndexOf('}', i);
                string keyName = text.Substring(i + 1, end - i - 1);
                SendKeySpec(keyName);
                i = end;
                continue;
            }

            short vkScan = VkKeyScan(text[i]);
            byte keyVk = (byte)(vkScan & 0xFF);
            byte shift = (byte)((vkScan >> 8) & 0xFF);

            if ((shift & 1) != 0)
            {
                keybd_event(0x10 /* VK_SHIFT */, 0, 0, 0);
                Thread.Sleep(15);
            }

            keybd_event(keyVk, 0, 0, 0);
            Thread.Sleep(25);
            keybd_event(keyVk, 0, KEYEVENTF_KEYUP, 0);
            Thread.Sleep(15);

            if ((shift & 1) != 0)
            {
                keybd_event(0x10 /* VK_SHIFT */, 0, KEYEVENTF_KEYUP, 0);
                Thread.Sleep(15);
            }
        }
    }

    [STAThread]
    private static int Main(string[] args)
    {
        try { SetProcessDpiAwarenessContext((IntPtr)(-4)); }
        catch { try { SetProcessDPIAware(); } catch { } }

        if (args.Length == 0)
        {
            Console.WriteLine("Commands: screen, cap-title, cap-hwnd, list-wins, dump, click-btn, click-name, click-xy, set-text, type, key, wait-win");
            return 1;
        }

        string cmd = args[0].ToLowerInvariant();

        switch (cmd)
        {
            case "screen":
            {
                string outPath = args.Length >= 2 ? args[1] : "screen.png";
                var bounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
                using var bmp = new Bitmap(bounds.Width, bounds.Height);
                using (var g = Graphics.FromImage(bmp))
                {
                    IntPtr screenDc = GetWindowDC(IntPtr.Zero);
                    IntPtr memDc = g.GetHdc();
                    BitBlt(memDc, 0, 0, bounds.Width, bounds.Height, screenDc, 0, 0, 0x00CC0020);
                    g.ReleaseHdc(memDc);
                    ReleaseDC(IntPtr.Zero, screenDc);
                }
                bmp.Save(outPath, ImageFormat.Png);
                Console.WriteLine($"CAPTURED_SCREEN: {outPath} ({bounds.Width}x{bounds.Height})");
                return 0;
            }

            case "title":
            case "cap-title":
            {
                if (args.Length < 3) { Console.Error.WriteLine("Usage: cap-title <titleFilter> <outPath>"); return 1; }
                IntPtr h = FindWindowByTitle(args[1]);
                if (h == IntPtr.Zero) { Console.Error.WriteLine($"ERROR: Window '{args[1]}' not found"); return 2; }
                return CaptureHwnd(h, args[2]) ? 0 : 3;
            }

            case "close-title":
            {
                if (args.Length < 2) { Console.Error.WriteLine("Usage: close-title <titleFilter>"); return 1; }
                IntPtr h = FindWindowByTitle(args[1]);
                if (h == IntPtr.Zero) { Console.Error.WriteLine($"ERROR: Window '{args[1]}' not found"); return 2; }
                PostMessage(h, 0x0010 /* WM_CLOSE */, IntPtr.Zero, IntPtr.Zero);
                Console.WriteLine($"CLOSED_WINDOW: '{args[1]}' HWND={h}");
                return 0;
            }

            case "cap-hwnd":
            {
                if (args.Length < 3) { Console.Error.WriteLine("Usage: cap-hwnd <hwnd> <outPath>"); return 1; }
                IntPtr h = (IntPtr)long.Parse(args[1]);
                return CaptureHwnd(h, args[2]) ? 0 : 3;
            }

            case "cap-pid":
            {
                if (args.Length < 3) { Console.Error.WriteLine("Usage: cap-pid <pid> <outPath>"); return 1; }
                uint targetPid = uint.Parse(args[1]);
                IntPtr found = IntPtr.Zero;
                EnumWindows((h, l) =>
                {
                    GetWindowThreadProcessId(h, out uint pid);
                    if (pid == targetPid)
                    {
                        GetWindowRect(h, out RECT r);
                        int w = r.Right - r.Left;
                        int hg = r.Bottom - r.Top;
                        Console.WriteLine($"Found PID={pid} HWND={h} Size={w}x{hg}");
                        if (w > 200 && hg > 150)
                        {
                            found = h;
                            return false;
                        }
                    }
                    return true;
                }, IntPtr.Zero);
                if (found == IntPtr.Zero) { Console.Error.WriteLine($"ERROR: Window for PID {targetPid} not found"); return 2; }
                return CaptureHwnd(found, args[2]) ? 0 : 3;
            }

            case "list-wins":
            {
                EnumWindows((h, l) =>
                {
                    if (IsWindowVisible(h))
                    {
                        var sb = new StringBuilder(256);
                        GetWindowText(h, sb, 256);
                        if (sb.Length > 0)
                        {
                            GetWindowThreadProcessId(h, out uint pid);
                            Console.WriteLine($"HWND={h} PID={pid} TITLE='{sb}'");
                        }
                    }
                    return true;
                }, IntPtr.Zero);
                return 0;
            }

            case "dump":
            {
                string title = args.Length >= 2 ? args[1] : "Clovent";
                IntPtr h = FindWindowByTitle(title);
                if (h == IntPtr.Zero) { Console.Error.WriteLine($"Window '{title}' not found"); return 2; }
                var win = AutomationElement.FromHandle(h);
                var all = win.FindAll(TreeScope.Descendants, Condition.TrueCondition);
                Console.WriteLine($"DUMP: {title} (Total elements: {all.Count})");
                foreach (AutomationElement el in all)
                {
                    var n = el.Current.Name;
                    var c = el.Current.ClassName;
                    var t = el.Current.ControlType.ProgrammaticName.Replace("ControlType.", "");
                    var aid = el.Current.AutomationId;
                    var r = el.Current.BoundingRectangle;
                    int hwndVal = el.Current.NativeWindowHandle;
                    if (!string.IsNullOrWhiteSpace(n) || !string.IsNullOrWhiteSpace(aid))
                    {
                        Console.WriteLine($"[{t}] Name='{n}' Id='{aid}' HWND={hwndVal} Class='{c}' Rect=({(int)r.X},{(int)r.Y},{(int)r.Width},{(int)r.Height})");
                    }
                }
                return 0;
            }

            case "inspect":
            {
                if (args.Length < 3) { Console.Error.WriteLine("Usage: inspect <windowTitle> <name>"); return 1; }
                IntPtr h = FindWindowByTitle(args[1]);
                if (h == IntPtr.Zero) { Console.Error.WriteLine($"Window '{args[1]}' not found"); return 2; }
                var win = AutomationElement.FromHandle(h);
                var el = FindElementByName(win, args[2]);
                if (el == null) { Console.Error.WriteLine($"Element '{args[2]}' not found"); return 3; }
                Console.WriteLine($"Found: Name='{el.Current.Name}' Id='{el.Current.AutomationId}' Type='{el.Current.ControlType.ProgrammaticName}'");
                foreach (var pattern in el.GetSupportedPatterns())
                {
                    Console.WriteLine($"  Pattern: {pattern.ProgrammaticName}");
                }
                var children = el.FindAll(TreeScope.Children, Condition.TrueCondition);
                Console.WriteLine($"  Children count: {children.Count}");
                foreach (AutomationElement c in children)
                {
                    Console.WriteLine($"    Child: Name='{c.Current.Name}' Id='{c.Current.AutomationId}' Type='{c.Current.ControlType.ProgrammaticName}' Rect={c.Current.BoundingRectangle}");
                }
                return 0;
            }

            case "select-combo":
            {
                if (args.Length < 4) { Console.Error.WriteLine("Usage: select-combo <windowTitle> <comboName> <itemText>"); return 1; }
                IntPtr h = FindWindowByTitle(args[1]);
                if (h == IntPtr.Zero) { Console.Error.WriteLine($"Window '{args[1]}' not found"); return 2; }
                var win = AutomationElement.FromHandle(h);
                var el = FindElementByName(win, args[2], "ComboBox") ?? FindElementByName(win, args[2]);
                if (el == null) { Console.Error.WriteLine($"ComboBox '{args[2]}' not found"); return 3; }

                SetForegroundWindow(h);
                Thread.Sleep(100);

                if (el.TryGetCurrentPattern(ValuePattern.Pattern, out object? vpObj) && vpObj is ValuePattern vp)
                {
                    try
                    {
                        vp.SetValue(args[3]);
                        Console.WriteLine($"SET_COMBO_VALUE: '{args[2]}' -> '{args[3]}'");
                        return 0;
                    }
                    catch {}
                }

                if (el.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out object? ecpObj) && ecpObj is ExpandCollapsePattern ecp)
                {
                    try
                    {
                        ecp.Expand();
                        Thread.Sleep(300);
                        var listItem = win.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, args[3]));
                        if (listItem != null && listItem.TryGetCurrentPattern(SelectionItemPattern.Pattern, out object? sipObj) && sipObj is SelectionItemPattern sip)
                        {
                            sip.Select();
                            Console.WriteLine($"SELECTED_COMBO_ITEM: '{args[3]}'");
                            return 0;
                        }
                    }
                    catch {}
                }

                // Fallback: Click dropdown arrow child button if found
                var openBtn = el.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button));
                if (openBtn != null)
                {
                    var r = openBtn.Current.BoundingRectangle;
                    int cx = (int)(r.X + r.Width / 2);
                    int cy = (int)(r.Y + r.Height / 2);
                    SetCursorPos(cx, cy);
                    Thread.Sleep(50);
                    mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                    Thread.Sleep(50);
                    mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                    Thread.Sleep(300);
                    SendKeysDirect(args[3]);
                    Thread.Sleep(100);
                    SendKeySpec("enter");
                    Console.WriteLine($"CLICKED_OPEN_TYPED: '{args[3]}'");
                    return 0;
                }

                Console.Error.WriteLine($"Could not select '{args[3]}' in ComboBox '{args[2]}'");
                return 4;
            }

            case "maximize-win":
            {
                if (args.Length < 2) { Console.Error.WriteLine("Usage: maximize-win <windowTitle>"); return 1; }
                IntPtr h = FindWindowByTitle(args[1]);
                if (h == IntPtr.Zero) { Console.Error.WriteLine($"Window '{args[1]}' not found"); return 2; }
                ShowWindow(h, 3 /* SW_MAXIMIZE */);
                SetForegroundWindow(h);
                Console.WriteLine($"MAXIMIZED_WINDOW: '{args[1]}' HWND={h}");
                Thread.Sleep(500);
                return 0;
            }

            case "select-tab":
            {
                if (args.Length < 3) { Console.Error.WriteLine("Usage: select-tab <windowTitle> <tabName>"); return 1; }
                IntPtr h = FindWindowByTitle(args[1]);
                if (h == IntPtr.Zero) { Console.Error.WriteLine($"Window '{args[1]}' not found"); return 2; }
                var win = AutomationElement.FromHandle(h);
                var el = FindElementByName(win, args[2], "TabItem") ?? FindElementByName(win, args[2]);
                if (el == null) { Console.Error.WriteLine($"Tab '{args[2]}' not found"); return 3; }
                SetForegroundWindow(h);
                Thread.Sleep(100);

                if (el.TryGetCurrentPattern(SelectionItemPattern.Pattern, out object? sipObj) && sipObj is SelectionItemPattern sip)
                {
                    try { sip.Select(); } catch { }
                }

                var r = el.Current.BoundingRectangle;
                if (r.Width > 0 && r.Height > 0)
                {
                    int cx = (int)(r.X + r.Width / 2);
                    int cy = (int)(r.Y + r.Height / 2);
                    ClickScreenPoint(cx, cy, h);
                    Console.WriteLine($"SELECTED_TAB_CLICK: '{args[2]}' at ({cx}, {cy})");
                }
                else
                {
                    Console.WriteLine($"SELECTED_TAB: '{args[2]}'");
                }
                Thread.Sleep(500);
                return 0;
            }

            case "click-btn":
            {
                if (args.Length < 3) { Console.Error.WriteLine("Usage: click-btn <windowTitle> <buttonName>"); return 1; }
                IntPtr h = FindWindowByTitle(args[1]);
                if (h == IntPtr.Zero) { Console.Error.WriteLine($"Window '{args[1]}' not found"); return 2; }
                var win = AutomationElement.FromHandle(h);
                var el = FindElementByName(win, args[2], "Button") ?? FindElementByName(win, args[2], "MenuItem") ?? FindElementByName(win, args[2]);
                if (el == null) { Console.Error.WriteLine($"Button '{args[2]}' not found"); return 3; }
                SetForegroundWindow(h);
                Thread.Sleep(100);

                var r = el.Current.BoundingRectangle;
                if (r.Width > 0 && r.Height > 0)
                {
                    int cx = (int)(r.X + r.Width / 2);
                    int cy = (int)(r.Y + r.Height / 2);
                    ClickScreenPoint(cx, cy, h);
                    Console.WriteLine($"CLICKED_BTN_POINT: '{args[2]}' at ({cx}, {cy})");
                }

                if (el.TryGetCurrentPattern(InvokePattern.Pattern, out object? ipObj) && ipObj is InvokePattern ip)
                {
                    try { ip.Invoke(); Console.WriteLine($"INVOKED_BTN: '{args[2]}'"); } catch { }
                }

                IntPtr btnHwnd = (IntPtr)el.Current.NativeWindowHandle;
                if (btnHwnd != IntPtr.Zero)
                {
                    ClickControlHwnd(btnHwnd);
                    Console.WriteLine($"CLICKED_BTN_HWND: '{args[2]}' HWND={btnHwnd}");
                }

                Thread.Sleep(300);
                return 0;
            }

            case "click-id":
            {
                if (args.Length < 3) { Console.Error.WriteLine("Usage: click-id <windowTitle> <automationId>"); return 1; }
                IntPtr h = FindWindowByTitle(args[1]);
                if (h == IntPtr.Zero) { Console.Error.WriteLine($"Window '{args[1]}' not found"); return 2; }
                var win = AutomationElement.FromHandle(h);
                var el = win.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, args[2]));
                if (el == null) { Console.Error.WriteLine($"Element with AutomationId '{args[2]}' not found"); return 3; }

                for (int w = 0; w < 10 && !el.Current.IsEnabled; w++)
                {
                    Thread.Sleep(500);
                }

                var r = el.Current.BoundingRectangle;
                int cx = (int)(r.X + r.Width / 2);
                int cy = (int)(r.Y + r.Height / 2);
                SetForegroundWindow(h);
                Thread.Sleep(100);
                ClickScreenPoint(cx, cy, h);

                if (el.TryGetCurrentPattern(InvokePattern.Pattern, out object? ipObj) && ipObj is InvokePattern ip)
                {
                    try { ip.Invoke(); Console.WriteLine($"INVOKED_ID: '{args[2]}'"); } catch { }
                }

                IntPtr btnHwnd = (IntPtr)el.Current.NativeWindowHandle;
                if (btnHwnd != IntPtr.Zero)
                {
                    ClickControlHwnd(btnHwnd);
                }

                Console.WriteLine($"CLICKED_ID: '{args[2]}' at ({cx}, {cy})");
                Thread.Sleep(300);
                return 0;
            }

            case "select-pos-payment":
            {
                if (args.Length < 3) { Console.Error.WriteLine("Usage: select-pos-payment <windowTitle> <methodName>"); return 1; }
                IntPtr h = FindWindowByTitle(args[1]);
                if (h == IntPtr.Zero) { Console.Error.WriteLine($"Window '{args[1]}' not found"); return 2; }
                var win = AutomationElement.FromHandle(h);
                var panel = win.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, "pnlPaymentMethods"));
                if (panel == null) { Console.Error.WriteLine("pnlPaymentMethods not found"); return 3; }
                var children = panel.FindAll(TreeScope.Children, Condition.TrueCondition);
                AutomationElement? lookup = null;
                foreach (AutomationElement c in children)
                {
                    if (c.Current.Name != "PAYMENT METHOD")
                    {
                        lookup = c;
                        break;
                    }
                }
                if (lookup == null && children.Count > 1) lookup = children[1];
                if (lookup == null) { Console.Error.WriteLine("Payment method lookup not found in panel"); return 4; }

                Console.WriteLine($"LOOKUP_INFO: Name='{lookup.Current.Name}', Type='{lookup.Current.ControlType.ProgrammaticName}', HWND={lookup.Current.NativeWindowHandle}");

                SetForegroundWindow(h);
                Thread.Sleep(100);

                try
                {
                    lookup.SetFocus();
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"lookup.SetFocus failed: {ex.Message}");
                }

                string target = args[2].Trim().ToLowerInvariant();

                // 1. Try ValuePattern
                bool valueSet = false;
                if (lookup.TryGetCurrentPattern(ValuePattern.Pattern, out object? vpObj) && vpObj is ValuePattern vp)
                {
                    try
                    {
                        string valToSet = target == "card" ? "Card" : target == "cash" ? "Cash" : "On Account";
                        vp.SetValue(valToSet);
                        Console.WriteLine($"VALUE_PATTERN set to '{valToSet}'");
                        valueSet = true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ValuePattern.SetValue failed: {ex.Message}");
                    }
                }

                // 2. Try ExpandCollapsePattern
                if (lookup.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out object? ecpObj) && ecpObj is ExpandCollapsePattern ecp)
                {
                    try
                    {
                        ecp.Expand();
                        Thread.Sleep(200);
                        Console.WriteLine("EXPANDED_DROPDOWN");
                    }
                    catch { }
                }

                // 3. Click and keyboard navigation
                var r = lookup.Current.BoundingRectangle;
                int textX = (int)(r.Left + 40);
                int cy = (int)(r.Top + r.Height / 2);
                int arrowX = (int)(r.Right - 15);

                ClickScreenPoint(textX, cy, h);
                Thread.Sleep(100);

                try { lookup.SetFocus(); } catch { }
                Thread.Sleep(100);

                // Press F4 to toggle dropdown open
                SendKeySpec("f4");
                Thread.Sleep(200);

                if (target == "card")
                {
                    // Card is the very first row
                    SendKeySpec("home");
                    Thread.Sleep(100);
                    SendKeySpec("enter");
                    Thread.Sleep(100);
                    // Also send Up arrow in case dropdown didn't open and Up navigates previous
                    SendKeySpec("up");
                    Thread.Sleep(100);
                    SendKeySpec("enter");
                }
                else if (target == "cash")
                {
                    SendKeySpec("home");
                    Thread.Sleep(100);
                    SendKeySpec("down");
                    Thread.Sleep(100);
                    SendKeySpec("enter");
                }
                else if (target == "on account" || target == "account")
                {
                    SendKeySpec("end");
                    Thread.Sleep(100);
                    SendKeySpec("enter");
                }

                Thread.Sleep(200);
                // Also click dropdown arrow if popup still open or needs toggle
                SendKeySpec("enter");
                Thread.Sleep(200);
                Console.WriteLine($"SELECTED_POS_PAYMENT: '{args[2]}'");
                return 0;
            }

            case "click-name":
            {
                if (args.Length < 3) { Console.Error.WriteLine("Usage: click-name <windowTitle> <elementName>"); return 1; }
                IntPtr h = FindWindowByTitle(args[1]);
                if (h == IntPtr.Zero) { Console.Error.WriteLine($"Window '{args[1]}' not found"); return 2; }
                var win = AutomationElement.FromHandle(h);
                var el = FindElementByName(win, args[2]);
                if (el == null) { Console.Error.WriteLine($"Element '{args[2]}' not found"); return 3; }
                var r = el.Current.BoundingRectangle;
                int cx = (int)(r.X + r.Width / 2);
                int cy = (int)(r.Y + r.Height / 2);
                SetForegroundWindow(h);
                Thread.Sleep(100);
                ClickScreenPoint(cx, cy, h);

                if (el.TryGetCurrentPattern(InvokePattern.Pattern, out object? ipObj) && ipObj is InvokePattern ip)
                {
                    try { ip.Invoke(); Console.WriteLine($"INVOKED_NAME: '{args[2]}'"); } catch { }
                }

                IntPtr elHwnd = (IntPtr)el.Current.NativeWindowHandle;
                if (elHwnd != IntPtr.Zero)
                {
                    ClickControlHwnd(elHwnd);
                    Console.WriteLine($"CLICKED_NAME_HWND: '{args[2]}' HWND={elHwnd}");
                }

                Console.WriteLine($"CLICKED_NAME: '{args[2]}' at ({cx}, {cy})");
                Thread.Sleep(300);
                return 0;
            }

            case "click-xy":
            {
                if (args.Length < 3) { Console.Error.WriteLine("Usage: click-xy <x> <y>"); return 1; }
                int x = int.Parse(args[1]);
                int y = int.Parse(args[2]);
                SetCursorPos(x, y);
                Thread.Sleep(50);
                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                Thread.Sleep(50);
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                Console.WriteLine($"CLICKED_XY: ({x}, {y})");
                Thread.Sleep(200);
                return 0;
            }

            case "click-point-rel":
            {
                if (args.Length < 4) { Console.Error.WriteLine("Usage: click-point-rel <windowTitle> <relX> <relY>"); return 1; }
                IntPtr h = FindWindowByTitle(args[1]);
                if (h == IntPtr.Zero) { Console.Error.WriteLine($"Window '{args[1]}' not found"); return 2; }
                GetWindowRect(h, out RECT r);
                int rx = int.Parse(args[2]);
                int ry = int.Parse(args[3]);
                int sx = r.Left + rx;
                int sy = r.Top + ry;
                ClickScreenPoint(sx, sy, h);
                Console.WriteLine($"CLICKED_POINT_REL: '{args[1]}' rel=({rx}, {ry}) screen=({sx}, {sy})");
                Thread.Sleep(300);
                return 0;
            }

            case "set-text":
            {
                if (args.Length < 4) { Console.Error.WriteLine("Usage: set-text <windowTitle> <elementName> <text>"); return 1; }
                IntPtr h = FindWindowByTitle(args[1]);
                if (h == IntPtr.Zero) { Console.Error.WriteLine($"Window '{args[1]}' not found"); return 2; }
                var win = AutomationElement.FromHandle(h);
                var el = FindElementByName(win, args[2], "Edit") ?? FindElementByName(win, args[2]);
                if (el == null) { Console.Error.WriteLine($"Element '{args[2]}' not found"); return 3; }

                // If el is container, find child Edit
                var childEdit = el.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit));
                if (childEdit != null) el = childEdit;
                
                var r = el.Current.BoundingRectangle;
                int cx = (int)(r.X + r.Width / 2);
                int cy = (int)(r.Y + r.Height / 2);
                SetForegroundWindow(h);
                Thread.Sleep(100);
                SetCursorPos(cx, cy);
                Thread.Sleep(50);
                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                Thread.Sleep(50);
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                Thread.Sleep(100);

                if (el.TryGetCurrentPattern(ValuePattern.Pattern, out object? valPatternObj) && valPatternObj is ValuePattern vp && !vp.Current.IsReadOnly)
                {
                    try
                    {
                        vp.SetValue(args[3]);
                        Console.WriteLine($"SET_TEXT_VALUEPATTERN: '{args[2]}' -> '{args[3]}'");
                        return 0;
                    }
                    catch { }
                }

                int editHwnd = el.Current.NativeWindowHandle;
                if (editHwnd != 0)
                {
                    SendMessage((IntPtr)editHwnd, 0x000C /* WM_SETTEXT */, IntPtr.Zero, Marshal.StringToBSTR(args[3]));
                    Console.WriteLine($"SET_TEXT_WM_SETTEXT: '{args[2]}' -> '{args[3]}'");
                    return 0;
                }

                SendKeysDirect("^a{BACKSPACE}");
                Thread.Sleep(50);
                SendKeysDirect(args[3]);
                Console.WriteLine($"SET_TEXT: '{args[2]}' -> '{args[3]}'");
                Thread.Sleep(200);
                return 0;
            }

            case "type":
            {
                if (args.Length < 2) { Console.Error.WriteLine("Usage: type <text> OR type <windowTitle> <text>"); return 1; }
                if (args.Length >= 3)
                {
                    IntPtr h = FindWindowByTitle(args[1]);
                    if (h != IntPtr.Zero) { SetForegroundWindow(h); Thread.Sleep(150); }
                    SendKeysDirect(args[2]);
                    Console.WriteLine($"TYPED: '{args[2]}'");
                }
                else
                {
                    SendKeysDirect(args[1]);
                    Console.WriteLine($"TYPED: '{args[1]}'");
                }
                return 0;
            }

            case "focus-key":
            {
                if (args.Length < 3) { Console.Error.WriteLine("Usage: focus-key <windowTitle> <key>"); return 1; }
                IntPtr h = FindWindowByTitle(args[1]);
                if (h == IntPtr.Zero) { Console.Error.WriteLine($"Window '{args[1]}' not found"); return 2; }
                uint targetThread = GetWindowThreadProcessId(h, out _);
                uint currentThread = GetCurrentThreadId();
                AttachThreadInput(currentThread, targetThread, true);
                SetForegroundWindow(h);
                SetFocus(h);
                Thread.Sleep(100);
                SendKeySpec(args[2]);
                Thread.Sleep(100);
                AttachThreadInput(currentThread, targetThread, false);
                Console.WriteLine($"FOCUS_KEY: '{args[2]}' to HWND={h}");
                return 0;
            }

            case "key":
            {
                if (args.Length < 2) { Console.Error.WriteLine("Usage: key <keyspec> OR key <windowTitle> <keyspec>"); return 1; }
                if (args.Length >= 3)
                {
                    IntPtr h = FindWindowByTitle(args[1]);
                    if (h != IntPtr.Zero) { SetForegroundWindow(h); Thread.Sleep(150); }
                    SendKeySpec(args[2]);
                    Console.WriteLine($"KEY: '{args[2]}'");
                }
                else
                {
                    SendKeySpec(args[1]);
                    Console.WriteLine($"KEY: '{args[1]}'");
                }
                return 0;
            }

            case "wait-win":
            {
                string title = args.Length >= 2 ? args[1] : "Clovent";
                int timeoutSec = args.Length >= 3 ? int.Parse(args[2]) : 30;
                for (int i = 0; i < timeoutSec * 2; i++)
                {
                    IntPtr h = FindWindowByTitle(title);
                    if (h != IntPtr.Zero)
                    {
                        Console.WriteLine($"FOUND_WIN: '{title}' HWND={h}");
                        return 0;
                    }
                    Thread.Sleep(500);
                }
                Console.Error.WriteLine($"TIMEOUT: Window '{title}' did not appear in {timeoutSec} seconds");
                return 2;
            }

            case "close-win":
            {
                if (args.Length < 2) { Console.Error.WriteLine("Usage: close-win <windowTitle>"); return 1; }
                IntPtr h = FindWindowByTitle(args[1]);
                if (h == IntPtr.Zero) { Console.Error.WriteLine($"Window '{args[1]}' not found"); return 2; }
                SendMessage(h, 0x0010 /* WM_CLOSE */, IntPtr.Zero, IntPtr.Zero);
                Console.WriteLine($"CLOSED_WIN: '{args[1]}' HWND={h}");
                Thread.Sleep(300);
                return 0;
            }

            default:
                Console.Error.WriteLine($"Unknown command: {cmd}");
                return 1;
        }
    }
}
