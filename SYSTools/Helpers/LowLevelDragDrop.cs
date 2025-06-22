using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace SYSTools.Helpers
{
    public class LowLevelDragDrop
    {
        [DllImport("shell32.dll")]
        private static extern bool DragAcceptFiles(IntPtr hWnd, bool fAccept);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern uint DragQueryFile(IntPtr hDrop, uint iFile, IntPtr lpszFile, uint cch);

        [DllImport("shell32.dll")]
        private static extern void DragFinish(IntPtr hDrop);

        [DllImport("ole32.dll")]
        private static extern int RevokeDragDrop(IntPtr hWnd);

        private const uint WM_DROPFILES = 0x0233;
        private Window _window;
        private HwndSource _hwndSource;

        public event Action<string[]> FilesDropped;

        public LowLevelDragDrop(Window window)
        {
            _window = window;
        }

        public void Enable()
        {
            try
            {
                if (_window.IsLoaded)
                {
                    SetupHook();
                }
                else
                {
                    _window.Loaded += (s, e) => SetupHook();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"启用底层拖拽监听失败: {ex.Message}");
            }
        }

        private void SetupHook()
        {
            try
            {
                _hwndSource = HwndSource.FromHwnd(new WindowInteropHelper(_window).Handle);
                if (_hwndSource != null)
                {
                    var hwnd = _hwndSource.Handle;
                    RevokeDragDrop(hwnd);
                    DragAcceptFiles(hwnd, true);
                    _hwndSource.AddHook(WndProc);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"设置底层拖拽钩子失败: {ex.Message}");
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            try
            {
                if ((uint)msg == WM_DROPFILES)
                {
                    HandleDropFiles(wParam);
                    handled = true;
                    return IntPtr.Zero;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"底层消息处理错误: {ex.Message}");
            }

            return IntPtr.Zero;
        }

        private void HandleDropFiles(IntPtr hDrop)
        {
            try
            {
                uint fileCount = DragQueryFile(hDrop, 0xFFFFFFFF, IntPtr.Zero, 0);
                var files = new string[fileCount];
                for (uint i = 0; i < fileCount; i++)
                {
                    uint pathLength = DragQueryFile(hDrop, i, IntPtr.Zero, 0) + 1;
                    IntPtr pathBuffer = Marshal.AllocHGlobal((int)pathLength * 2);
                    try
                    {
                        DragQueryFile(hDrop, i, pathBuffer, pathLength);
                        files[i] = Marshal.PtrToStringUni(pathBuffer);
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(pathBuffer);
                    }
                }
                DragFinish(hDrop);
                FilesDropped?.Invoke(files);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"提取失败: {ex.Message}");
            }
        }

        public void Disable()
        {
            try
            {
                if (_hwndSource != null)
                {
                    var hwnd = _hwndSource.Handle;
                    DragAcceptFiles(hwnd, false);
                    _hwndSource.RemoveHook(WndProc);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"禁用底层拖拽监听失败: {ex.Message}");
            }
        }
    }
} 