using iNKORE.UI.WPF.Modern.Common.IconKeys;
using iNKORE.UI.WPF.Modern.Helpers.Styles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows.Input;
using System.ComponentModel;
using SYSTools.Helpers;

namespace SYSTools.Pages
{
    /// <summary>
    /// Test.xaml 的交互逻辑
    /// </summary>
    public partial class Test : Page
    {
        /// <summary>
        /// 信息条目 - 存储原始数据，显示时实时本地化
        /// </summary>
        private class TestInfoItem
        {
            /// <summary>资源键（为空则仅显示 RawValue）</summary>
            public string ResKey;
            /// <summary>格式化参数</summary>
            public string[] Args;
            /// <summary>是否为缩进子项</summary>
            public bool IsIndented;
            /// <summary>纯文本（ResKey 为空时使用，如 GPU/声卡名称）</summary>
            public string RawText;

            public TestInfoItem(string resKey, string[] args, bool isIndented = false)
            {
                ResKey = resKey;
                Args = args;
                IsIndented = isIndented;
                RawText = null;
            }

            public TestInfoItem(string rawText, bool isIndented = false)
            {
                RawText = rawText;
                IsIndented = isIndented;
                ResKey = null;
                Args = null;
            }
        }

        private Dictionary<string, List<TestInfoItem>> hardwareInfo;
        private bool _isLanguageSubscribed = false;

        // 简化的本地化辅助方法
        private static string T(string key, string fallback = "")
        {
            return Properties.Lang.ResourceManager.GetString(key,
                System.Globalization.CultureInfo.CurrentUICulture) ?? fallback;
        }

        public Test()
        {
            InitializeComponent();
            hardwareInfo = new Dictionary<string, List<TestInfoItem>>();
            iNKORE.UI.WPF.Modern.Controls.MessageBox.DefaultBackdropType = BackdropType.Acrylic11;

            // 从资源文件加载按钮文本
            TestBotton.Content = T("Test_DetectButton", "检测配置");
            TestBotton.ToolTip = T("Test_InfoTitle", "系统配置检测说明");

            this.SizeChanged += (s, e) =>
            {
                if (hardwareInfo.Count > 0)
                {
                    Dispatcher.BeginInvoke(new Action(() => BuildUI()),
                        System.Windows.Threading.DispatcherPriority.Background);
                }
            };

            // 订阅语言切换事件
            SubscribeLanguageChanged();
        }

        private void SubscribeLanguageChanged()
        {
            if (!_isLanguageSubscribed)
            {
                LocalizationManager.Instance.PropertyChanged += OnLanguageChanged;
                _isLanguageSubscribed = true;
            }
        }

        private void OnLanguageChanged(object sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(LocalizationManager.CurrentCulture))
            {
                if (hardwareInfo.Count > 0)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        // 更新检测按钮文字和提示
                        if (TestBotton.IsEnabled)
                            TestBotton.Content = T("Test_DetectButton", "检测配置");
                        TestBotton.ToolTip = T("Test_InfoTitle", "系统配置检测说明");
                        BuildUI();
                    }), System.Windows.Threading.DispatcherPriority.Normal);
                }
            }
        }

        private async void TestBotton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TestBotton.IsEnabled = false;
                TestBotton.Content = T("Test_Detecting", "检测中...");

                await Task.Run(() => CollectHardwareInfo());

                Dispatcher.Invoke(() =>
                {
                    BuildUI();
                    TestBotton.Content = T("Test_DetectButton", "检测配置");
                    TestBotton.IsEnabled = true;
                });

                iNKORE.UI.WPF.Modern.Controls.MessageBox.Show(
                    T("Test_DetectSuccess", "配置获取成功"),
                    T("Test_DialogTitle", "系统配置测试"),
                    MessageBoxButton.OK,
                    SegoeFluentIcons.SpecialEffectSize);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting hardware info: {ex}");
                TestBotton.Content = T("Test_DetectButton", "检测配置");
                TestBotton.IsEnabled = true;

                iNKORE.UI.WPF.Modern.Controls.MessageBox.Show(
                    T("Test_DetectFail", "系统配置获取失败 请联系开发者"),
                    T("Test_DialogTitle", "系统配置测试"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void CollectHardwareInfo()
        {
            hardwareInfo.Clear();

            try
            {
                // 计算机信息 - 存储原始数据
                var computerInfo = new List<TestInfoItem>();
                using (ManagementObjectSearcher cmsystem = new ManagementObjectSearcher("SELECT * FROM win32_computersystem"))
                {
                    foreach (ManagementObject cmsys in cmsystem.Get())
                    {
                        double memory = Convert.ToDouble(cmsys.GetPropertyValue("totalphysicalmemory")) / 1024 / 1024 / 1024;
                        memory = (int)memory;

                        computerInfo.Add(new TestInfoItem("Test_Workgroup", new[] { cmsys.GetPropertyValue("domain")?.ToString() }));
                        computerInfo.Add(new TestInfoItem("Test_ComputerName", new[] { cmsys.GetPropertyValue("__server")?.ToString() }));
                        computerInfo.Add(new TestInfoItem("Test_Manufacturer", new[] { cmsys.GetPropertyValue("manufacturer")?.ToString() }));
                        computerInfo.Add(new TestInfoItem("Test_TotalMemory_Format", new[] { (memory + 1).ToString() }));
                    }
                }
                hardwareInfo["Computer"] = computerInfo;

                // 操作系统信息
                var osInfo = new List<TestInfoItem>();
                using (ManagementObjectSearcher OpSystem = new ManagementObjectSearcher("SELECT * FROM win32_OperatingSystem"))
                {
                    foreach (ManagementObject OpSys in OpSystem.Get())
                    {
                        osInfo.Add(new TestInfoItem("Test_WindowsVersion", new[] { OpSys.GetPropertyValue("Caption")?.ToString() }));
                        osInfo.Add(new TestInfoItem("Test_OSArchitecture", new[] { OpSys.GetPropertyValue("OSArchitecture")?.ToString() }));
                        osInfo.Add(new TestInfoItem("Test_KernelVersion", new[] { OpSys.GetPropertyValue("Version")?.ToString() }));
                    }
                }
                hardwareInfo["OS"] = osInfo;

                // CPU信息
                var cpuInfo = new List<TestInfoItem>();
                using (ManagementObjectSearcher Process = new ManagementObjectSearcher("SELECT * FROM win32_Processor"))
                {
                    foreach (ManagementObject CPU in Process.Get())
                    {
                        cpuInfo.Add(new TestInfoItem("Test_CPUModel", new[] { CPU.GetPropertyValue("Name")?.ToString() }));
                        cpuInfo.Add(new TestInfoItem("Test_CPUCores_Format", new[] { CPU.GetPropertyValue("NumberOfCores")?.ToString() }));
                        cpuInfo.Add(new TestInfoItem("Test_CPUThreads_Format", new[] { CPU.GetPropertyValue("NumberOfLogicalProcessors")?.ToString() }));
                    }
                }
                hardwareInfo["CPU"] = cpuInfo;

                // 硬盘信息
                var diskInfo = new List<TestInfoItem>();
                using (ManagementObjectSearcher DiskDrive = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive"))
                {
                    int diskIndex = 1;
                    foreach (ManagementObject Disk in DiskDrive.Get())
                    {
                        ulong size = Convert.ToUInt64(Disk.GetPropertyValue("Size"));
                        double sizeInGB = size / (1024.0 * 1024.0 * 1024.0);
                        double sizeInTB = sizeInGB / 1024.0;
                        string model = Disk.GetPropertyValue("Model")?.ToString();

                        diskInfo.Add(new TestInfoItem("Test_Disk_Format", new[] { diskIndex.ToString(), model }));
                        diskInfo.Add(new TestInfoItem("Test_DiskCapacity_Format",
                            new[] { sizeInGB.ToString("F2"), sizeInTB.ToString("F2") }, isIndented: true));
                        diskIndex++;
                    }
                }
                hardwareInfo["Disk"] = diskInfo;

                // 显卡信息 - 无标签，纯原始文本
                var videoInfo = new List<TestInfoItem>();
                using (ManagementObjectSearcher Video = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController"))
                {
                    foreach (ManagementObject VideoDevice_Object in Video.Get())
                    {
                        string VideoProcessor = (string)VideoDevice_Object["VideoProcessor"];
                        if (VideoProcessor != null)
                        {
                            videoInfo.Add(new TestInfoItem(VideoDevice_Object.GetPropertyValue("Name")?.ToString()));
                        }
                    }
                }
                hardwareInfo["GPU"] = videoInfo;

                // 显示器信息
                var monitorInfo = new List<TestInfoItem>();
                using (ManagementObjectSearcher Monitor = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE service='monitor'"))
                {
                    int monitorIndex = 1;
                    foreach (ManagementObject Monitor_Object in Monitor.Get())
                    {
                        monitorInfo.Add(new TestInfoItem("Test_Monitor_Format", new[] { monitorIndex.ToString(), Monitor_Object.GetPropertyValue("Name")?.ToString() }));
                        string[] hardwareIDs = Monitor_Object.GetPropertyValue("HardwareID") as string[];
                        string firstHardwareID = hardwareIDs?.FirstOrDefault();
                        if (firstHardwareID != null)
                        {
                            monitorInfo.Add(new TestInfoItem("Test_HardwareID", new[] { firstHardwareID }, isIndented: true));
                        }
                        monitorIndex++;
                    }
                }
                hardwareInfo["Monitor"] = monitorInfo;

                // 屏幕分辨率信息
                var resolutionInfo = new List<TestInfoItem>();
                using (ManagementObjectSearcher Video = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController"))
                {
                    foreach (ManagementObject DeskTop_Info in Video.Get())
                    {
                        try
                        {
                            string VideoProcessor = (string)DeskTop_Info["VideoProcessor"];
                            if (VideoProcessor != null)
                            {
                                string resolution = DeskTop_Info.GetPropertyValue("VideoModeDescription")?.ToString() ?? T("Test_Unknown", "Unknown");
                                string refreshRate = DeskTop_Info.GetPropertyValue("CurrentRefreshRate")?.ToString() ?? T("Test_Unknown", "Unknown");
                                resolutionInfo.Add(new TestInfoItem(DeskTop_Info.GetPropertyValue("Name")?.ToString()));
                                resolutionInfo.Add(new TestInfoItem($"{resolution} {refreshRate} Hz", isIndented: true));
                            }
                        }
                        catch (NullReferenceException)
                        {
                            resolutionInfo.Add(new TestInfoItem(DeskTop_Info.GetPropertyValue("Name")?.ToString()));
                            resolutionInfo.Add(new TestInfoItem(T("Test_NoMonitor", "No display connected"), isIndented: true));
                        }
                    }
                }
                hardwareInfo["Resolution"] = resolutionInfo;

                // 网卡信息 - 按优先级排序
                var netAdapterInfo = new List<TestInfoItem>();
                var netAdapterList = new List<(string name, int priority)>();

                using (ManagementObjectSearcher NetAdapter = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapter WHERE Manufacturer != 'Microsoft'"))
                {
                    foreach (ManagementObject NetAdapter_Object in NetAdapter.Get())
                    {
                        string name = NetAdapter_Object.GetPropertyValue("Name")?.ToString();
                        if (string.IsNullOrEmpty(name)) continue;

                        string pnpDeviceId = NetAdapter_Object.GetPropertyValue("PNPDeviceID")?.ToString() ?? "";
                        string manufacturer = NetAdapter_Object.GetPropertyValue("Manufacturer")?.ToString() ?? "";
                        var netConnectionStatus = NetAdapter_Object.GetPropertyValue("NetConnectionStatus");
                        bool isConnected = netConnectionStatus != null && Convert.ToInt32(netConnectionStatus) == 2;

                        int priority = GetNetworkAdapterPriority(name, pnpDeviceId, manufacturer, isConnected);
                        netAdapterList.Add((name, priority));
                    }
                }

                netAdapterList.Sort((a, b) => a.priority.CompareTo(b.priority));
                foreach (var adapter in netAdapterList)
                {
                    netAdapterInfo.Add(new TestInfoItem("Test_NetworkCard_Format", new[] { adapter.name }));
                }
                hardwareInfo["NetAdapter"] = netAdapterInfo;

                // IP/MAC信息 - 按优先级排序
                var networkInfo = new List<TestInfoItem>();
                var networkConfigList = new List<(string description, string macAddress, string[] ipAddresses, int priority)>();

                using (ManagementObjectSearcher NETconfig = new ManagementObjectSearcher("SELECT * FROM win32_NetworkAdapterConfiguration WHERE IPEnabled = True AND MACAddress != Null"))
                {
                    foreach (ManagementObject MNF in NETconfig.Get())
                    {
                        string description = MNF.GetPropertyValue("Description")?.ToString();
                        string macAddress = MNF.GetPropertyValue("MACAddress")?.ToString();
                        string[] ipAddresses = (string[])MNF["IPAddress"];

                        if (string.IsNullOrEmpty(description)) continue;

                        int priority = GetNetworkAdapterPriority(description, "", "", true);
                        networkConfigList.Add((description, macAddress, ipAddresses, priority));
                    }
                }

                networkConfigList.Sort((a, b) => a.priority.CompareTo(b.priority));

                foreach (var config in networkConfigList)
                {
                    networkInfo.Add(new TestInfoItem("Test_NetworkCard_Format", new[] { config.description }));

                    if (!string.IsNullOrEmpty(config.macAddress))
                    {
                        networkInfo.Add(new TestInfoItem("Test_MACAddress", new[] { config.macAddress }, isIndented: true));
                    }

                    if (config.ipAddresses != null && config.ipAddresses.Length > 0)
                    {
                        foreach (string ipAddress in config.ipAddresses)
                        {
                            networkInfo.Add(new TestInfoItem("Test_IPAddress", new[] { ipAddress }, isIndented: true));
                        }
                    }
                }
                hardwareInfo["NetworkConfig"] = networkInfo;

                // 声卡信息 - 纯原始文本
                var soundInfo = new List<TestInfoItem>();
                using (ManagementObjectSearcher Sound = new ManagementObjectSearcher("SELECT * FROM Win32_SoundDevice"))
                {
                    foreach (ManagementObject SoundDevice_Object in Sound.Get())
                    {
                        soundInfo.Add(new TestInfoItem(SoundDevice_Object.GetPropertyValue("Caption")?.ToString()));
                    }
                }
                hardwareInfo["Sound"] = soundInfo;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error collecting hardware info: {ex}");
                throw;
            }
        }

        /// <summary>
        /// 渲染单个信息条目为本地化字符串
        /// </summary>
        private string RenderItem(TestInfoItem item)
        {
            if (item.RawText != null)
                return item.RawText; // 纯文本（GPU名、分辨率等）
            if (item.ResKey != null && item.Args != null)
                return string.Format(T(item.ResKey, item.ResKey), item.Args); // 格式化文本
            return "";
        }

        private void BuildUI()
        {
            MainPanel.Children.Clear();
            MainPanel.ColumnDefinitions.Clear();

            if (PlaceholderText != null)
            {
                PlaceholderText.Visibility = Visibility.Collapsed;
            }

            double availableWidth = this.ActualWidth - 70;
            int columnCount = Math.Max(1, (int)(availableWidth / 488));

            var columnStacks = new List<StackPanel>();
            for (int i = 0; i < columnCount; i++)
            {
                MainPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var stackPanel = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Margin = new Thickness(0, 0, i < columnCount - 1 ? 8 : 0, 0)
                };
                Grid.SetColumn(stackPanel, i);
                MainPanel.Children.Add(stackPanel);
                columnStacks.Add(stackPanel);
            }

            int cardIndex = 0;
            foreach (var category in hardwareInfo)
            {
                if (category.Value.Count == 0) continue;

                var cardBorder = new Border
                {
                    Width = 480,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Margin = new Thickness(0, 0, 0, 8),
                    Padding = new Thickness(16, 14, 16, 14),
                    VerticalAlignment = VerticalAlignment.Top
                };

                cardBorder.SetResourceReference(Border.BackgroundProperty, "CardBackgroundFillColorDefaultBrush");
                cardBorder.SetResourceReference(Border.BorderBrushProperty, "CardStrokeColorDefaultBrush");

                var shadowColor = Application.Current.TryFindResource("ShadowColor") as Color? ?? Colors.Black;
                cardBorder.Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = shadowColor,
                    Direction = 270,
                    ShadowDepth = 2,
                    BlurRadius = 8,
                    Opacity = 0.2
                };

                var containerStack = new StackPanel();

                var headerPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 0, 0, 12)
                };

                var iconText = new TextBlock
                {
                    Text = GetCategoryIcon(category.Key),
                    FontFamily = (FontFamily)Application.Current.TryFindResource("SegoeIcons")
                                 ?? new FontFamily("Segoe MDL2 Assets"),
                    FontSize = 20,
                    Margin = new Thickness(0, 0, 10, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                iconText.SetResourceReference(TextBlock.ForegroundProperty, "AccentTextFillColorPrimaryBrush");
                headerPanel.Children.Add(iconText);

                var headerText = new TextBlock
                {
                    Text = GetLocalizedCategoryName(category.Key),
                    FontSize = 16,
                    FontWeight = FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontFamily = new FontFamily("Microsoft YaHei UI, Segoe UI")
                };
                headerText.SetResourceReference(TextBlock.ForegroundProperty, "AccentTextFillColorPrimaryBrush");
                headerPanel.Children.Add(headerText);

                containerStack.Children.Add(headerPanel);

                var separator = new Border
                {
                    Height = 1,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                separator.SetResourceReference(Border.BackgroundProperty, "DividerStrokeColorDefaultBrush");
                containerStack.Children.Add(separator);

                var contentStack = new StackPanel
                {
                    Margin = new Thickness(2, 0, 0, 0)
                };

                foreach (var item in category.Value)
                {
                    string displayText = RenderItem(item);

                    if (string.IsNullOrWhiteSpace(displayText)) continue;

                    var itemPanel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Margin = new Thickness(item.IsIndented ? 16 : 0, 2, 0, 2)
                    };

                    if (item.IsIndented)
                    {
                        var bullet = new TextBlock
                        {
                            Text = "\u25CF",
                            FontSize = 7,
                            Margin = new Thickness(0, 0, 6, 0),
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        bullet.SetResourceReference(TextBlock.ForegroundProperty, "AccentTextFillColorSecondaryBrush");
                        itemPanel.Children.Add(bullet);
                    }

                    var textBlock = new TextBlock
                    {
                        Text = displayText,
                        FontSize = item.IsIndented ? 12.5 : 13,
                        FontWeight = item.IsIndented ? FontWeights.Normal : FontWeights.Medium,
                        Margin = new Thickness(0),
                        TextWrapping = TextWrapping.Wrap,
                        FontFamily = new FontFamily("Consolas, Microsoft YaHei UI, Segoe UI")
                    };

                    textBlock.SetResourceReference(TextBlock.ForegroundProperty,
                        item.IsIndented ? "TextFillColorSecondaryBrush" : "TextFillColorPrimaryBrush");

                    textBlock.MouseEnter += (s, ev) =>
                    {
                        var hoverBrush = Application.Current.TryFindResource("SubtleFillColorSecondaryBrush") as Brush
                                        ?? new SolidColorBrush(Color.FromArgb(25, 0, 120, 215));
                        textBlock.Background = hoverBrush;
                        textBlock.Cursor = Cursors.Hand;
                    };
                    textBlock.MouseLeave += (s, ev) =>
                    {
                        textBlock.Background = Brushes.Transparent;
                        textBlock.Cursor = Cursors.Arrow;
                    };

                    var contextMenu = new ContextMenu();
                    var copyMenuItem = new MenuItem
                    {
                        Header = T("Test_CopyItem"),
                        FontFamily = new FontFamily("Microsoft YaHei UI")
                    };
                    copyMenuItem.Click += (s, ev) =>
                    {
                        try
                        {
                            TextCopy.ClipboardService.SetText(displayText);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(
                                T("Test_CopyError") + ex.Message,
                                T("ErrorTitle"),
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                        }
                    };
                    contextMenu.Items.Add(copyMenuItem);

                    var copyAllMenuItem = new MenuItem
                    {
                        Header = T("Test_CopyCategory"),
                        FontFamily = new FontFamily("Microsoft YaHei UI")
                    };
                    // 收集当前分类所有渲染文本
                    var catText = string.Join("\r\n", category.Value.Select(i => RenderItem(i)));
                    copyAllMenuItem.Click += (s, ev) =>
                    {
                        try
                        {
                            var allText = string.Join("\r\n", category.Value.Select(i => RenderItem(i)));
                            TextCopy.ClipboardService.SetText(allText);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(
                                T("Test_CopyError") + ex.Message,
                                T("ErrorTitle"),
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                        }
                    };
                    contextMenu.Items.Add(copyAllMenuItem);

                    textBlock.ContextMenu = contextMenu;

                    itemPanel.Children.Add(textBlock);
                    contentStack.Children.Add(itemPanel);
                }

                containerStack.Children.Add(contentStack);
                cardBorder.Child = containerStack;

                int targetColumn = cardIndex % columnCount;
                columnStacks[targetColumn].Children.Add(cardBorder);
                cardIndex++;
            }
        }

        private string GetCategoryIcon(string category)
        {
            return category switch
            {
                "Computer" => "\uE977",
                "OS" => "\uF4A5",
                "CPU" => "\uE950",
                "Disk" => "\uEDA2",
                "GPU" => "\uE7FC",
                "Monitor" => "\uE7F4",
                "Resolution" => "\uF57D",
                "NetAdapter" => "\uE968",
                "NetworkConfig" => "\uE8A1",
                "Sound" => "\uE767",
                _ => "\uE8F1"
            };
        }

        private static string GetLocalizedCategoryName(string categoryKey)
        {
            return categoryKey switch
            {
                "Computer" => T("Test_CatComputer"),
                "OS" => T("Test_CatOS"),
                "CPU" => T("Test_CatCPU"),
                "Disk" => T("Test_CatDisk"),
                "GPU" => T("Test_CatGPU"),
                "Monitor" => T("Test_CatMonitor"),
                "Resolution" => T("Test_CatResolution"),
                "NetAdapter" => T("Test_CatNetAdapter"),
                "NetworkConfig" => T("Test_CatNetworkConfig"),
                "Sound" => T("Test_CatSound"),
                _ => categoryKey
            };
        }

        private int GetNetworkAdapterPriority(string adapterName, string pnpDeviceId, string manufacturer, bool isConnected)
        {
            if (string.IsNullOrEmpty(adapterName))
                return 1000;

            string nameLower = adapterName.ToLower();
            string pnpLower = pnpDeviceId.ToLower();
            string mfgLower = manufacturer.ToLower();

            int basePriority;

            if (IsVirtualAdapter(nameLower, pnpLower, mfgLower))
                basePriority = 100;
            else if (nameLower.Contains("bluetooth") || pnpLower.Contains("bth"))
                basePriority = 80;
            else if (IsWirelessAdapter(nameLower, pnpLower))
                basePriority = 50;
            else if (IsWiredAdapter(nameLower, pnpLower))
                basePriority = 10;
            else
                basePriority = 90;

            return isConnected ? basePriority - 5 : basePriority;
        }

        private bool IsVirtualAdapter(string nameLower, string pnpLower, string mfgLower)
        {
            string[] virtualKeywords = {
                "virtual", "vmware", "virtualbox", "hyper-v", "vethernet",
                "vboxnet", "tap-windows", "openstack", "docker", "vnic",
                "tunnel", "loopback", "kdnic", "npcap", "vpn", "pptp",
                "l2tp", "ipsec", "nettap", "utun"
            };
            string[] virtualPnpKeywords = {
                "ven_1af4", "root\\", "netvsc", "vmbus", "vbox", "vmware", "mstunnel", "wan\\"
            };
            string[] virtualMfgKeywords = {
                "vmware", "oracle", "red hat", "qemu", "parallels", "citrix"
            };

            return virtualKeywords.Any(k => nameLower.Contains(k)) ||
                   virtualPnpKeywords.Any(k => pnpLower.Contains(k)) ||
                   virtualMfgKeywords.Any(k => mfgLower.Contains(k));
        }

        private bool IsWiredAdapter(string nameLower, string pnpLower)
        {
            string[] wiredKeywords = {
                "ethernet", "gigabit", "realtek", "intel.*connection",
                "killer ethernet", "marvell", "broadcom", "qualcomm atheros ar",
                "rtl8", "i219", "i211", "i225", "e1000", "82579", "pcie gbe"
            };

            if (wiredKeywords.Any(k => nameLower.Contains(k) &&
                !nameLower.Contains("wireless") && !nameLower.Contains("wi-fi") && !nameLower.Contains("wifi")))
                return true;

            return pnpLower.Contains("pci\\ven") &&
                (pnpLower.Contains("&cc_0200") || pnpLower.Contains("ven_10ec") || pnpLower.Contains("ven_8086"));
        }

        private bool IsWirelessAdapter(string nameLower, string pnpLower)
        {
            string[] wirelessKeywords = {
                "wi-fi", "wifi", "wireless", "802.11", "wlan",
                "intel.*wi-fi", "intel.*wireless", "qualcomm", "broadcom.*802.11",
                "realtek.*wireless", "mediatek.*wi-fi", "killer.*wi-fi"
            };

            if (wirelessKeywords.Any(k => nameLower.Contains(k.Replace(".*", ""))))
                return true;

            return pnpLower.Contains("pci\\ven") && pnpLower.Contains("&cc_0280");
        }

        private void Info_Click(object sender, RoutedEventArgs e)
        {
            iNKORE.UI.WPF.Modern.Controls.MessageBox.Show(
                T("Test_InfoContent"),
                T("Test_InfoTitle"),
                MessageBoxButton.OK,
                MessageBoxImage.Question);
        }
    }
}
