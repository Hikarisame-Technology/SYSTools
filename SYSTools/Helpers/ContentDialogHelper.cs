using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using iNKORE.UI.WPF.Modern.Controls;
using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;

namespace SYSTools.Helpers
{
    public static class ContentDialogHelper
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private static ContentDialog _currentDialog;
        private static readonly object _lock = new object();
        private static bool _isWindowStateMonitored = false;

        public static async Task<ContentDialogResult> ShowAsync(ContentDialog dialog)
        {
            EnsureWindowStateMonitoring();
            ForceCloseAllDialogs();
            EnsureMainWindowVisible();
            await Task.Delay(100);

            lock (_lock)
            {
                _currentDialog = dialog;
            }

            try
            {
                var mainWindow = Application.Current.MainWindow;
                if (mainWindow != null && dialog is FrameworkElement element)
                {
                    element.DataContext = mainWindow.DataContext;
                }

                var result = await dialog.ShowAsync();
                
                lock (_lock)
                {
                    if (_currentDialog == dialog)
                    {
                        _currentDialog = null;
                    }
                }
                
                return result;
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("ContentDialog"))
            {
                lock (_lock)
                {
                    _currentDialog = null;
                }
                
                ForceCloseAllDialogs();
                await Task.Delay(500);
                throw new Exception("ContentDialog unavailable, using fallback");
            }
            catch (Exception ex)
            {
                lock (_lock)
                {
                    if (_currentDialog == dialog)
                    {
                        _currentDialog = null;
                    }
                }
                
                throw;
            }
        }

        public static async Task<ContentDialogResult> ShowMessageAsync(string title, string content, string closeButtonText = "关闭")
        {
            try
            {
                var dialog = new ContentDialog
                {
                    Title = title,
                    Content = content,
                    CloseButtonText = closeButtonText,
                    DefaultButton = ContentDialogButton.Close
                };

                return await ShowAsync(dialog);
            }
            catch
            {
                MessageBox.Show("显示对话框失败，请联系开发者", "ContentDialog组件错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return ContentDialogResult.None;
            }
        }

        public static async Task<ContentDialogResult> ShowConfirmationAsync(string title, string content, 
            string primaryButtonText = "确定", string closeButtonText = "取消")
        {
            try
            {
                var dialog = new ContentDialog
                {
                    Title = title,
                    Content = content,
                    PrimaryButtonText = primaryButtonText,
                    CloseButtonText = closeButtonText,
                    DefaultButton = ContentDialogButton.Primary
                };

                return await ShowAsync(dialog);
            }
            catch
            {
                MessageBox.Show("显示对话框失败，请联系开发者", "ContentDialog组件错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return ContentDialogResult.None;
            }
        }

        public static async Task<ContentDialogResult> ShowTextContentAsync(string title, string textContent,
            string primaryButtonText = null, string closeButtonText = "关闭", bool isReadOnly = true)
        {
            try
            {
                var textBox = new System.Windows.Controls.TextBox
                {
                    Text = textContent,
                    AcceptsReturn = true,
                    AcceptsTab = true,
                    TextWrapping = TextWrapping.Wrap,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    IsReadOnly = isReadOnly,
                    Padding = new Thickness(10, 0, 20, 0),
                    MaxHeight = 400,
                    MinHeight = 200,
                    Width = 500
                };

                var dialog = new ContentDialog
                {
                    Title = title,
                    Content = textBox,
                    CloseButtonText = closeButtonText,
                    DefaultButton = ContentDialogButton.Close
                };

                if (!string.IsNullOrEmpty(primaryButtonText))
                {
                    dialog.PrimaryButtonText = primaryButtonText;
                }

                return await ShowAsync(dialog);
            }
            catch
            {
                MessageBox.Show("显示对话框失败，请联系开发者", "ContentDialog组件错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return ContentDialogResult.None;
            }
        }

        public static void CloseCurrentDialog()
        {
            lock (_lock)
            {
                if (_currentDialog != null)
                {
                    try
                    {
                        _currentDialog.Hide();
                    }
                    catch
                    {
                        // Ignore errors
                    }
                    _currentDialog = null;
                }
            }
        }

        public static bool IsDialogOpen()
        {
            lock (_lock)
            {
                return _currentDialog != null;
            }
        }

        public static void ResetDialogState()
        {
            ForceCloseAllDialogs();
            EnsureMainWindowVisible();
        }

        private static void ForceCloseAllDialogs()
        {
            lock (_lock)
            {
                if (_currentDialog != null)
                {
                    try
                    {
                        _currentDialog.Hide();
                    }
                    catch
                    {
                        // Ignore errors
                    }
                    _currentDialog = null;
                }
            }

            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var mainWindow = Application.Current.MainWindow;
                    if (mainWindow != null)
                    {
                        var contentDialogs = FindVisualChildren<ContentDialog>(mainWindow);
                        foreach (var dialog in contentDialogs)
                        {
                            try
                            {
                                dialog.Hide();
                            }
                            catch
                            {
                                // Ignore errors
                            }
                        }
                    }
                });
            }
            catch
            {
                // Ignore errors during cleanup
            }
        }

        private static System.Collections.Generic.IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = System.Windows.Media.VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        private static void EnsureWindowStateMonitoring()
        {
            if (_isWindowStateMonitored) return;

            try
            {
                var mainWindow = Application.Current.MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.StateChanged += MainWindow_StateChanged;
                    _isWindowStateMonitored = true;
                }
            }
            catch
            {
                // Ignore errors during setup
            }
        }

        private static void MainWindow_StateChanged(object sender, EventArgs e)
        {
            var window = sender as Window;
            if (window != null && window.WindowState == WindowState.Minimized)
            {
                ForceCloseAllDialogs();
            }
        }

        private static void EnsureMainWindowVisible()
        {
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        // Store the current state before any modifications
                        var currentState = mainWindow.WindowState;
                        
                        // If window is minimized, restore it to its previous state
                        // We need to check if it was maximized before being minimized
                        if (mainWindow.WindowState == WindowState.Minimized)
                        {
                            // Try to restore to maximized if that was the previous state
                            // Check if the window was maximized by looking at its RestoreBounds
                            if (mainWindow.RestoreBounds.IsEmpty || 
                                (mainWindow.RestoreBounds.Width >= SystemParameters.PrimaryScreenWidth * 0.9 &&
                                 mainWindow.RestoreBounds.Height >= SystemParameters.PrimaryScreenHeight * 0.9))
                            {
                                mainWindow.WindowState = WindowState.Maximized;
                            }
                            else
                            {
                                mainWindow.WindowState = WindowState.Normal;
                            }
                        }

                        // Make sure the window is visible
                        if (!mainWindow.IsVisible)
                        {
                            mainWindow.Show();
                        }

                        // Bring window to front and activate it
                        mainWindow.Activate();
                        mainWindow.Focus();

                        // Force the window to be on top temporarily
                        var wasTopmost = mainWindow.Topmost;
                        mainWindow.Topmost = true;
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            mainWindow.Topmost = wasTopmost;
                        }), System.Windows.Threading.DispatcherPriority.Background);

                        // Alternative method to bring to front on Windows
                        try
                        {
                            var handle = new System.Windows.Interop.WindowInteropHelper(mainWindow).Handle;
                            if (handle != IntPtr.Zero)
                            {
                                SetForegroundWindow(handle);
                                // Only restore if the window is actually minimized
                                // SW_RESTORE (9) would change maximized windows to normal size
                                if (mainWindow.WindowState == WindowState.Minimized)
                                {
                                    ShowWindow(handle, 9); // SW_RESTORE
                                }
                                SetForegroundWindow(handle); // Call twice for better results
                            }
                        }
                        catch
                        {
                            // Fallback if P/Invoke fails
                        }
                    }
                    catch
                    {
                        // Ignore window management errors
                    }
                });
            }
        }



    }
} 
