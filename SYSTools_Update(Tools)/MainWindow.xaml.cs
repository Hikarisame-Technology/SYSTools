using AutoUpdaterDotNET;
using System;
using System.Net.Http;
using System.Windows;

namespace SYSTools_Update_Tools_
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly HttpClient Client = new HttpClient();
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                HttpResponseMessage response = await Client.GetAsync("https://systools.hksstudio.work/Tools_Update/Tools_Update_Version");
                response.EnsureSuccessStatusCode();
                string webCode = await response.Content.ReadAsStringAsync();

                Version.Text = "当前本地版本为: " + (Application.ResourceAssembly.GetName().Version.ToString());
                Cloud_Version.Text = "云端版本为: " + webCode;

                if (Application.ResourceAssembly.GetName().Version.ToString() == webCode)
                {
                    Version_Verify.Text = "暂无更新";
                }
                else
                {
                    Version_Verify.Text = "有新版本 等待获取更新..";
                    AutoUpdateVersion();
                }
            }
            catch (HttpRequestException)
            {
                Cloud_Version.Text = "！- 网络或云端出现问题 - ！";
            }
        }

        private void AutoUpdateVersion()
        {
            AutoUpdater.HttpUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/88.0.4324.104 Safari/537.36";
            AutoUpdater.Start("https://systools.hksstudio.work/Tools_Update/Tools_AutoUpdate.xml");
            AutoUpdater.UpdateFormSize = new System.Drawing.Size(800, 600);
            Version_Verify.Text = "已获取更新 若不小心关闭请关闭该窗口重新启动";
        
        }
    }

}
