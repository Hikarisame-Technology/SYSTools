using System;
using System.Management;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace SYSTools.Pages
{
    /// <summary>
    /// Home.xaml 的交互逻辑
    /// </summary>
    public partial class Home : Page
    {
        static readonly HttpClient client = new HttpClient();
        DateTime UPTime = DateTime.Now.AddMilliseconds(-(Environment.TickCount));

        public Home()
        {
            InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            OpenTime.Text = UPTime.ToLongDateString();

            //欢迎头部文本
            Home_SP_Tip.ToolTip = "您好 :" + Convert.ToChar(32) + Environment.UserName;


            //一言卡片获取
            try
            {
                System.Net.Http.HttpResponseMessage response = await client.GetAsync("https://v1.hitokoto.cn/?c=b&c=a&encode=text");
                response.EnsureSuccessStatusCode();
                string webCode = await response.Content.ReadAsStringAsync();
                Hitokoto.Text = webCode;
            }
            catch (Exception)
            {
                Hitokoto.Text = "！- 网络或网站出现问题 - ！";
            }

            if (Hitokoto.Text == "！- 网络或网站出现问题 - ！")
            {
                IPv4.Text = "!";
                IPv6.Text = "!";
            }

            // 获取Windows系统版本与版本号
            ManagementObjectSearcher OpSystem = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
            ManagementObjectCollection OpS = OpSystem.Get();
            foreach (ManagementObject OpSys in OpS)
            {
                Windows_Name.Text = OpSys.GetPropertyValue("Caption").ToString();
                Windows_Version.Text = OpSys.GetPropertyValue("Version").ToString();
            }

            // 计时器
            DispatcherTimer Timer = new DispatcherTimer();
            Timer.Tick += Timer_Tick;
            Timer.Interval = new TimeSpan(0, 0, 1);
            Timer.Start();

        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            // 启动时间获取并更新
            TimeSpan Nows = DateTime.Now - UPTime;
            string RunTime_ = Nows.Days + " 天 " + Nows.Hours + " 时 " + Nows.Minutes + " 分 " + Nows.Seconds + " 秒 ";
            RunTime.Text = RunTime_;
            CommandManager.InvalidateRequerySuggested();
        }

        private void IPv4_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            IPv4_Info();
        }

        private void IPv4_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            IPv4.Text = "***.***.***.***";
        }

        private void IPv6_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            IPv6_Info();
        }

        private void IPv6_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            IPv6.Text = "****:****:****:****:****:****:****:****";
        }

        private async void IPv4_Info()
        {
            try
            {
                this.IPv4.Text = "Now Loading...";
                HttpResponseMessage IPv4Test = await client.GetAsync("https://myip.ipip.net/");
                IPv4Test.EnsureSuccessStatusCode();
                string IPv4 = await IPv4Test.Content.ReadAsStringAsync();
                this.IPv4.Text = Regex.Replace(IPv4, "[\r\n]", "");
            }
            catch (Exception IPv4Error)
            {
                IPv4.Text = "当前网络可能没有IPV4地址或获取失败";
            }
        }

        private async void IPv6_Info()
        {
            try
            {
                this.IPv6.Text = "Now Loading...";
                HttpResponseMessage IPv6Test = await client.GetAsync("https://speed.neu6.edu.cn/getIP.php");
                IPv6Test.EnsureSuccessStatusCode();
                string IPv6 = await IPv6Test.Content.ReadAsStringAsync();
                this.IPv6.Text = "当前IPv6地址 : " + IPv6;
            }
            catch (Exception IPv6Error)
            {
                IPv6.Text = "当前网络可能没有IPV6地址或获取失败";
            }
        }
    }
}
