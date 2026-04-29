using System.Net;
using System.Windows;

namespace SYSTools.Updater
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // 下载优化：提高并发连接数
            ServicePointManager.DefaultConnectionLimit = 12;
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            base.OnStartup(e);
        }
    }
}
