using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace smartClass.Services
{
    /// <summary>
    /// Windows AppBar API 封装：注册顶栏为系统 AppBar，
    /// 自动缩小桌面工作区排开图标和最大化窗口。
    /// </summary>
    public static class AppBarHelper
    {
        // ---- AppBar 消息 ----
        private const int ABM_NEW = 0x00;
        private const int ABM_REMOVE = 0x01;
        private const int ABM_QUERYPOS = 0x02;
        private const int ABM_SETPOS = 0x03;

        // ---- 边缘 ----
        private const int ABE_TOP = 1;

        // ---- AppBar 通知回调消息 ----
        private const int WM_APPBARNOTIFY = 0x0400 + 100;
        private const int ABN_POSCHANGED = 1;
        private const int ABN_FULLSCREENAPP = 2;

        [StructLayout(LayoutKind.Sequential)]
        private struct APPBARDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public int uCallbackMessage;
            public int uEdge;
            public RECT rc;
            public int lParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        [DllImport("shell32.dll")]
        private static extern IntPtr SHAppBarMessage(int dwMessage, ref APPBARDATA pData);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        private static readonly IntPtr HWND_TOP = new IntPtr(0);
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;

        private static IntPtr _hwnd;
        private static bool _registered;
        private static HwndSource _hwndSource;
        private static Window _window;

        /// <summary>
        /// 注册窗口为顶栏 AppBar 并缩小工作区
        /// </summary>
        public static void Register(Window window)
        {
            if (_registered) return;
            _window = window;

            // 获取 HWND
            _hwnd = new WindowInteropHelper(window).EnsureHandle();

            // 监听窗口消息以响应全屏/位置变化
            _hwndSource = HwndSource.FromHwnd(_hwnd);
            _hwndSource?.AddHook(WndProc);

            var abd = new APPBARDATA();
            abd.cbSize = Marshal.SizeOf(abd);
            abd.hWnd = _hwnd;
            abd.uCallbackMessage = WM_APPBARNOTIFY;
            abd.uEdge = ABE_TOP;

            // 1) 注册 AppBar
            SHAppBarMessage(ABM_NEW, ref abd);

            // 2) 设置位置 — Windows 自动缩小工作区
            var screen = System.Windows.Forms.Screen.PrimaryScreen;
            if (screen != null)
            {
                var b = screen.Bounds;
                abd.rc.Left = b.Left;
                abd.rc.Right = b.Right;
                abd.rc.Top = b.Top;
                abd.rc.Bottom = b.Top + 70;
            }

            SHAppBarMessage(ABM_QUERYPOS, ref abd);
            SHAppBarMessage(ABM_SETPOS, ref abd);

            // 3) 移动窗口到系统指定的位置
            SetWindowPos(_hwnd, HWND_TOP,
                abd.rc.Left, abd.rc.Top,
                abd.rc.Right - abd.rc.Left,
                abd.rc.Bottom - abd.rc.Top,
                SWP_NOACTIVATE | SWP_SHOWWINDOW);

            _registered = true;
            LogService.Log($"AppBar 已注册: Top=70, Width={abd.rc.Right - abd.rc.Left}");
        }

        /// <summary>
        /// 注销 AppBar 并恢复桌面工作区
        /// </summary>
        public static void Unregister()
        {
            if (!_registered) return;

            try
            {
                _hwndSource?.RemoveHook(WndProc);
                _hwndSource?.Dispose();
                _hwndSource = null;
            }
            catch { }

            var abd = new APPBARDATA();
            abd.cbSize = Marshal.SizeOf(abd);
            abd.hWnd = _hwnd;
            abd.uEdge = ABE_TOP;
            SHAppBarMessage(ABM_REMOVE, ref abd);

            _registered = false;
            LogService.Log("AppBar 已注销，工作区已恢复");
        }

        private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_APPBARNOTIFY)
            {
                int code = wParam.ToInt32();
                if (code == ABN_POSCHANGED)
                {
                    // 其他 AppBar 变化 — 重新申请位置
                    ReapplyPosition();
                }
                else if (code == ABN_FULLSCREENAPP)
                {
                    // 全屏应用启动/关闭 — 调整置顶
                    bool fullscreen = lParam.ToInt32() != 0;
                    _window.Topmost = !fullscreen;
                }
                handled = true;
            }
            return IntPtr.Zero;
        }

        private static void ReapplyPosition()
        {
            var abd = new APPBARDATA();
            abd.cbSize = Marshal.SizeOf(abd);
            abd.hWnd = _hwnd;
            abd.uEdge = ABE_TOP;

            var screen = System.Windows.Forms.Screen.PrimaryScreen;
            if (screen != null)
            {
                var b = screen.Bounds;
                abd.rc.Left = b.Left;
                abd.rc.Right = b.Right;
                abd.rc.Top = b.Top;
                abd.rc.Bottom = b.Top + 70;
            }

            SHAppBarMessage(ABM_QUERYPOS, ref abd);
            SHAppBarMessage(ABM_SETPOS, ref abd);

            SetWindowPos(_hwnd, HWND_TOP,
                abd.rc.Left, abd.rc.Top,
                abd.rc.Right - abd.rc.Left,
                abd.rc.Bottom - abd.rc.Top,
                SWP_NOACTIVATE | SWP_SHOWWINDOW);
        }
    }
}
