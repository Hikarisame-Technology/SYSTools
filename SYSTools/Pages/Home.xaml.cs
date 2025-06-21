using System;
using System.Diagnostics;
using System.Management;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Media.Animation;

namespace SYSTools.Pages
{
    /// <summary>
    /// Home.xaml 的交互逻辑
    /// </summary>
    public partial class Home : Page
    {
        static readonly HttpClient client = new HttpClient();
        DateTime UPTime = DateTime.Now.AddMilliseconds(-(Environment.TickCount));
        private List<string> notices = new List<string>();
        private int currentNoticeIndex = 0;
        private DispatcherTimer noticeTimer;
        private DispatcherTimer mainTimer; // 将计时器作为类成员
        
        // 缓存本地化字符串和语言判断结果
        private string dayUnit;
        private string hourUnit; 
        private string minuteUnit;
        private string secondUnit;
        private bool isChineseLanguage;

        public Home()
        {
            InitializeComponent();
            
            // 初始化主计时器
            mainTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            mainTimer.Tick += Timer_Tick;
            
            // 初始化公告计时器
            noticeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            noticeTimer.Tick += NoticeTimer_Tick;
            
            // 注册Unloaded事件
            this.Unloaded += Home_Unloaded;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // 计时器
            mainTimer?.Stop();
            RefreshLanguageCache();
            UpdateTimeDisplay();
            mainTimer.Start();

            isChineseLanguage = System.Globalization.CultureInfo.CurrentUICulture.Name.StartsWith("zh");

            if (isChineseLanguage)
            {
                OpenTime.Text = UPTime.ToLongDateString();
            }
            else
            {
                RunTime.Text = UPTime.ToLongDateString();
            }

            //欢迎头部文本
            Home_SP_Tip.ToolTip = Properties.Lang.ResourceManager.GetString("Hello", System.Globalization.CultureInfo.CurrentUICulture) + Convert.ToChar(32) + Environment.UserName;


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
                Hitokoto.Text = "！- " + Properties.Lang.ResourceManager.GetString("NetError", System.Globalization.CultureInfo.CurrentUICulture) + " - ！";
            }

            if (Hitokoto.Text == "！- " + Properties.Lang.ResourceManager.GetString("NetError", System.Globalization.CultureInfo.CurrentUICulture) + " - ！")
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

            // 获取公告
            try
            {
                // 根据当前语言选择公告URL
                string noticeUrl = System.Globalization.CultureInfo.CurrentUICulture.Name.StartsWith("zh") 
                    ? "http://systools.hksstudio.work/PublicNotice"          // 中文公告 Notice变更News但Url不修改
                    : "http://systools.hksstudio.work/PublicNotice_EN";      // 英文公告

                HttpResponseMessage response = await client.GetAsync(noticeUrl);
                response.EnsureSuccessStatusCode();
                string noticeContent = await response.Content.ReadAsStringAsync();
                
                // 按行分割公告内容
                notices = noticeContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                                     .Select(n => n.Trim())
                                     .Where(n => !string.IsNullOrEmpty(n))
                                     .ToList();

                if (notices.Count > 0)
                {
                    string firstNotice = notices[0];
                    PublicNews.Text = firstNotice;
                    
                    if (notices.Count > 1)
                    {
                        // 设置初始间隔
                        noticeTimer.Interval = firstNotice.Length > 15 ? 
                            TimeSpan.FromSeconds(5) : TimeSpan.FromSeconds(3);
                        noticeTimer.Start();
                    }
                }
                else
                {
                    PublicNews.Text = Properties.Lang.ResourceManager.GetString("NoticeInfo", System.Globalization.CultureInfo.CurrentUICulture);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取公告失败: {ex}");
                PublicNews.Text = Properties.Lang.ResourceManager.GetString("NoticeError", System.Globalization.CultureInfo.CurrentUICulture);
            }
        }

        private void RefreshLanguageCache()
        {
            isChineseLanguage = System.Globalization.CultureInfo.CurrentUICulture.Name.StartsWith("zh");
            dayUnit = Properties.Lang.ResourceManager.GetString("TimeUnitDay", System.Globalization.CultureInfo.CurrentUICulture);
            hourUnit = Properties.Lang.ResourceManager.GetString("TimeUnitHour", System.Globalization.CultureInfo.CurrentUICulture);
            minuteUnit = Properties.Lang.ResourceManager.GetString("TimeUnitMinute", System.Globalization.CultureInfo.CurrentUICulture);
            secondUnit = Properties.Lang.ResourceManager.GetString("TimeUnitSecond", System.Globalization.CultureInfo.CurrentUICulture);
        }

        private void UpdateTimeDisplay()
        {
            TimeSpan Nows = DateTime.Now - UPTime;
            string RunTime_ = $"{Nows.Days} {dayUnit} {Nows.Hours} {hourUnit} {Nows.Minutes} {minuteUnit} {Nows.Seconds} {secondUnit}";
            
            // 清空两个控件，然后只更新目标控件（避免闪烁）
            if (isChineseLanguage)
            {
                RunTime.Text = RunTime_;
            }
            else
            {
                OpenTime.Text = RunTime_;
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateTimeDisplay();
            CommandManager.InvalidateRequerySuggested();
        }

        private void NoticeTimer_Tick(object sender, EventArgs e)
        {
            if (notices.Count > 1)
            {
                currentNoticeIndex = (currentNoticeIndex + 1) % notices.Count;
                PublicNews.Opacity = 0;
                string nextNotice = notices[currentNoticeIndex];
                PublicNews.Text = nextNotice;
                
                // 根据文本长度调整显示时间
                int textLength = nextNotice.Length;
                TimeSpan interval = textLength > 15 ? TimeSpan.FromSeconds(5) : TimeSpan.FromSeconds(3);
                noticeTimer.Interval = interval;

                PublicNews.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(500)));
            }
        }

        private void Home_Unloaded(object sender, RoutedEventArgs e)
        {
            mainTimer?.Stop();
            noticeTimer?.Stop();
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
                Debug.WriteLine(IPv4Error);
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
                Debug.WriteLine(IPv6Error);
            }
        }
    }
}
