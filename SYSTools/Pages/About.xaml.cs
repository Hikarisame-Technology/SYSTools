using iNKORE.UI.WPF.Modern.Common.IconKeys;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SYSTools.Utils;
using SYSTools.Helpers;
using iNKORE.UI.WPF.Modern.Controls;
using System.Windows.Threading;

namespace SYSTools.Pages
{
    /// <summary>
    /// About.xaml 的交互逻辑
    /// </summary>
    public partial class About : System.Windows.Controls.Page
    {
        private static readonly HttpClient Client = new HttpClient();
        public About()
        {
            InitializeComponent();
            DataContext = this;
            
            var libraries = new List<Library>
            {
                new Library { Name = "iNKORE.UI.WPF (LGPL-2.1 License)", Uri = "https://github.com/iNKORE-NET/UI.WPF" },
                new Library { Name = "iNKORE.UI.WPF.Modern (LGPL-2.1 License)", Uri = "https://github.com/iNKORE-Public/UI.WPF.Modern" },
                new Library { Name = "LibreHardwareMonitorLib (MPL-2.0 License)", Uri = "https://github.com/LibreHardwareMonitor/LibreHardwareMonitor" },
                new Library { Name = "Log4Net (Apache-2.0 License)", Uri = "https://logging.apache.org/log4net/" },
                new Library { Name = "Microsoft.Windows.SDK.Contracts", Uri = "https://aka.ms/WinSDKProjectURL" },
                new Library { Name = "System.Runtime.WindowsRuntime", Uri = "https://github.com/dotnet/corefx" },
                new Library { Name = "System.Runtime.WindowsRuntime.UI.Xaml", Uri = "https://github.com/dotnet/corefx" },
                new Library { Name = "System.ValueTuple", Uri = "https://dot.net" },
                new Library { Name = "TextCopy", Uri = "https://github.com/CopyText/TextCopy" },
                new Library { Name = ".NET Runtime", Uri = "https://github.com/dotnet/runtime" },
                new Library { Name = "HidSharp", Uri = "http://www.zer7.com/software/hidsharp" },
                new Library { Name = "Microsoft.Bcl.AsyncInterfaces", Uri = "https://dot.net/" },
                new Library { Name = "Microsoft.Extensions.DependencyInjection.Abstractions", Uri = "https://dot.net/" },
                new Library { Name = "Microsoft.Web.WebView2", Uri = "https://aka.me/webview" },
                new Library { Name = "System.CodeDom", Uri = "https://dot.net/" },
                new Library { Name = "System.Management", Uri = "https://dot.net/" },
                new Library { Name = "System.Runtime.CompilerServices.Unsafe", Uri = "https://dot.net/" },
                new Library { Name = "System.Runtime.InteropServices.RuntimeInformation", Uri = "https://dot.net/" },
                new Library { Name = "System.Runtime.InteropServices.WindowsRuntime", Uri = "https://dot.net/" },
                new Library { Name = "System.Threading.Tasks.Extensions", Uri = "https://dot.net/" }
            };
            
            librariesItemsControl.ItemsSource = libraries;
        }

        private void QQ_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://jq.qq.com/?_wv=1027&k=qTaIu5Fa");
        }

        private void Web_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://systools.hksstudio.work");
        }

        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ContentDialogHelper.ResetDialogState();
                
                var currentVersion = System.Windows.Application.ResourceAssembly.GetName().Version;
                var updateInfo = await CustomUpdater.CheckForUpdate("https://systools.hksstudio.work/SYSTools_Update_Version");
                
                if (currentVersion >= updateInfo.Version)
                {
                    iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("暂无更新🤐", "暂未获取更新");
                }
                else
                {
                    await CustomUpdater.ShowUpdateDialog(updateInfo);
                }
            }
            catch (Exception ex)
            {
                await ContentDialogHelper.ShowMessageAsync("检查更新失败", ex.Message);
            }
        }

        private Version GetToolkitVersion()
        {
            try
            {
                string toolkitPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Software Package", "Version");
                if (File.Exists(toolkitPath))
                {
                    string versionText = File.ReadAllText(toolkitPath);
                    if (Version.TryParse(versionText.Trim(), out Version version))
                    {
                        return version;
                    }
                }
                return new Version(0, 0, 0, 0); // 如果文件不存在或解析失败，返回0.0.0.0
            }
            catch
            {
                return new Version(0, 0, 0, 0);
            }
        }

        private async void Tool_Update_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Reset dialog state to ensure no stuck dialogs
                ContentDialogHelper.ResetDialogState();
                
                var currentVersion = GetToolkitVersion();
                var updateInfo = await CustomUpdater.CheckForUpdate("https://systools.hksstudio.work/Tools_Update/Tools_Update_Version");

                if (currentVersion >= updateInfo.Version)
                {
                    iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("暂无更新🤐", "暂未获取更新");
                }
                else
                {
                    await CustomUpdater.ShowUpdateDialog(updateInfo);
                }
            }
            catch (Exception ex)
            {
                iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("检查更新失败", ex.Message);
            }
        }

        private async void Privacy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 从URL下载txt内容
                string url = "https://systools.hksstudio.work/Agree_Privacy/Privacy.txt";
                string txtContent = await GetTxtFromUrlAsync(url);

                // 使用ContentDialogHelper显示文本内容对话框
                var result = await ContentDialogHelper.ShowTextContentAsync(
                    "SYSTools 隐私协议",
                    txtContent,
                    "打开Url查看",
                    "关闭"
                );

                // 设定Url跳转地址
                if (result == iNKORE.UI.WPF.Modern.Controls.ContentDialogResult.Primary)
                {
                    Process.Start(new ProcessStartInfo("https://systools.hksstudio.work/privacy") { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                // 最后的备用方案 - 直接使用 MessageBox
                try
                {
                    System.Windows.MessageBox.Show($"加载隐私协议失败: {ex.Message}\n\n点击确定打开网页查看", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    Process.Start(new ProcessStartInfo("https://systools.hksstudio.work/privacy") { UseShellExecute = true });
                }
                catch (Exception ex2)
                {
                    // Ignore final fallback errors
                }
            }
        }

        private void Agreement_Click(object sender, RoutedEventArgs e)
        {

        }

        // 获取txt文件内容，GB2312编码
        private async Task<string> GetTxtFromUrlAsync(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                byte[] bytes = await client.GetByteArrayAsync(url);
                return Encoding.GetEncoding("GB2312").GetString(bytes);
            }
        }

        private void OnCardClicked_Repository(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://github.com/Hikarisame-Technology/SYSTools") { UseShellExecute = true });
        }

        private void OnCardClicked_Issue(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://github.com/Hikarisame-Technology/SYSTools/issues") { UseShellExecute = true });
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateVersion.Text = Application.ResourceAssembly.GetName().Version.ToString();
        }

        private void SYSTools_SettingsExpander_Loaded(object sender, RoutedEventArgs e)
        {
            var expander = sender as SettingsExpander;
            expander.Dispatcher.BeginInvoke(new Action(() => expander.IsExpanded = true), DispatcherPriority.Loaded);
        }

        private void Update_SettingsExpander_Loaded(object sender, RoutedEventArgs e)
        {
            var expander = sender as SettingsExpander;
            expander.Dispatcher.BeginInvoke(new Action(() => expander.IsExpanded = true), DispatcherPriority.Loaded);
        }
    }

    public class Library
    {
        public string Name { get; set; }
        public string Uri { get; set; }
    }
}
