using AutoUpdaterDotNET;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using SYSTools.Dialog;

namespace SYSTools.Pages
{
    /// <summary>
    /// About.xaml 的交互逻辑
    /// </summary>
    public partial class About : Page
    {
        private static readonly HttpClient Client = new HttpClient();
        public About()
        {
            InitializeComponent();
        }

        private void QQ_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://jq.qq.com/?_wv=1027&k=qTaIu5Fa");
        }

        private void Web_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://systools.hksstudio.work");
        }

        private void Github_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/Hikarisame-Technology/SYSTools");
        }

        private void AutoUpdateVersion() {
            AutoUpdater.HttpUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36";
            AutoUpdater.UpdateFormSize = new System.Drawing.Size(800, 600);
            AutoUpdater.Start("https://systools.hksstudio.work/SYSTools_AutoUpdate.xml");
        }

        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            //软件本体检查更新
            HttpResponseMessage response = await Client.GetAsync("https://systools.hksstudio.work/SYSTools_Update_Version");
            response.EnsureSuccessStatusCode();
            string webCode = await response.Content.ReadAsStringAsync();

            Version currentVersion = System.Windows.Application.ResourceAssembly.GetName().Version;
            Version webVersion = new Version(webCode);

            if (currentVersion >= webVersion)
            {
                NoUpdate Dialog = new NoUpdate();
                await Dialog.ShowAsync().ConfigureAwait(false);
            }
            else
            {
                AutoUpdateVersion();
            }
        }

        private async void Tool_Update_Click(object sender, RoutedEventArgs e)
        {
            //工具包检查更新
            string ToolsExecutablePath = Path.Combine(Directory.GetCurrentDirectory(), "SYSTools_Update(Tools).exe");

            FileVersionInfo ToolsFileVersionInfo = FileVersionInfo.GetVersionInfo(ToolsExecutablePath);

            HttpResponseMessage response = await Client.GetAsync("https://systools.hksstudio.work/Tools_Update/Tools_Update_Version");
            response.EnsureSuccessStatusCode();
            string webCode = await response.Content.ReadAsStringAsync();

            if (ToolsFileVersionInfo.FileVersion == webCode)
            {
                NoUpdate Dialog = new NoUpdate();
                await Dialog.ShowAsync().ConfigureAwait(false);
            }
            else
            {
                Process.Start(ToolsExecutablePath);
            }
        }

        private void Agreement_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private async void Privacy_Click(object sender, RoutedEventArgs e)
        {
            // 从URL下载txt内容
            string url = "https://systools.hksstudio.work/Agree_Privacy/Privacy.txt";
            string txtContent = await GetTxtFromUrlAsync(url);

            // 创建并显示ContentDialog
            iNKORE.UI.WPF.Modern.Controls.ContentDialog dialog = new iNKORE.UI.WPF.Modern.Controls.ContentDialog
            {
                Title = "SYSTools 隐私协议",
                Content = new System.Windows.Controls.TextBox
                {
                    Text = txtContent,
                    AcceptsReturn = true,
                    AcceptsTab = true,
                    TextWrapping = TextWrapping.Wrap,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    IsReadOnly = true,
                    Padding = new Thickness(10, 0, 20, 0)
                },

                CloseButtonText = "关闭",
                PrimaryButtonText = "打开Url查看",
                DefaultButton = iNKORE.UI.WPF.Modern.Controls.ContentDialogButton.Close
            };

            var result = await dialog.ShowAsync();
            // 设定Url跳转地址
            if (result == iNKORE.UI.WPF.Modern.Controls.ContentDialogResult.Primary)
            {
                Process.Start(new ProcessStartInfo("https://systools.hksstudio.work/privacy.html") { UseShellExecute = true });
            }
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
    }
}
