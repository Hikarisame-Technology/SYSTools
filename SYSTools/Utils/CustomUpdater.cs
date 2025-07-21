using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;
using System.Net;
using iNKORE.UI.WPF.Modern.Controls;
using System.Windows;
using System.IO.Compression;
using System.Linq;
using iNKORE.UI.WPF.Modern.Common.IconKeys;
using System.Windows.Controls;
using SYSTools.Pages;
using SYSTools.Helpers;
using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;

namespace SYSTools.Utils
{
    public class CustomUpdater
    {
        private static readonly HttpClient Client = new HttpClient();
        
        public enum UpdateFileType
        {
            Executable,
            ZipPackage,
            Unknown
        }
        
        public class UpdateInfo
        {
            public Version Version { get; set; }
            public string DownloadUrl { get; set; }
            public string ReleaseNotes { get; set; }
            public long FileSize { get; set; }
            public UpdateFileType FileType { get; set; }
        }

        // 检查文件类型的方法
        private static UpdateFileType DetectFileType(byte[] fileHeader)
        {
            // ZIP文件头: 50 4B 03 04
            byte[] zipSignature = new byte[] { 0x50, 0x4B, 0x03, 0x04 };
            // EXE文件头: 4D 5A
            byte[] exeSignature = new byte[] { 0x4D, 0x5A };

            if (fileHeader.Take(4).SequenceEqual(zipSignature))
                return UpdateFileType.ZipPackage;
            else if (fileHeader.Take(2).SequenceEqual(exeSignature))
                return UpdateFileType.Executable;
            
            return UpdateFileType.Unknown;
        }

        public static async Task<UpdateInfo> CheckForUpdate(string versionUrl)
        {
            try
            {
                var response = await Client.GetAsync(versionUrl);
                response.EnsureSuccessStatusCode();
                var updateInfo = await response.Content.ReadAsStringAsync();
                
                // 现在只需要: 版本号|下载地址|更新说明|文件大小
                var parts = updateInfo.Split('|');
                
                // 获取文件头进行类型检测
                var fileResponse = await Client.GetAsync(parts[1], HttpCompletionOption.ResponseHeadersRead);
                var buffer = new byte[4];
                using (var stream = await fileResponse.Content.ReadAsStreamAsync())
                {
                    await stream.ReadAsync(buffer, 0, 4);
                }

                return new UpdateInfo
                {
                    Version = new Version(parts[0]),
                    DownloadUrl = parts[1],
                    ReleaseNotes = parts[2],
                    FileSize = long.Parse(parts[3]),
                    FileType = DetectFileType(buffer)
                };
            }
            catch (Exception ex)
            {
                throw new Exception("检查更新失败: " + ex.Message);
            }
        }

        public static async Task ShowUpdateDialog(UpdateInfo updateInfo)
        {
            if (updateInfo.FileType == UpdateFileType.Unknown)
            {
                await ContentDialogHelper.ShowMessageAsync(
                    "更新错误", 
                    "无法识别更新文件类型，更新已取消\n\n请前往Github提交Issue和联系开发者。"
                );
                return;
            }
            // 仅将 ReleaseNotes 设为可滚动区域，其他信息保持静态
            string sizeDisplay = FormatFileSize(updateInfo.FileSize);
            // 创建显示控件
            var headerText = new TextBlock
            {
                Text = $"🎉 发现新版本 v{updateInfo.Version}",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            var infoText = new TextBlock
            {
                Text = $"📦 文件大小：{sizeDisplay}  🔧 更新类型：{(updateInfo.FileType == UpdateFileType.Executable ? "可执行文件" : "压缩包")}",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            };
            var notesLabel = new TextBlock
            {
                Text = "📝 更新说明：",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 5)
            };
            var notesBox = new TextBox
            {
                Text = updateInfo.ReleaseNotes,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                IsReadOnly = true,
                MaxHeight = 200
            };
            var footerText = new TextBlock
            {
                Text = "是否立即下载并安装此更新？",
                Margin = new Thickness(0, 10, 0, 0)
            };
            var panel = new StackPanel();
            panel.Children.Add(headerText);
            panel.Children.Add(infoText);
            panel.Children.Add(notesLabel);
            panel.Children.Add(notesBox);
            panel.Children.Add(footerText);
            var dialog = new ContentDialog
            {
                Title = "发现新版本",
                Content = panel,
                PrimaryButtonText = "立即更新",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary
            };
            var result = await ContentDialogHelper.ShowAsync(dialog);

            if (result == ContentDialogResult.Primary)
            {
                await DownloadAndInstall(updateInfo);
            }
        }

        private static async Task DownloadAndInstall(UpdateInfo updateInfo)
        {
            Debug.WriteLine($"开始下载和安装: FileType={updateInfo.FileType}, URL={updateInfo.DownloadUrl}");
            
            // 根据更新类型和文件类型确定临时文件名
            string tempFileName;
            if (updateInfo.DownloadUrl.Contains("ToolsPack.zip"))
            {
                tempFileName = "ToolsPack.zip";
            }
            else
            {
                tempFileName = updateInfo.FileType == UpdateFileType.Executable ? 
                    "SYSTools_Update.exe" : "SYSTools_Update.zip";
            }

            var tempPath = Path.Combine(Path.GetTempPath(), tempFileName);
            Debug.WriteLine($"临时文件路径: {tempPath}");

            // 创建自定义下载窗口
            var progressWindow = new Window
            {
                Title = "📥 下载更新",
                Width = 450,
                Height = 220,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.CanMinimize,
                WindowStyle = WindowStyle.SingleBorderWindow,
                ShowInTaskbar = true,
                Topmost = false
            };

            // 创建窗口内容
            var mainGrid = new Grid
            {
                Margin = new Thickness(30, 20, 30, 20),
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = new GridLength(15) },
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = new GridLength(10) },
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = new GridLength(10) },
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = new GridLength(20) },
                    new RowDefinition { Height = GridLength.Auto }
                }
            };

            // 标题文本
            var titleText = new TextBlock
            {
                Text = "正在下载更新文件...",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // 进度条
            var progressBar = new iNKORE.UI.WPF.Modern.Controls.ProgressBar
            {
                IsIndeterminate = false,
                Minimum = 0,
                Maximum = 100,
                Height = 10,
                CornerRadius = new CornerRadius(5)
            };

            // 进度文本
            var statusText = new TextBlock
            {
                Text = "准备下载...",
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                FontSize = 12,
                Opacity = 0.8
            };

            // 速度文本
            var speedText = new TextBlock
            {
                Text = "",
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                FontSize = 11,
                Opacity = 0.6
            };

            // 按钮面板
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var cancelButton = new Button
            {
                Content = "取消下载",
                Width = 100,
                Height = 32,
                IsEnabled = true
            };

            buttonPanel.Children.Add(cancelButton);

            // 添加控件到网格
            mainGrid.Children.Add(titleText);
            mainGrid.Children.Add(progressBar);
            mainGrid.Children.Add(statusText);
            mainGrid.Children.Add(speedText);
            mainGrid.Children.Add(buttonPanel);

            Grid.SetRow(titleText, 0);
            Grid.SetRow(progressBar, 2);
            Grid.SetRow(statusText, 4);
            Grid.SetRow(speedText, 6);
            Grid.SetRow(buttonPanel, 8);

            progressWindow.Content = mainGrid;

            // WebClient 用于下载
            WebClient webClient = null;
            bool downloadCancelled = false;
            bool downloadCompleted = false;
            Exception downloadException = null; // 统一保存异常信息

            // 取消按钮事件
            cancelButton.Click += (s, e) =>
            {
                downloadCancelled = true;
                webClient?.CancelAsync();
                progressWindow.Close();
            };

            // 窗口关闭事件
            progressWindow.Closing += (s, e) =>
            {
                // 只有在下载未完成且用户主动关闭时才取消下载
                if (!downloadCompleted && !downloadCancelled && webClient != null)
                {
                    downloadCancelled = true;
                    webClient.CancelAsync();
                }
            };

            try
            {
                webClient = new WebClient();
                var lastBytes = 0L;
                var lastTime = DateTime.Now;
                var updateInterval = TimeSpan.FromSeconds(1);

                webClient.DownloadProgressChanged += (s, e) =>
                {
                    if (downloadCancelled) return;

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        progressBar.Value = e.ProgressPercentage;
                        
                        var now = DateTime.Now;
                        if (now - lastTime >= updateInterval)
                        {
                            var bytesChange = e.BytesReceived - lastBytes;
                            var speed = bytesChange / (now - lastTime).TotalSeconds;
                            
                            string speedText_value;
                            if (speed >= 1024 * 1024) // MB/s
                            {
                                speedText_value = $"下载速度：{speed / 1024 / 1024:F2} MB/s";
                            }
                            else if (speed >= 1024) // KB/s
                            {
                                speedText_value = $"下载速度：{speed / 1024:F2} KB/s";
                            }
                            else // B/s
                            {
                                speedText_value = $"下载速度：{speed:F0} B/s";
                            }

                            statusText.Text = $"已下载：{FormatFileSize(e.BytesReceived)} / {FormatFileSize(e.TotalBytesToReceive)} ({e.ProgressPercentage}%)";
                            speedText.Text = speedText_value;

                            lastBytes = e.BytesReceived;
                            lastTime = now;
                        }
                    });
                };

                // 使用TaskCompletionSource来统一处理异步完成
                var downloadTask = new TaskCompletionSource<bool>();

                webClient.DownloadFileCompleted += (s, e) =>
                {
                    downloadCompleted = true;
                    
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        progressWindow.Close();
                    });

                    if (downloadCancelled)
                    {
                        downloadTask.SetCanceled();
                        return;
                    }

                    if (e.Error != null)
                    {
                        downloadException = e.Error;
                        downloadTask.SetException(e.Error);
                    }
                    else if (e.Cancelled)
                    {
                        downloadTask.SetCanceled();
                    }
                    else
                    {
                        downloadTask.SetResult(true);
                    }
                };

                // 显示窗口并开始下载
                progressWindow.Show();
                
                // 开始下载并等待完成
                webClient.DownloadFileAsync(new Uri(updateInfo.DownloadUrl), tempPath);
                await downloadTask.Task;

                // 下载成功后处理文件
                await ProcessDownloadedFile(updateInfo, tempPath);
            }
            catch (OperationCanceledException)
            {
                // 用户取消，不显示错误
                Debug.WriteLine("下载被用户取消");
            }
            catch (Exception ex)
            {
                if (progressWindow.IsVisible)
                {
                    progressWindow.Close();
                }

                if (!downloadCancelled)
                {
                    // 统一的错误处理 - 添加owner参数
                    string errorMessage = GetFriendlyErrorMessage(ex);
                    MessageBox.Show(
                        Application.Current.MainWindow, // 添加owner参数
                        errorMessage + "\n\n请前往Github提交Issue和联系开发者。",
                        "❌ 下载失败",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }
            }
            finally
            {
                webClient?.Dispose();
            }
        }

        private static async Task ProcessDownloadedFile(UpdateInfo updateInfo, string tempPath)
        {
            // 下载完成后再次验证文件类型
            byte[] fileHeader = new byte[4];
            using (var fs = File.OpenRead(tempPath))
            {
                await fs.ReadAsync(fileHeader, 0, 4);
            }
            var actualFileType = DetectFileType(fileHeader);
            Debug.WriteLine($"下载文件类型检测: 期望类型={updateInfo.FileType}, 实际类型={actualFileType}");

            if (actualFileType != updateInfo.FileType)
            {
                throw new InvalidOperationException("更新文件类型验证失败，文件可能已损坏或不是有效的更新包。");
            }

            if (updateInfo.FileType == UpdateFileType.Executable)
            {
                Debug.WriteLine("检测到可执行文件更新，准备重启程序");
                // 显示即将重启的提示 - 添加owner参数
                MessageBox.Show(
                    Application.Current.MainWindow, // 添加owner参数
                    "更新文件下载完成，程序将自动重启以完成更新。",
                    "✅ 下载完成",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
                
                Process.Start(tempPath);
                Application.Current.Shutdown();
            }
            else
            {
                Debug.WriteLine("检测到ZIP包更新，准备调用 InstallZipUpdate");
                Debug.WriteLine($"开始调用 InstallZipUpdate: {tempPath}");
                await InstallZipUpdate(tempPath);
                Debug.WriteLine("InstallZipUpdate 调用完成");
            }
        }

        private static string GetFriendlyErrorMessage(Exception ex)
        {
            return ex switch
            {
                System.Net.WebException webEx when webEx.Status == System.Net.WebExceptionStatus.NameResolutionFailure => 
                    "无法连接到更新服务器，请检查网络连接。",
                
                System.Net.WebException webEx when webEx.Status == System.Net.WebExceptionStatus.Timeout => 
                    "下载超时，请检查网络连接或稍后重试。",
                
                System.Net.WebException webEx when webEx.Status == System.Net.WebExceptionStatus.ConnectFailure => 
                    "无法连接到服务器，请检查网络设置。",
                
                UnauthorizedAccessException => 
                    "权限不足，请以管理员身份运行程序。",
                
                DirectoryNotFoundException => 
                    "目标目录不存在，请检查程序安装路径。",
                
                IOException ioEx when ioEx.Message.Contains("space") => 
                    "磁盘空间不足，请清理磁盘空间后重试。",
                
                InvalidOperationException => 
                    ex.Message,
                
                _ => $"下载更新时发生错误：\n{ex.Message}\n\n请检查网络连接或稍后重试。"
            };
        }

        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double size = bytes;
            
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size = size / 1024;
            }

            return $"{size:F2} {sizes[order]}";
        }

        private static async Task InstallZipUpdate(string zipPath)
        {
            try
            {
                string appPath = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
                string updaterPath = Path.Combine(appPath, "SYSTools.Updater.exe");
                string tempUpdaterPath = Path.Combine(Path.GetTempPath(), "SYSTools.Updater.exe");

                // 如果是工具包更新，确保目标目录存在
                if (zipPath.Contains("ToolsPack.zip"))
                {
                    string softwarePackagePath = Path.Combine(appPath, "Software Package");
                    if (!Directory.Exists(softwarePackagePath))
                    {
                        Directory.CreateDirectory(softwarePackagePath);
                    }
                }

                // 复制更新器到临时目录
                if (File.Exists(updaterPath))
                {
                    File.Copy(updaterPath, tempUpdaterPath, true);
                }
                else
                {
                    MessageBox.Show(
                        Application.Current.MainWindow, // 添加owner参数
                        "找不到更新器程序文件。\n\n请确保 SYSTools.Updater.exe 文件存在于程序目录中，\n或前往Github提交Issue和联系开发者。",
                        "❌ 更新失败",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                    return;
                }

                // 清理和准备参数
                string cleanZipPath = zipPath.Trim('"').TrimEnd('\\');
                string cleanAppPath = appPath.Trim('"').TrimEnd('\\');
                string updateType = cleanZipPath.Contains("ToolsPack.zip") ? "toolkit" : "software";

                // 构建参数数组并连接
                string[] arguments = new[]
                {
                    $"\"{cleanZipPath}\"",
                    $"\"{cleanAppPath}\"",
                    updateType
                };

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = tempUpdaterPath,
                    Arguments = string.Join(" ", arguments),
                    UseShellExecute = true
                };

                Process.Start(startInfo);

                // 如果是软件更新，显示提示并关闭当前进程
                if (updateType == "software") 
                { 
                    MessageBox.Show(
                        Application.Current.MainWindow, // 添加owner参数
                        "程序将在更新完成后自动重启。\n请稍等片刻...",
                        "🔄 正在更新",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                    Application.Current.Shutdown();
                }
                else
                {
                    MessageBox.Show(
                        Application.Current.MainWindow, // 添加owner参数
                        "工具包正在更新，请稍等片刻...\n待工具解压完成",
                        "🔄 工具包正在更新",
                        MessageBoxButton.OK, 
                        MessageBoxImage.Information
                    );
                }
                
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    Application.Current.MainWindow, // 添加owner参数
                    $"启动更新程序时发生错误：\n{ex.Message}\n\n请稍后重试，或前往Github提交Issue和联系开发者。",
                    "❌ 更新失败",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }
    }
} 