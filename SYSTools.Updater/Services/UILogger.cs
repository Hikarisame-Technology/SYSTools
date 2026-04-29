using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace SYSTools.Updater.Services
{
    public class UILogger : ILogger
    {
        private readonly TextBlock _logTextBlock;
        private readonly TextBlock _statusText;
        private readonly ProgressBar _progressBar;

        public UILogger(TextBlock logTextBlock, TextBlock statusText, ProgressBar progressBar)
        {
            _logTextBlock = logTextBlock ?? throw new ArgumentNullException(nameof(logTextBlock));
            _statusText = statusText ?? throw new ArgumentNullException(nameof(statusText));
            _progressBar = progressBar ?? throw new ArgumentNullException(nameof(progressBar));
        }

        public void Log(string message)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() => Log(message));
                return;
            }

            _logTextBlock.Text += message + "\n";

            // 更新后自动滚动到底部
            try
            {
                if (_logTextBlock.Parent is ScrollViewer scrollViewer)
                {
                    scrollViewer.ScrollToVerticalOffset(scrollViewer.ExtentHeight);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UILogger] 自动滚动失败: {ex.Message}");
            }
        }

        public void LogError(string message)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() => LogError(message));
                return;
            }
            MessageBox.Show(message, "更新错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void UpdateStatus(string status)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() => UpdateStatus(status));
                return;
            }
            _statusText.Text = status;
            Log(status);
        }

        public void UpdateProgress(double value)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() => UpdateProgress(value));
                return;
            }
            
            value = Math.Max(0, Math.Min(100, value));
            _progressBar.IsIndeterminate = false;
            
            var animation = new DoubleAnimation
            {
                To = value,
                Duration = TimeSpan.FromMilliseconds(200),
                FillBehavior = FillBehavior.Stop
            };
            
            animation.Completed += (s, e) => _progressBar.Value = value;
            _progressBar.BeginAnimation(ProgressBar.ValueProperty, animation);
        }
    }
} 