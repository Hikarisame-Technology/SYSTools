using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using AutoUpdaterDotNET;
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

            Version currentVersion = Application.ResourceAssembly.GetName().Version;
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
    }
}
