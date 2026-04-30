using iNKORE.UI.WPF.TrayIcons;
using System.Net;
using System.Windows;
using SYSTools.Model;
using SYSTools.Helpers;

namespace SYSTools
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        public static TrayIcon TaskbarIcon;

        protected override void OnStartup(StartupEventArgs e)
        {
            // 提升并发连接数 (.NET Framework 默认为 2)
            ServicePointManager.DefaultConnectionLimit = 12;
            // 启用 TLS 1.2 支持（某些 API 要求）
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            base.OnStartup(e);

            // 恢复保存的语言设置
            string savedLanguage = AppSettings.Instance.Language;
            if (!string.IsNullOrEmpty(savedLanguage))
            {
                var culture = new System.Globalization.CultureInfo(savedLanguage);
                System.Threading.Thread.CurrentThread.CurrentCulture = culture;
                System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
                LocalizationManager.Instance.CurrentCulture = culture;
            }
            
            TaskbarIcon = (TrayIcon)FindResource("Taskbar");
            TaskbarIcon.ToolTipText = "SYSTools Ver" + (Application.ResourceAssembly.GetName().Version.ToString());
        }
    }
}
