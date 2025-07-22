using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using SYSTools.Updater.Services;
using SYSTools.Updater.Utils;

namespace SYSTools.Updater
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly ThemeService _themeService;
        private ILogger _logger;
        private UpdateService _updateService;
        private bool _isDarkMode;
        private string _updateTitle;

        public bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                if (_isDarkMode != value)
                {
                    _isDarkMode = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsDarkMode)));
                }
            }
        }

        public string UpdateTitle
        {
            get => _updateTitle;
            set
            {
                if (_updateTitle != value)
                {
                    _updateTitle = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UpdateTitle)));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            
            _themeService = new ThemeService();
            _themeService.ThemeChanged += OnThemeChanged;
            IsDarkMode = _themeService.IsDarkMode;

            // 使用Loaded事件来确保UI元素完全初始化
            this.Loaded += MainWindow_Loaded;
        }

        private void OnThemeChanged(bool isDarkMode)
        {
            IsDarkMode = isDarkMode;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 使用Dispatcher延迟执行，确保UI完全渲染完成
            Dispatcher.BeginInvoke(new Action(() =>
            {
                InitializeWithRetry(0);
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void InitializeWithRetry(int retryCount)
        {
            const int maxRetries = 3;
            
            try
            {
                // 设置进度条为不确定状态
                if (UpdateProgress != null)
                {
                    UpdateProgress.IsIndeterminate = true;
                }

                // 确保UI元素完全初始化后再创建UILogger
                if (LogTextBlock == null || StatusText == null || UpdateProgress == null)
                {
                    if (retryCount < maxRetries)
                    {
                        // 重试，延迟更长时间
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            InitializeWithRetry(retryCount + 1);
                        }), System.Windows.Threading.DispatcherPriority.Background);
                        return;
                    }
                    else
                    {
                        // 所有重试都失败，使用备用logger
                        _logger = new FallbackLogger();
                        MessageBox.Show("UI初始化失败，使用备用模式继续更新", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    _logger = new UILogger(LogTextBlock, StatusText, UpdateProgress);
                }
                
                InitializeUpdateService();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"更新器初始化失败: {ex.Message}", "初始化错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        // 备用Logger实现，当UI初始化失败时使用
        private class FallbackLogger : ILogger
        {
            public void Log(string message)
            {
                Console.WriteLine($"[LOG] {message}");
                System.Diagnostics.Debug.WriteLine($"[LOG] {message}");
            }

            public void LogError(string message)
            {
                Console.WriteLine($"[ERROR] {message}");
                System.Diagnostics.Debug.WriteLine($"[ERROR] {message}");
                MessageBox.Show(message, "更新错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            public void UpdateStatus(string status)
            {
                Console.WriteLine($"[STATUS] {status}");
                System.Diagnostics.Debug.WriteLine($"[STATUS] {status}");
            }

            public void UpdateProgress(double value)
            {
                Console.WriteLine($"[PROGRESS] {value:F1}%");
                System.Diagnostics.Debug.WriteLine($"[PROGRESS] {value:F1}%");
            }
        }

        private void InitializeUpdateService()
        {
            try
            {
                if (_logger == null)
                {
                    throw new InvalidOperationException("Logger未正确初始化");
                }

                var args = Environment.GetCommandLineArgs();
                if (args.Length < 4)
                {
                    ShowError($"参数不足: 需要4个参数，当前只有{args.Length}个参数");
                    LogCommandLineArgs(args);
                    return;
                }

                string zipPath = FileUtils.CleanPath(args[1].Trim('"'));
                string targetPath = FileUtils.CleanPath(args[2].Trim('"'));
                bool isToolkitUpdate = args[3].Trim('"').Equals("toolkit", StringComparison.OrdinalIgnoreCase);

                UpdateTitle = isToolkitUpdate ? "正在更新工具包..." : "正在更新 SYSTools...";

                _logger.Log($"更新类型: {(isToolkitUpdate ? "工具包更新" : "软件更新")}");
                _logger.Log($"更新包路径: {zipPath}");
                _logger.Log($"目标路径: {targetPath}");

                _updateService = new UpdateService(zipPath, targetPath, isToolkitUpdate, _logger);
                StartUpdate();
            }
            catch (Exception ex)
            {
                ShowError($"初始化更新服务失败: {ex.Message}");
            }
        }

        private async void StartUpdate()
        {
            try
            {
                await _updateService.StartUpdate();
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void LogCommandLineArgs(string[] args)
        {
            if (_logger != null)
            {
                _logger.Log("参数列表:");
                for (int i = 0; i < args.Length; i++)
                {
                    _logger.Log($"参数[{i}]: {args[i]}");
                }
            }
        }

        private void ShowError(string message)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => ShowError(message));
                return;
            }
            MessageBox.Show(message, "更新错误", MessageBoxButton.OK, MessageBoxImage.Error);
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _themeService.Cleanup();
            
            // 清理事件处理器
            this.Loaded -= MainWindow_Loaded;
        }
    }
} 