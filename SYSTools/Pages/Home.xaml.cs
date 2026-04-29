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
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Threading.Tasks;
using System.Windows.Media.Effects;
using System.Windows.Controls.Primitives;
using System.ComponentModel;
using SYSTools.Helpers;
using SYSTools.Services;
using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;

namespace SYSTools.Pages
{
    /// <summary>
    /// Home.xaml 的交互逻辑
    /// </summary>
    public partial class Home : Page
    {
        static readonly HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        DateTime UPTime = DateTime.Now.AddMilliseconds(-(Environment.TickCount));
        private List<string> notices = new List<string>();
        private int currentNoticeIndex = 0;
        private DispatcherTimer noticeTimer;
        private DispatcherTimer mainTimer;
        // 资源监控使用 mainTimer 每秒统一刷新
        
        // 缓存本地化字符串
        private string dayUnit, hourUnit, minuteUnit, secondUnit;
        private bool isChineseLanguage;

        // 资源监控相关
        private PerformanceCounter cpuCounter;
        private PerformanceCounter ramCounter;
        private HardwareMonitorService hardwareService;
        private TextBlock cpuValueText, memValueText, diskValueText, gpuValueText;
        private Border cpuProgressBar, memProgressBar, diskProgressBar, gpuProgressBar;
        private TextBlock cpuTempText, gpuTempText; // 温度显示文本
        private List<Border> allCards; // 缓存卡片列表
        private bool isLanguageEventSubscribed = false; // 标记是否已订阅语言变化事件
        private bool isInitialized = false; // 标记页面是否已初始化

        public Home()
        {
            InitializeComponent();
            
            // 初始化性能计数器
            try
            {
                cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                ramCounter = new PerformanceCounter("Memory", "% Committed Bytes In Use");
                // 首次调用 NextValue() 总是返回 0，需要第二次调用才能获取实际值
                // 这里先预热计数器，在 Page_Loaded 中会再次采样获取有效值
                cpuCounter.NextValue();
                ramCounter.NextValue();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing performance counters: {ex}");
            }
            
            // 初始化硬件监控服务（获取 GPU 占用率、温度等）
            try
            {
                hardwareService = HardwareMonitorService.Instance;
                hardwareService.Initialize();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing hardware monitor service: {ex}");
            }

            // 初始化主计时器（每秒刷新，统管时间和资源更新）
            mainTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            mainTimer.Tick += Timer_Tick;
            
            // 初始化公告计时器
            noticeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            noticeTimer.Tick += NoticeTimer_Tick;
            
            // 监听窗口大小变化，重新布局
            this.SizeChanged += (s, e) =>
            {
                Dispatcher.BeginInvoke(new Action(() => BuildCardsLayout()), 
                    System.Windows.Threading.DispatcherPriority.Background);
            };
            
            // 订阅语言变化事件
            SubscribeLanguageChanged();
        }

        // 订阅语言变化事件（防止重复订阅）
        private void SubscribeLanguageChanged()
        {
            if (!isLanguageEventSubscribed)
            {
                LocalizationManager.Instance.PropertyChanged += OnLanguageChanged;
                isLanguageEventSubscribed = true;
            }
        }
        
        private void OnLanguageChanged(object sender, PropertyChangedEventArgs e)
        {
            // LocalizationManager触发PropertyChanged时传递空字符串
            // 所以我们不检查PropertyName，或者检查空字符串/null
            if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(LocalizationManager.CurrentCulture))
            {
                // 使用BeginInvoke确保在UI线程上执行
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        // 刷新语言缓存
                        RefreshLanguageCache();
                        
                        // 更新欢迎区域的用户名
                        UsernameText.Text = Properties.Lang.ResourceManager.GetString("Hello", 
                            System.Globalization.CultureInfo.CurrentUICulture) + " " + Environment.UserName;
                        
                        // 重新构建卡片布局（会重新创建所有卡片并应用新语言）
                        BuildCardsLayout();
                        
                        // 重新加载一言（切换语言后加载对应语言的一言）
                        _ = LoadHitokotoAsync();
                        
                        // 重新加载公告（切换语言后加载对应语言的公告）
                        _ = LoadNoticesAsync();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Home页面语言更新失败: {ex.Message}");
                    }
                }), System.Windows.Threading.DispatcherPriority.Normal);
            }
        }

        // 安全注册名称的辅助方法
        private void SafeRegisterName(string name, object scopedElement)
        {
            try
            {
                UnregisterName(name);
            }
            catch { }
            
            try
            {
                RegisterName(name, scopedElement);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error registering name {name}: {ex}");
            }
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // 确保语言变化事件已订阅（KeepAlive页面可能被重新加载）
            SubscribeLanguageChanged();
            
            if (!isInitialized)
            {
                // 首次加载：完整初始化
            // 刷新语言缓存
            RefreshLanguageCache();
            
            // 设置用户名
            UsernameText.Text = Properties.Lang.ResourceManager.GetString("Hello", 
                System.Globalization.CultureInfo.CurrentUICulture) + " " + Environment.UserName;
            
            // 构建卡片布局
            BuildCardsLayout();
            
            // 启动主定时器（统管时间、资源和健康状态刷新）
            UpdateTimeDisplay();
            UpdateResourceMonitor();
            UpdateSystemHealth();
            mainTimer.Start();
            
            // 加载一言卡片
            await LoadHitokotoAsync();
            
            // 加载公告
            await LoadNoticesAsync();
            
            // 加载天气
            await LoadWeatherAsync();
            
            // 加载系统健康状态
            UpdateSystemHealth();
                
                // 标记为已初始化
                isInitialized = true;
            }
            else
            {
                // 后续加载：mainTimer 一直运行，无需额外操作
                    
                if (notices.Count > 1 && !noticeTimer.IsEnabled)
                    noticeTimer.Start();
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            // 页面切换时：
            // - mainTimer 继续运行（持续刷新时间和资源）
            // - noticeTimer 暂停（UI 动画在后台无意义）
            noticeTimer?.Stop();
        }

        private void UnregisterCardNames()
        {
            // 取消注册所有卡片相关的名称
            string[] registeredNames = new[] 
            {
                "OpenTimeText", "RunTimeText", "IPv4Text", "IPv6Text",
                "HealthIcon", "HealthStatus",
                "WeatherIcon", "WeatherTemp", "WeatherLocation", "HitokotoText"
            };
            
            foreach (var name in registeredNames)
            {
                try
                {
                    if (FindName(name) != null)
                        UnregisterName(name);
                }
                catch { /* 忽略未注册的名称 */ }
            }
        }

        private void BuildCardsLayout()
        {
            // 先取消注册所有已注册的名称，避免重复调用时冲突
            UnregisterCardNames();

            CardsPanel.Children.Clear();
            CardsPanel.ColumnDefinitions.Clear();
            CardsPanel.RowDefinitions.Clear();
            
            // 计算列数 (每列约300px，默认3列)
            double availableWidth = this.ActualWidth - 50;
            if (availableWidth < 400) availableWidth = 1000;
            int columnCount = Math.Max(2, (int)(availableWidth / 300));
            
            // 创建列定义和列容器
            var columnStacks = new List<StackPanel>();
            for (int i = 0; i < columnCount; i++)
            {
                CardsPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                
                var stackPanel = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Margin = new Thickness(0, 0, i < columnCount - 1 ? 8 : 0, 0) // 最后一列无右边距
                };
                Grid.SetColumn(stackPanel, i);
                CardsPanel.Children.Add(stackPanel);
                columnStacks.Add(stackPanel);
            }
            
            // 每次都重新创建卡片（确保使用最新的语言设置）
            allCards = new List<Border>
            {
                CreateSystemTimeCard(),      // 1. 系统时间卡片
                CreateSystemInfoCard(),       // 2. 系统信息卡片
                CreateResourceMonitorCard(),  // 3. 系统资源卡片（包含温度）
                CreateSystemHealthCard(),     // 4. 系统健康卡片
                CreateQuickActionsCard(),     // 5. 快速操作卡片
                CreateNetworkInfoCard(),      // 6. 网络信息卡片
                CreateWeatherCard(),          // 7. 天气卡片
                CreateHitokotoCard()          // 8. 一言卡片
            };
            
            // 将卡片轮询分配到各列
            for (int i = 0; i < allCards.Count; i++)
            {
                int targetColumn = i % columnCount;
                columnStacks[targetColumn].Children.Add(allCards[i]);
            }
        }

        #region 卡片创建方法

        private Border CreateCard(string icon, string title, UIElement content)
        {
            var card = new Border
            {
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(18, 16, 18, 16),  // 调整内边距，使内容更加均衡和居中
                CornerRadius = new CornerRadius(8),
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch  // 改为 Stretch 实现底部对齐
            };
            
            card.SetResourceReference(Border.BackgroundProperty, "CardBackgroundFillColorDefaultBrush");
            card.SetResourceReference(Border.BorderBrushProperty, "CardStrokeColorDefaultBrush");
            
            card.Effect = new DropShadowEffect
            {
                Color = Colors.Black,
                Direction = 270,
                ShadowDepth = 2,
                BlurRadius = 6,
                Opacity = 0.15
            };
            
            var stack = new StackPanel();
            
            // 标题
            var headerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(2, 0, 2, 12),  // 添加左右边距，使其更居中
                HorizontalAlignment = HorizontalAlignment.Left
            };
            
            var iconText = new TextBlock
            {
                Text = icon,
                FontFamily = (FontFamily)Application.Current.TryFindResource("SegoeIcons") 
                             ?? new FontFamily("Segoe MDL2 Assets"),
                FontSize = 18,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            iconText.SetResourceReference(TextBlock.ForegroundProperty, "AccentTextFillColorPrimaryBrush");
            
            var titleText = new TextBlock
            {
                Text = title,
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                FontFamily = new FontFamily("Microsoft YaHei UI, Segoe UI")
            };
            titleText.SetResourceReference(TextBlock.ForegroundProperty, "AccentTextFillColorPrimaryBrush");
            
            headerPanel.Children.Add(iconText);
            headerPanel.Children.Add(titleText);
            stack.Children.Add(headerPanel);
            
            // 分隔线
            var separator = new Border
            {
                Height = 1,
                Margin = new Thickness(-2, 0, -2, 10)  // 负边距使分割线延伸到卡片边缘，更加整齐
            };
            separator.SetResourceReference(Border.BackgroundProperty, "DividerStrokeColorDefaultBrush");
            stack.Children.Add(separator);
            
            // 内容
            stack.Children.Add(content);
            
            card.Child = stack;
            return card;
        }
        
        private Border CreateCardWithStatus(string icon, string title, UIElement content)
        {
            var card = new Border
            {
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(18, 16, 18, 16),
                CornerRadius = new CornerRadius(8),
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            
            card.SetResourceReference(Border.BackgroundProperty, "CardBackgroundFillColorDefaultBrush");
            card.SetResourceReference(Border.BorderBrushProperty, "CardStrokeColorDefaultBrush");
            
            card.Effect = new DropShadowEffect
            {
                Color = Colors.Black,
                Direction = 270,
                ShadowDepth = 2,
                BlurRadius = 6,
                Opacity = 0.15
            };
            
            var stack = new StackPanel();
            
            // 标题行（使用Grid布局以支持左右两侧内容）
            var headerGrid = new Grid
            {
                Margin = new Thickness(2, 0, 2, 12)
            };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            
            // 左侧：图标和标题
            var leftPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            
            var iconText = new TextBlock
            {
                Text = icon,
                FontFamily = (FontFamily)Application.Current.TryFindResource("SegoeIcons") 
                             ?? new FontFamily("Segoe MDL2 Assets"),
                FontSize = 18,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            iconText.SetResourceReference(TextBlock.ForegroundProperty, "AccentTextFillColorPrimaryBrush");
            
            var titleText = new TextBlock
            {
                Text = title,
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                FontFamily = new FontFamily("Microsoft YaHei UI, Segoe UI")
            };
            titleText.SetResourceReference(TextBlock.ForegroundProperty, "AccentTextFillColorPrimaryBrush");
            
            leftPanel.Children.Add(iconText);
            leftPanel.Children.Add(titleText);
            Grid.SetColumn(leftPanel, 0);
            headerGrid.Children.Add(leftPanel);
            
            // 右侧：状态显示
            var statusPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            
            var statusIcon = new TextBlock
            {
                Name = "HealthIcon",
                Text = "\uE73E",  // StatusCircleCheckmark
                FontFamily = (FontFamily)Application.Current.TryFindResource("SegoeIcons") 
                             ?? new FontFamily("Segoe MDL2 Assets"),
                FontSize = 16,
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            statusIcon.SetResourceReference(TextBlock.ForegroundProperty, "SystemFillColorSuccessBrush");
            SafeRegisterName("HealthIcon", statusIcon);
            
            var statusText = new TextBlock
            {
                Name = "HealthStatus",
                Text = Properties.Lang.ResourceManager.GetString("SystemHealthGood", 
                    System.Globalization.CultureInfo.CurrentUICulture) ?? "良好",
                FontSize = 13,
                FontWeight = FontWeights.Medium,
                VerticalAlignment = VerticalAlignment.Center
            };
            statusText.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");
            SafeRegisterName("HealthStatus", statusText);
            
            statusPanel.Children.Add(statusIcon);
            statusPanel.Children.Add(statusText);
            Grid.SetColumn(statusPanel, 2);
            headerGrid.Children.Add(statusPanel);
            
            stack.Children.Add(headerGrid);
            
            // 分隔线
            var separator = new Border
            {
                Height = 1,
                Margin = new Thickness(-2, 0, -2, 10)
            };
            separator.SetResourceReference(Border.BackgroundProperty, "DividerStrokeColorDefaultBrush");
            stack.Children.Add(separator);
            
            // 内容
            stack.Children.Add(content);
            
            card.Child = stack;
            return card;
        }

        private Border CreateSystemTimeCard()
        {
            var content = new StackPanel();
            
            var startTimeLabel = new TextBlock
            {
                Text = Properties.Lang.ResourceManager.GetString("SystemStartTime", 
                    System.Globalization.CultureInfo.CurrentUICulture),
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 5)
            };
            startTimeLabel.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
            
            var openTime = new TextBlock
            {
                Name = "OpenTime",
                Text = UPTime.ToLongDateString(),
                FontSize = 14,
                FontWeight = FontWeights.Medium,
                Margin = new Thickness(0, 0, 0, 12)
            };
            openTime.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");
            SafeRegisterName("OpenTimeText", openTime);
            
            var runTimeLabel = new TextBlock
            {
                Text = Properties.Lang.ResourceManager.GetString("SystemRunTime", 
                    System.Globalization.CultureInfo.CurrentUICulture),
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 5)
            };
            runTimeLabel.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
            
            var runTime = new TextBlock
            {
                Name = "RunTime",
                Text = "...",
                FontSize = 14,
                FontWeight = FontWeights.Medium,
                FontFamily = new FontFamily("Consolas, Microsoft YaHei UI")
            };
            runTime.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");
            SafeRegisterName("RunTimeText", runTime);
            
            content.Children.Add(startTimeLabel);
            content.Children.Add(openTime);
            content.Children.Add(runTimeLabel);
            content.Children.Add(runTime);
            
            return CreateCard("\uE2AD", Properties.Lang.ResourceManager.GetString("SystemTime", 
                System.Globalization.CultureInfo.CurrentUICulture), content);
        }

        private Border CreateSystemInfoCard()
        {
            var content = new StackPanel();
            
            // 获取系统信息
            string osName = "", osVersion = "";
            try
            {
                using (ManagementObjectSearcher OpSystem = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem"))
                {
                    foreach (ManagementObject OpSys in OpSystem.Get())
                    {
                        osName = OpSys.GetPropertyValue("Caption")?.ToString() ?? "Unknown";
                        osVersion = OpSys.GetPropertyValue("Version")?.ToString() ?? "Unknown";
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting OS info: {ex}");
                osName = "Windows";
                osVersion = "Unknown";
            }
            
            var nameText = new TextBlock
            {
                Text = osName,
                FontSize = 13,
                FontWeight = FontWeights.Medium,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            };
            nameText.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");
            
            var versionText = new TextBlock
            {
                Text = $"{Properties.Lang.ResourceManager.GetString("Version", System.Globalization.CultureInfo.CurrentUICulture)}: {osVersion}",
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap
            };
            versionText.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
            
            content.Children.Add(nameText);
            content.Children.Add(versionText);
            
            return CreateCard("\uE7F8", Properties.Lang.ResourceManager.GetString("CurrentSystem", 
                System.Globalization.CultureInfo.CurrentUICulture), content);
        }

        private Border CreateResourceMonitorCard()
        {
            var content = new StackPanel();
            
            // CPU
            var cpuRow = CreateResourceRow("CPU", out cpuValueText, out cpuProgressBar);
            content.Children.Add(cpuRow);
            
            // 内存
            var memRow = CreateResourceRow(Properties.Lang.ResourceManager.GetString("Memory", 
                System.Globalization.CultureInfo.CurrentUICulture) ?? "内存", out memValueText, out memProgressBar);
            content.Children.Add(memRow);
            
            // GPU（如果可用）
            if (hardwareService?.HasGpu() == true)
            {
                var gpuRow = CreateResourceRow("GPU", out gpuValueText, out gpuProgressBar);
                content.Children.Add(gpuRow);
            }
            
            // 磁盘
            var diskRow = CreateResourceRow(Properties.Lang.ResourceManager.GetString("Disk", 
                System.Globalization.CultureInfo.CurrentUICulture) ?? "磁盘", out diskValueText, out diskProgressBar);
            content.Children.Add(diskRow);
            
            return CreateCardWithStatus("\uE950", Properties.Lang.ResourceManager.GetString("SystemResources", 
                System.Globalization.CultureInfo.CurrentUICulture) ?? "系统资源", content);
        }
        
        private Border CreateTemperatureItem(string icon, string label, out TextBlock valueTextBlock)
        {
            var border = new Border
            {
                Padding = new Thickness(6, 4, 6, 4),
                Margin = new Thickness(2),
                CornerRadius = new CornerRadius(4),
                BorderThickness = new Thickness(1)
            };
            border.SetResourceReference(Border.BorderBrushProperty, "ControlStrokeColorDefaultBrush");
            border.SetResourceReference(Border.BackgroundProperty, "CardBackgroundFillColorSecondaryBrush");
            
            var stack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            
            var iconText = new TextBlock
            {
                Text = icon,
                FontFamily = (FontFamily)Application.Current.TryFindResource("SegoeIcons") 
                             ?? new FontFamily("Segoe MDL2 Assets"),
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 4, 0)
            };
            iconText.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
            
            var labelText = new TextBlock
            {
                Text = label + ":",
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 4, 0)
            };
            labelText.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
            
            valueTextBlock = new TextBlock
            {
                Text = "--°C",
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                FontFamily = new FontFamily("Consolas"),
                VerticalAlignment = VerticalAlignment.Center
            };
            valueTextBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");
            
            stack.Children.Add(iconText);
            stack.Children.Add(labelText);
            stack.Children.Add(valueTextBlock);
            
            border.Child = stack;
            
            // 返回 border 用于添加到UI
            return border;
        }

        private StackPanel CreateResourceRow(string label, out TextBlock valueText, out Border progressBar)
        {
            var row = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
            
            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            
            var labelText = new TextBlock
            {
                Text = label,
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 4)
            };
            labelText.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
            Grid.SetColumn(labelText, 0);
            
            valueText = new TextBlock
            {
                Text = "0%",
                FontSize = 12,
                FontWeight = FontWeights.Medium,
                FontFamily = new FontFamily("Consolas"),
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 0, 4)
            };
            valueText.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");
            Grid.SetColumn(valueText, 1);
            
            headerGrid.Children.Add(labelText);
            headerGrid.Children.Add(valueText);
            row.Children.Add(headerGrid);
            
            // 进度条背景
            var progressBackground = new Border
            {
                Height = 6,
                CornerRadius = new CornerRadius(3)
            };
            progressBackground.SetResourceReference(Border.BackgroundProperty, "ControlStrokeColorDefaultBrush");
            
            // 进度条前景
            progressBar = new Border
            {
                Height = 6,
                CornerRadius = new CornerRadius(3),
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = 0
            };
            progressBar.Background = new SolidColorBrush(Color.FromRgb(0, 120, 212));
            
            var progressGrid = new Grid();
            progressGrid.Children.Add(progressBackground);
            progressGrid.Children.Add(progressBar);
            row.Children.Add(progressGrid);
            
            return row;
        }

        private Border CreateSystemHealthCard()
        {
            var content = new StackPanel();
            
            var tempGrid = new UniformGrid
            {
                Columns = hardwareService?.HasGpu() == true ? 2 : 1,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            
            // CPU 温度（使用 WMI 回退时标明是系统环境温度）
            string cpuLabel = hardwareService?.IsWmiFallbackActive == true ? 
                Properties.Lang.ResourceManager.GetString("SystemAmbientTemp", 
                    System.Globalization.CultureInfo.CurrentUICulture) ?? "系统" : "CPU";
            var cpuTempBorder = CreateTemperatureItem("\uE950", cpuLabel, out cpuTempText);
            if (hardwareService?.IsWmiFallbackActive == true)
            {
                cpuTempBorder.ToolTip = Properties.Lang.ResourceManager.GetString("AmbientTempTooltip",
                    System.Globalization.CultureInfo.CurrentUICulture) ?? "当前为环境温度，以管理员身份运行可获取CPU核心温度";
            }
            tempGrid.Children.Add(cpuTempBorder);
            
            // GPU 温度（如果可用）
            if (hardwareService?.HasGpu() == true)
            {
                var gpuTempBorder = CreateTemperatureItem("\uE7FC", "GPU", out gpuTempText);
                tempGrid.Children.Add(gpuTempBorder);
            }
            
            content.Children.Add(tempGrid);
            
            return CreateCard("\uE7F8", Properties.Lang.ResourceManager.GetString("SystemTemperature", 
                System.Globalization.CultureInfo.CurrentUICulture) ?? "系统温度", content);
        }

        private Border CreateQuickActionsCard()
        {
            var content = new UniformGrid
            {
                Columns = 3,
                Rows = 2,
                HorizontalAlignment = HorizontalAlignment.Center,  // 水平居中
                VerticalAlignment = VerticalAlignment.Center  // 垂直居中
            };
            
            // 创建快速操作按钮
            content.Children.Add(CreateActionButton("\uE7E8", Properties.Lang.ResourceManager.GetString("Restart",
                System.Globalization.CultureInfo.CurrentUICulture) ?? "重启", () => ExecuteSystemCommand("shutdown /r /t 0")));
            content.Children.Add(CreateActionButton("\uE7E8", Properties.Lang.ResourceManager.GetString("Shutdown",
                System.Globalization.CultureInfo.CurrentUICulture) ?? "关机", () => ExecuteSystemCommand("shutdown /s /t 0")));
            content.Children.Add(CreateActionButton("\uE708", Properties.Lang.ResourceManager.GetString("Sleep",
                System.Globalization.CultureInfo.CurrentUICulture) ?? "睡眠", () => ExecuteSystemCommand("rundll32.exe powrprof.dll,SetSuspendState 0,1,0")));
            content.Children.Add(CreateActionButton("\uE74D", Properties.Lang.ResourceManager.GetString("Cleanup",
                System.Globalization.CultureInfo.CurrentUICulture) ?? "清理", () => ExecuteSystemCommand("cleanmgr")));
            content.Children.Add(CreateActionButton("\uE8B8", Properties.Lang.ResourceManager.GetString("TaskManager",
                System.Globalization.CultureInfo.CurrentUICulture) ?? "任务管理器", () => Process.Start("taskmgr")));
            content.Children.Add(CreateActionButton("\uE713", Properties.Lang.ResourceManager.GetString("Settings",
                System.Globalization.CultureInfo.CurrentUICulture) ?? "设置", () => ExecuteSystemCommand("ms-settings:")));
            
            return CreateCard("\uE90F", Properties.Lang.ResourceManager.GetString("QuickActions", 
                System.Globalization.CultureInfo.CurrentUICulture) ?? "快速操作", content);
        }

        private Button CreateActionButton(string icon, string label, Action action)
        {
            var button = new Button
            {
                Margin = new Thickness(2),
                Padding = new Thickness(5),
                Height = 50,
                Width = 80,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(1)
            };
            button.SetResourceReference(Button.BorderBrushProperty, "ControlStrokeColorDefaultBrush");
            
            var stack = new StackPanel 
            { 
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            
            var iconText = new TextBlock
            {
                Text = icon,
                FontFamily = (FontFamily)Application.Current.TryFindResource("SegoeIcons") 
                             ?? new FontFamily("Segoe MDL2 Assets"),
                FontSize = 18,  // 稍微增大图标
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 6)  // 增加图标和文字的间距
            };
            iconText.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");
            
            var labelText = new TextBlock
            {
                Text = label,
                FontSize = 12,  // 增大文字尺寸
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center
            };
            labelText.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
            
            stack.Children.Add(iconText);
            stack.Children.Add(labelText);
            button.Content = stack;
            
            button.Click += (s, e) =>
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error executing action: {ex}");
                    MessageBox.Show($"执行操作失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            
            return button;
        }

        private Border CreateNetworkInfoCard()
        {
            var content = new StackPanel();
            
            var ipv4Panel = CreateIPPanel("IPv4", out TextBlock ipv4Text, IPv4_MouseLeftButtonDown, IPv4_MouseRightButtonDown);
            SafeRegisterName("IPv4Text", ipv4Text);
            content.Children.Add(ipv4Panel);
            
            var ipv6Panel = CreateIPPanel("IPv6", out TextBlock ipv6Text, IPv6_MouseLeftButtonDown, IPv6_MouseRightButtonDown);
            SafeRegisterName("IPv6Text", ipv6Text);
            content.Children.Add(ipv6Panel);
            
            var providerText = new TextBlock
            {
                Text = Properties.Lang.ResourceManager.GetString("ServiceProvider", 
                    System.Globalization.CultureInfo.CurrentUICulture),
                FontSize = 10,
                Margin = new Thickness(0, 8, 0, 0)
            };
            providerText.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorTertiaryBrush");
            content.Children.Add(providerText);
            
            return CreateCard("\uE968", Properties.Lang.ResourceManager.GetString("IP", 
                System.Globalization.CultureInfo.CurrentUICulture), content);
        }

        private StackPanel CreateIPPanel(string label, out TextBlock valueText, 
            MouseButtonEventHandler leftClick, MouseButtonEventHandler rightClick)
        {
            var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
            
            var labelText = new TextBlock
            {
                Text = label + ":",
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 4)
            };
            labelText.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
            
            valueText = new TextBlock
            {
                Text = "***.***.***.**" + (label == "IPv6" ? "*" : ""),
                FontSize = 12,
                FontFamily = new FontFamily("Consolas, Microsoft YaHei UI"),
                Cursor = Cursors.Hand,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 280,
                LineHeight = 18,
                ToolTip = Properties.Lang.ResourceManager.GetString("LR_ToolTip", 
                    System.Globalization.CultureInfo.CurrentUICulture)
            };
            valueText.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");
            valueText.MouseLeftButtonDown += leftClick;
            valueText.MouseRightButtonDown += rightClick;
            
            panel.Children.Add(labelText);
            panel.Children.Add(valueText);
            
            return panel;
        }

        private Border CreateWeatherCard()
        {
            var content = new StackPanel();
            
            var weatherPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 8)
            };
            
            var weatherIcon = new TextBlock
            {
                Name = "WeatherIcon",
                Text = "☀️",
                FontSize = 32,
                Margin = new Thickness(0, 0, 10, 0)
            };
            SafeRegisterName("WeatherIcon", weatherIcon);
            
            var weatherInfo = new StackPanel();
            
            var tempText = new TextBlock
            {
                Name = "WeatherTemp",
                Text = "--°C",
                FontSize = 20,
                FontWeight = FontWeights.SemiBold
            };
            tempText.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");
            SafeRegisterName("WeatherTemp", tempText);
            
            var locationText = new TextBlock
            {
                Name = "WeatherLocation",
                Text = Properties.Lang.ResourceManager.GetString("LoadingWeather", 
                    System.Globalization.CultureInfo.CurrentUICulture) ?? "加载中...",
                FontSize = 11
            };
            locationText.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
            SafeRegisterName("WeatherLocation", locationText);
            
            weatherInfo.Children.Add(tempText);
            weatherInfo.Children.Add(locationText);
            
            weatherPanel.Children.Add(weatherIcon);
            weatherPanel.Children.Add(weatherInfo);
            content.Children.Add(weatherPanel);
            
            return CreateCard("\uE753", Properties.Lang.ResourceManager.GetString("Weather", 
                System.Globalization.CultureInfo.CurrentUICulture) ?? "天气", content);
        }

        private Border CreateHitokotoCard()
        {
            var content = new TextBlock
            {
                Name = "HitokotoText",
                Text = Properties.Lang.ResourceManager.GetString("LoadingHitokoto", 
                    System.Globalization.CultureInfo.CurrentUICulture) ?? "Now loading",
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
                FontStyle = FontStyles.Italic,
                LineHeight = 20
            };
            content.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
            SafeRegisterName("HitokotoText", content);
            
            return CreateCard("\uE8F2", Properties.Lang.ResourceManager.GetString("Hitokoto", 
                System.Globalization.CultureInfo.CurrentUICulture) ?? "一言", content);
        }

        #endregion

        #region 更新方法

        private void RefreshLanguageCache()
        {
            isChineseLanguage = System.Globalization.CultureInfo.CurrentUICulture.Name.StartsWith("zh");
            dayUnit = Properties.Lang.ResourceManager.GetString("TimeUnitDay", System.Globalization.CultureInfo.CurrentUICulture) ?? "天";
            hourUnit = Properties.Lang.ResourceManager.GetString("TimeUnitHour", System.Globalization.CultureInfo.CurrentUICulture) ?? "时";
            minuteUnit = Properties.Lang.ResourceManager.GetString("TimeUnitMinute", System.Globalization.CultureInfo.CurrentUICulture) ?? "分";
            secondUnit = Properties.Lang.ResourceManager.GetString("TimeUnitSecond", System.Globalization.CultureInfo.CurrentUICulture) ?? "秒";
        }

        private void UpdateTimeDisplay()
        {
            try
            {
                TimeSpan Nows = DateTime.Now - UPTime;
                string RunTime_ = $"{Nows.Days} {dayUnit} {Nows.Hours} {hourUnit} {Nows.Minutes} {minuteUnit} {Nows.Seconds} {secondUnit}";
                
                if (FindName("RunTimeText") is TextBlock runTimeText)
                {
                    runTimeText.Text = RunTime_;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating time: {ex}");
            }
        }

        private void UpdateResourceMonitor()
        {
            try
            {
                // 更新所有硬件数据
                hardwareService?.Update();
                
                // CPU使用率
                float cpuUsage = hardwareService?.GetCpuUsage() ?? 0;
                UpdateResourceDisplay(cpuValueText, cpuProgressBar, cpuUsage);
                
                // 内存使用率
                float memUsage = hardwareService?.GetMemoryUsage() ?? 0;
                UpdateResourceDisplay(memValueText, memProgressBar, memUsage);
                
                // GPU使用率（如果可用）
                if (hardwareService?.HasGpu() == true)
                {
                    float gpuUsage = hardwareService.GetGpuUsage();
                    
                    // 调试输出：每10秒打印一次 GPU 信息
                    //if (DateTime.Now.Second % 10 == 0)
                    //{
                    //    Debug.WriteLine($"GPU Usage: {gpuUsage}%");
                    //    // 取消下面这行注释可以查看详细的 GPU 传感器信息
                    //    Debug.WriteLine(hardwareService.GetGpuInfo());
                    //}
                    
                    UpdateResourceDisplay(gpuValueText, gpuProgressBar, gpuUsage);
                }
                
                // 磁盘（获取C盘使用率）
                float diskUsage = GetDiskUsage();
                UpdateResourceDisplay(diskValueText, diskProgressBar, diskUsage);
                
                // 更新温度
                UpdateTemperatureDisplay();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating resources: {ex}");
            }
        }
        
        private void UpdateTemperatureDisplay()
        {
            try
            {
                // CPU 温度
                if (cpuTempText != null)
                {
                    float cpuTemp = hardwareService?.GetCpuTemperature() ?? 0;
                    cpuTempText.Text = cpuTemp > 0 ? $"{cpuTemp:F0}°C" : "--°C";
                    ApplyTemperatureColor(cpuTempText, cpuTemp);
                }
                
                // GPU 温度
                if (gpuTempText != null && hardwareService?.HasGpu() == true)
                {
                    float gpuTemp = hardwareService.GetGpuTemperature();
                    gpuTempText.Text = gpuTemp > 0 ? $"{gpuTemp:F0}°C" : "--°C";
                    ApplyTemperatureColor(gpuTempText, gpuTemp);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating temperatures: {ex}");
            }
        }
        
        private void ApplyTemperatureColor(TextBlock textBlock, float temperature)
        {
            if (temperature <= 0)
            {
                textBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
                return;
            }
            
            Color color;
            if (temperature < 60)
            {
                // 绿色 - 安全温度
                color = Color.FromRgb(16, 185, 129);
            }
            else if (temperature < 80)
            {
                // 黄色 - 警告温度
                color = Color.FromRgb(245, 158, 11);
            }
            else
            {
                // 红色 - 危险温度
                color = Color.FromRgb(239, 68, 68);
            }
            
            textBlock.Foreground = new SolidColorBrush(color);
        }

        private void UpdateResourceDisplay(TextBlock valueText, Border progressBar, float percentage)
        {
            if (valueText == null || progressBar == null) return;
            
            valueText.Text = $"{percentage:F0}%";
            
            // 动画更新进度条
            double targetWidth = (progressBar.Parent as Grid)?.ActualWidth * (percentage / 100.0) ?? 0;
            var animation = new DoubleAnimation
            {
                To = Math.Max(0, targetWidth),
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase()
            };
            progressBar.BeginAnimation(FrameworkElement.WidthProperty, animation);
            
            // 根据使用率设置颜色
            Color color;
            if (percentage < 60)
                color = Color.FromRgb(16, 185, 129); // 绿色
            else if (percentage < 85)
                color = Color.FromRgb(245, 158, 11); // 黄色
            else
                color = Color.FromRgb(239, 68, 68); // 红色
                
            var colorAnimation = new ColorAnimation
            {
                To = color,
                Duration = TimeSpan.FromMilliseconds(300)
            };
            progressBar.Background.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
        }

        private float GetDiskUsage()
        {
            try
            {
                var drive = new System.IO.DriveInfo("C");
                if (drive.IsReady)
                {
                    double usedSpace = drive.TotalSize - drive.AvailableFreeSpace;
                    return (float)(usedSpace / drive.TotalSize * 100.0);
                }
            }
            catch { }
            return 0;
        }

        private void UpdateSystemHealth()
        {
            try
            {
                float cpuUsage = hardwareService?.GetCpuUsage() ?? 0;
                float memUsage = hardwareService?.GetMemoryUsage() ?? 0;
                
                string status, icon;
                SolidColorBrush iconColor;
                if (cpuUsage < 70 && memUsage < 80)
                {
                    status = Properties.Lang.ResourceManager.GetString("SystemHealthGood", 
                        System.Globalization.CultureInfo.CurrentUICulture) ?? "良好";
                    icon = "\uE73E";  // StatusCircleCheckmark - 绿色勾
                    iconColor = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                }
                else if (cpuUsage < 85 && memUsage < 90)
                {
                    status = Properties.Lang.ResourceManager.GetString("SystemHealthWarning", 
                        System.Globalization.CultureInfo.CurrentUICulture) ?? "警告";
                    icon = "\uE7BA";  // StatusCircleErrorX - 黄色警告
                    iconColor = new SolidColorBrush(Color.FromRgb(245, 158, 11));
                }
                else
                {
                    status = Properties.Lang.ResourceManager.GetString("SystemHealthCritical", 
                        System.Globalization.CultureInfo.CurrentUICulture) ?? "严重";
                    icon = "\uEA39";  // StatusCircleBlock - 红色禁止
                    iconColor = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                }
                
                if (FindName("HealthIcon") is TextBlock healthIcon)
                {
                    healthIcon.Text = icon;
                    healthIcon.Foreground = iconColor;
                }
                    
                if (FindName("HealthStatus") is TextBlock healthStatus)
                    healthStatus.Text = status;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating system health: {ex}");
            }
        }

        private async Task LoadHitokotoAsync()
        {
            try
            {
                var response = await client.GetAsync("https://v1.hitokoto.cn/?c=b&c=a&encode=text");
                response.EnsureSuccessStatusCode();
                string webCode = await response.Content.ReadAsStringAsync();
                
                if (FindName("HitokotoText") is TextBlock hitokotoText)
                {
                    hitokotoText.Text = webCode;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading hitokoto: {ex}");
                if (FindName("HitokotoText") is TextBlock hitokotoText)
                {
                    hitokotoText.Text = Properties.Lang.ResourceManager.GetString("NetError", 
                        System.Globalization.CultureInfo.CurrentUICulture) ?? "网络错误";
                }
            }
        }

        private async Task LoadWeatherAsync()
        {
            try
            {
                // 第一步：通过ip.sb获取地理位置
                var ipResponse = await client.GetAsync("https://api.ip.sb/geoip");
                ipResponse.EnsureSuccessStatusCode();
                string ipData = await ipResponse.Content.ReadAsStringAsync();
                
                var city = ExtractJsonValue(ipData, "city");
                var country = ExtractJsonValue(ipData, "country");
                
                if (string.IsNullOrEmpty(city))
                {
                    throw new Exception("无法获取城市信息");
                }
                
                // 第二步：使用wttr.in API根据城市名获取天气
                // 使用城市名查询，wttr.in会自动识别
                var weatherUrl = $"https://wttr.in/{Uri.EscapeDataString(city)}?format=j1";
                var weatherResponse = await client.GetAsync(weatherUrl);
                weatherResponse.EnsureSuccessStatusCode();
                string weatherData = await weatherResponse.Content.ReadAsStringAsync();
                
                // 解析wttr.in的JSON数据
                var temp = ExtractJsonValue(weatherData, "temp_C");
                var weatherDesc = ExtractJsonValue(weatherData, "weatherDesc");
                
                if (FindName("WeatherTemp") is TextBlock tempText)
                {
                    if (!string.IsNullOrEmpty(temp))
                    {
                        // 温度可能包含小数点，格式化为整数
                        if (double.TryParse(temp, System.Globalization.NumberStyles.Any, 
                            System.Globalization.CultureInfo.InvariantCulture, out double tempValue))
                        {
                            tempText.Text = $"{Math.Round(tempValue)}°C";
                        }
                        else
                        {
                            tempText.Text = $"{temp}°C";
                        }
                    }
                    else
                    {
                    tempText.Text = "--°C";
                    }
                }
                    
                if (FindName("WeatherLocation") is TextBlock locationText)
                {
                    string location = "";
                    if (!string.IsNullOrEmpty(city))
                    {
                        location = city;
                        if (!string.IsNullOrEmpty(country) && country != city)
                        {
                            location += $", {country}";
                        }
                    }
                    else if (!string.IsNullOrEmpty(country))
                    {
                        location = country;
                    }
                    else
                    {
                        location = Properties.Lang.ResourceManager.GetString("WeatherNotConfigured", 
                        System.Globalization.CultureInfo.CurrentUICulture) ?? "未配置天气服务";
                    }
                    locationText.Text = location;
                }
                
                if (FindName("WeatherIcon") is TextBlock iconText && !string.IsNullOrEmpty(weatherDesc))
                {
                    iconText.Text = GetWeatherEmojiFromDesc(weatherDesc);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading weather: {ex}");
                
                if (FindName("WeatherTemp") is TextBlock tempText)
                    tempText.Text = "--°C";
                    
                if (FindName("WeatherLocation") is TextBlock locationText)
                {
                    locationText.Text = Properties.Lang.ResourceManager.GetString("NetError", 
                        System.Globalization.CultureInfo.CurrentUICulture) ?? "网络错误";
                }
            }
        }
        
        private string ExtractJsonValue(string json, string key)
        {
            try
            {
                // 尝试匹配字符串值："key":"value"
                string pattern = $"\"{key}\"\\s*:\\s*\"([^\"]+)\"";
                var match = Regex.Match(json, pattern);
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
                
                // 尝试匹配数字值："key":123 或 "key":123.45
                pattern = $"\"{key}\"\\s*:\\s*([\\d\\.\\-]+)";
                match = Regex.Match(json, pattern);
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
                
                // 尝试数组中的第一个元素
                pattern = $"\"{key}\"\\s*:\\s*\\[\\s*{{[^}}]*\"value\"\\s*:\\s*\"([^\"]+)\"";
                match = Regex.Match(json, pattern);
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }
            catch { }
            return string.Empty;
        }
        
        // 根据wttr.in天气描述转换为emoji图标
        private string GetWeatherEmojiFromDesc(string weatherDesc)
        {
            if (string.IsNullOrEmpty(weatherDesc))
                return "🌤️";
            
            weatherDesc = weatherDesc.ToLower();
            
            if (weatherDesc.Contains("sunny") || weatherDesc.Contains("clear"))
                return "☀️";
            else if (weatherDesc.Contains("partly cloudy") || weatherDesc.Contains("partly cloud"))
                return "⛅";
            else if (weatherDesc.Contains("cloudy") || weatherDesc.Contains("overcast"))
                return "☁️";
            else if (weatherDesc.Contains("mist") || weatherDesc.Contains("fog"))
                return "🌫️";
            else if (weatherDesc.Contains("thunder") || weatherDesc.Contains("storm"))
                return "⛈️";
            else if (weatherDesc.Contains("snow") || weatherDesc.Contains("blizzard"))
                return "❄️";
            else if (weatherDesc.Contains("sleet") || weatherDesc.Contains("ice"))
                return "🌨️";
            else if (weatherDesc.Contains("rain") || weatherDesc.Contains("drizzle") || weatherDesc.Contains("shower"))
                return "🌧️";
            else if (weatherDesc.Contains("wind"))
                return "💨";
            else
                return "🌤️";
        }

        private async Task LoadNoticesAsync()
        {
            try
            {
                string noticeUrl = isChineseLanguage 
                    ? "https://systools.hksstudio.work/PublicNotice"
                    : "https://systools.hksstudio.work/PublicNotice_EN";

                var response = await client.GetAsync(noticeUrl);
                response.EnsureSuccessStatusCode();
                string noticeContent = await response.Content.ReadAsStringAsync();
                
                notices = noticeContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                                     .Select(n => n.Trim())
                                     .Where(n => !string.IsNullOrEmpty(n))
                                     .ToList();

                if (notices.Count > 0)
                {
                    PublicNews.Text = notices[0];
                    
                    if (notices.Count > 1)
                    {
                        noticeTimer.Interval = notices[0].Length > 15 ? 
                            TimeSpan.FromSeconds(5) : TimeSpan.FromSeconds(3);
                        noticeTimer.Start();
                    }
                }
                else
                {
                    PublicNews.Text = Properties.Lang.ResourceManager.GetString("NoticeInfo", 
                        System.Globalization.CultureInfo.CurrentUICulture);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading notices: {ex}");
                PublicNews.Text = Properties.Lang.ResourceManager.GetString("NoticeError", 
                    System.Globalization.CultureInfo.CurrentUICulture);
            }
        }

        #endregion

        #region 定时器事件

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateTimeDisplay();
            UpdateResourceMonitor();
            UpdateSystemHealth();
        }

        private void NoticeTimer_Tick(object sender, EventArgs e)
        {
            if (notices.Count > 1)
            {
                currentNoticeIndex = (currentNoticeIndex + 1) % notices.Count;
                PublicNews.Opacity = 0;
                string nextNotice = notices[currentNoticeIndex];
                PublicNews.Text = nextNotice;
                
                int textLength = nextNotice.Length;
                TimeSpan interval = textLength > 15 ? TimeSpan.FromSeconds(5) : TimeSpan.FromSeconds(3);
                noticeTimer.Interval = interval;

                PublicNews.BeginAnimation(UIElement.OpacityProperty, 
                    new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(500)));
            }
        }

        #endregion

        #region IP 获取方法

        private void IPv4_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            IPv4_Info();
        }

        private void IPv4_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (FindName("IPv4Text") is TextBlock ipv4Text)
                ipv4Text.Text = "***.***.***.***";
        }

        private void IPv6_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            IPv6_Info();
        }

        private void IPv6_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (FindName("IPv6Text") is TextBlock ipv6Text)
                ipv6Text.Text = "****:****:****:****:****:****:****:****";
        }

        private async void IPv4_Info()
        {
            if (!(FindName("IPv4Text") is TextBlock ipv4Text)) return;
            
            try
            {
                ipv4Text.Text = Properties.Lang.ResourceManager.GetString("Loading", 
                    System.Globalization.CultureInfo.CurrentUICulture) ?? "Loading...";
                    
                var response = await client.GetAsync("https://myip.ipip.net/");
                response.EnsureSuccessStatusCode();
                string IPv4 = await response.Content.ReadAsStringAsync();
                
                // 移除首尾空白字符
                IPv4 = IPv4.Trim();
                
                // 将"来自于"替换为换行符，使信息更易读
                IPv4 = IPv4.Replace("来自于：", "\n来自：");
                IPv4 = IPv4.Replace("来自于", "\n来自：");
                
                ipv4Text.Text = IPv4;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting IPv4: {ex}");
                ipv4Text.Text = Properties.Lang.ResourceManager.GetString("IPv4Error", 
                    System.Globalization.CultureInfo.CurrentUICulture) ?? "获取失败";
            }
        }

        private async void IPv6_Info()
        {
            if (!(FindName("IPv6Text") is TextBlock ipv6Text)) return;
            
            try
            {
                ipv6Text.Text = Properties.Lang.ResourceManager.GetString("Loading", 
                    System.Globalization.CultureInfo.CurrentUICulture) ?? "Loading...";
                    
                var response = await client.GetAsync("https://speed.neu6.edu.cn/getIP.php");
                response.EnsureSuccessStatusCode();
                string IPv6 = await response.Content.ReadAsStringAsync();
                ipv6Text.Text = IPv6;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting IPv6: {ex}");
                ipv6Text.Text = Properties.Lang.ResourceManager.GetString("IPv6Error", 
                    System.Globalization.CultureInfo.CurrentUICulture) ?? "获取失败";
            }
        }

        #endregion

        #region 快速操作方法

        private void ExecuteSystemCommand(string command)
        {
            try
            {
                if (command.StartsWith("ms-settings:"))
                {
                    Process.Start(new ProcessStartInfo(command) { UseShellExecute = true });
                }
                else
                {
                    var result = MessageBox.Show(
                        Properties.Lang.ResourceManager.GetString("ConfirmAction", 
                            System.Globalization.CultureInfo.CurrentUICulture) ?? "确认执行此操作？",
                        Properties.Lang.ResourceManager.GetString("Confirmation", 
                            System.Globalization.CultureInfo.CurrentUICulture) ?? "确认",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                        
                    if (result == MessageBoxResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo("cmd.exe", $"/c {command}") 
                        { 
                            CreateNoWindow = true,
                            UseShellExecute = false 
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error executing command: {ex}");
                MessageBox.Show(
                    Properties.Lang.ResourceManager.GetString("ExecutionError", 
                        System.Globalization.CultureInfo.CurrentUICulture) ?? "执行失败",
                    Properties.Lang.ResourceManager.GetString("Error", 
                        System.Globalization.CultureInfo.CurrentUICulture) ?? "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
