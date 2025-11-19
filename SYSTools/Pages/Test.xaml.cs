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

namespace SYSTools.Pages
{
    /// <summary>
    /// Test.xaml 的交互逻辑
    /// </summary>
    public partial class Test : Page
    {
        private Dictionary<string, List<string>> hardwareInfo;

        public Test()
        {
            InitializeComponent();
            hardwareInfo = new Dictionary<string, List<string>>();
            iNKORE.UI.WPF.Modern.Controls.MessageBox.DefaultBackdropType = BackdropType.Acrylic11;
            
            // 监听窗口大小变化，重新布局
            this.SizeChanged += (s, e) =>
            {
                if (hardwareInfo.Count > 0)
                {
                    Dispatcher.BeginInvoke(new Action(() => BuildUI()), 
                        System.Windows.Threading.DispatcherPriority.Background);
                }
            };
        }

        private async void TestBotton_Click(object sender, RoutedEventArgs e)
        {
            try 
            {
                TestBotton.IsEnabled = false;
                TestBotton.Content = "检测中...";
                
                await Task.Run(() => CollectHardwareInfo());
                
                Dispatcher.Invoke(() =>
                {
                    BuildUI();
                    TestBotton.Content = "检测配置";
                    TestBotton.IsEnabled = true;
                });
                
                iNKORE.UI.WPF.Modern.Controls.MessageBox.Show(
                    "配置获取成功", 
                    "系统配置测试", 
                    MessageBoxButton.OK, 
                    SegoeFluentIcons.SpecialEffectSize);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting hardware info: {ex}");
                TestBotton.Content = "检测配置";
                TestBotton.IsEnabled = true;
                
                iNKORE.UI.WPF.Modern.Controls.MessageBox.Show(
                    "系统配置获取失败 请联系开发者", 
                    "系统配置测试", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Warning);
            }
        }

        private void CollectHardwareInfo()
        {
            hardwareInfo.Clear();

            try
            {
                // 计算机信息
                var computerInfo = new List<string>();
                using (ManagementObjectSearcher cmsystem = new ManagementObjectSearcher("SELECT * FROM win32_computersystem"))
                {
                    foreach (ManagementObject cmsys in cmsystem.Get())
                    {
                        double memory = Convert.ToDouble(cmsys.GetPropertyValue("totalphysicalmemory")) / 1024 / 1024 / 1024;
                        memory = (int)memory;

                        computerInfo.Add($"工作组: {cmsys.GetPropertyValue("domain")}");
                        computerInfo.Add($"计算机名称: {cmsys.GetPropertyValue("__server")}");
                        computerInfo.Add($"计算机制造商: {cmsys.GetPropertyValue("manufacturer")}");
                        computerInfo.Add($"总内存: {memory + 1} GB");
                    }
                }
                hardwareInfo["计算机信息"] = computerInfo;

                // 操作系统信息
                var osInfo = new List<string>();
                using (ManagementObjectSearcher OpSystem = new ManagementObjectSearcher("SELECT * FROM win32_OperatingSystem"))
                {
                    foreach (ManagementObject OpSys in OpSystem.Get())
                    {
                        osInfo.Add($"Windows版本: {OpSys.GetPropertyValue("Caption")}");
                        osInfo.Add($"系统位数: {OpSys.GetPropertyValue("OSArchitecture")}操作系统");
                        osInfo.Add($"内核版本号: {OpSys.GetPropertyValue("Version")}");
                    }
                }
                hardwareInfo["操作系统信息"] = osInfo;

                // CPU信息
                var cpuInfo = new List<string>();
                using (ManagementObjectSearcher Process = new ManagementObjectSearcher("SELECT * FROM win32_Processor"))
                {
                    foreach (ManagementObject CPU in Process.Get())
                    {
                        cpuInfo.Add($"CPU型号: {CPU.GetPropertyValue("Name")}");
                        cpuInfo.Add($"核心数: {CPU.GetPropertyValue("NumberOfCores")} 核");
                        cpuInfo.Add($"线程数: {CPU.GetPropertyValue("NumberOfLogicalProcessors")} 线程");
                    }
                }
                hardwareInfo["处理器信息"] = cpuInfo;

                // 硬盘信息
                var diskInfo = new List<string>();
                using (ManagementObjectSearcher DiskDrive = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive"))
                {
                    int diskIndex = 1;
                    foreach (ManagementObject Disk in DiskDrive.Get())
                    {
                        ulong size = Convert.ToUInt64(Disk.GetPropertyValue("Size"));
                        double sizeInGB = size / (1024.0 * 1024.0 * 1024.0);
                        double sizeInTB = sizeInGB / 1024.0;
                        string model = Disk.GetPropertyValue("Model").ToString();

                        diskInfo.Add($"硬盘 {diskIndex}: {model}");
                        diskInfo.Add($"    容量: {sizeInGB:F2} GB ({sizeInTB:F2} TB)");
                        diskIndex++;
                    }
                }
                hardwareInfo["硬盘信息"] = diskInfo;

                // 显卡信息
                var videoInfo = new List<string>();
                using (ManagementObjectSearcher Video = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController"))
                {
                    foreach (ManagementObject VideoDevice_Object in Video.Get())
                    {
                        string VideoProcessor = (string)VideoDevice_Object["VideoProcessor"];
                        if (VideoProcessor != null)
                        {
                            videoInfo.Add(VideoDevice_Object.GetPropertyValue("Name").ToString());
                        }
                    }
                }
                hardwareInfo["显卡信息"] = videoInfo;

                // 显示器信息
                var monitorInfo = new List<string>();
                using (ManagementObjectSearcher Monitor = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE service='monitor'"))
                {
                    int monitorIndex = 1;
                    foreach (ManagementObject Monitor_Object in Monitor.Get())
                    {
                        monitorInfo.Add($"显示器 {monitorIndex}: {Monitor_Object.GetPropertyValue("Name")}");
                        string[] hardwareIDs = Monitor_Object.GetPropertyValue("HardwareID") as string[];
                        string firstHardwareID = hardwareIDs?.FirstOrDefault();
                        if (firstHardwareID != null)
                        {
                            monitorInfo.Add($"    硬件ID: {firstHardwareID}");
                        }
                        monitorIndex++;
                    }
                }
                hardwareInfo["显示器信息"] = monitorInfo;

                // 屏幕分辨率信息
                var resolutionInfo = new List<string>();
                using (ManagementObjectSearcher Video = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController"))
                {
                    foreach (ManagementObject DeskTop_Info in Video.Get())
                    {
                        try
                        {
                            string VideoProcessor = (string)DeskTop_Info["VideoProcessor"];
                            if (VideoProcessor != null)
                            {
                                string resolution = DeskTop_Info.GetPropertyValue("VideoModeDescription")?.ToString() ?? "未知";
                                string refreshRate = DeskTop_Info.GetPropertyValue("CurrentRefreshRate")?.ToString() ?? "未知";
                                resolutionInfo.Add($"{DeskTop_Info.GetPropertyValue("Name")}: {resolution} {refreshRate} Hz");
                            }
                        }
                        catch (NullReferenceException)
                        {
                            resolutionInfo.Add($"{DeskTop_Info.GetPropertyValue("Name")}: 屏幕未接入");
                        }
                    }
                }
                hardwareInfo["屏幕分辨率信息"] = resolutionInfo;

                // 网卡信息 - 按优先级排序
                var netAdapterInfo = new List<string>();
                var netAdapterList = new List<(string name, int priority)>();
                
                using (ManagementObjectSearcher NetAdapter = new ManagementObjectSearcher(@"SELECT * FROM Win32_NetworkAdapter WHERE Manufacturer != 'Microsoft' AND NOT PNPDeviceID LIKE 'ROOT\\%'"))
                {
                    foreach (ManagementObject NetAdapter_Object in NetAdapter.Get())
                    {
                        string name = NetAdapter_Object.GetPropertyValue("Name")?.ToString();
                        if (string.IsNullOrEmpty(name)) continue;
                        
                        // 获取更多信息用于判断
                        string pnpDeviceId = NetAdapter_Object.GetPropertyValue("PNPDeviceID")?.ToString() ?? "";
                        string manufacturer = NetAdapter_Object.GetPropertyValue("Manufacturer")?.ToString() ?? "";
                        var netConnectionStatus = NetAdapter_Object.GetPropertyValue("NetConnectionStatus");
                        bool isConnected = netConnectionStatus != null && Convert.ToInt32(netConnectionStatus) == 2; // 2 = Connected
                        
                        // 确定优先级：有线 > 无线 > 其他，已连接的排在前面
                        int priority = GetNetworkAdapterPriority(name, pnpDeviceId, manufacturer, isConnected);
                        
                        Debug.WriteLine($"网卡: {name}, 制造商: {manufacturer}, PNP: {pnpDeviceId}, 连接状态: {isConnected}, 优先级: {priority}");
                        
                        netAdapterList.Add((name, priority));
                    }
                }
                
                // 按优先级排序
                netAdapterList.Sort((a, b) => a.priority.CompareTo(b.priority));
                netAdapterInfo.AddRange(netAdapterList.Select(x => x.name));
                hardwareInfo["网卡信息"] = netAdapterInfo;

                // IP/MAC信息 - 按优先级排序
                var networkInfo = new List<string>();
                var networkConfigList = new List<(string description, string macAddress, string[] ipAddresses, int priority)>();
                
                using (ManagementObjectSearcher NETconfig = new ManagementObjectSearcher("SELECT * FROM win32_NetworkAdapterConfiguration WHERE IPEnabled = True AND MACAddress != Null"))
                {
                    foreach (ManagementObject MNF in NETconfig.Get())
                    {
                        string description = MNF.GetPropertyValue("Description")?.ToString();
                        string macAddress = MNF.GetPropertyValue("MACAddress")?.ToString();
                        string[] ipAddresses = (string[])MNF["IPAddress"];
                        
                        if (string.IsNullOrEmpty(description)) continue;
                        
                        // 确定优先级（使用简化版本，因为这里无法获取 PNPDeviceID）
                        int priority = GetNetworkAdapterPriority(description, "", "", true); // 这里都是已启用的网卡
                        
                        networkConfigList.Add((description, macAddress, ipAddresses, priority));
                    }
                }
                
                // 按优先级排序
                networkConfigList.Sort((a, b) => a.priority.CompareTo(b.priority));
                
                // 添加到列表
                foreach (var config in networkConfigList)
                {
                    networkInfo.Add($"网卡: {config.description}");
                    networkInfo.Add($"    MAC地址: {config.macAddress}");
                    
                    if (config.ipAddresses != null && config.ipAddresses.Length > 0)
                    {
                        foreach (string ipAddress in config.ipAddresses)
                        {
                            networkInfo.Add($"    IP地址: {ipAddress}");
                        }
                    }
                    networkInfo.Add(""); // 空行分隔
                }
                hardwareInfo["网络配置信息"] = networkInfo;

                // 声卡信息
                var soundInfo = new List<string>();
                using (ManagementObjectSearcher Sound = new ManagementObjectSearcher("SELECT * FROM Win32_SoundDevice"))
                {
                    foreach (ManagementObject SoundDevice_Object in Sound.Get())
                    {
                        soundInfo.Add(SoundDevice_Object.GetPropertyValue("Caption").ToString());
                    }
                }
                hardwareInfo["声卡信息"] = soundInfo;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error collecting hardware info: {ex}");
                throw;
            }
        }

        private void BuildUI()
        {
            MainPanel.Children.Clear();
            MainPanel.ColumnDefinitions.Clear();

            if (PlaceholderText != null)
            {
                PlaceholderText.Visibility = Visibility.Collapsed;
            }

            // 计算列数（根据可用宽度，每张卡片480px + 8px间距）
            double availableWidth = this.ActualWidth - 70; // 减去边距
            int columnCount = Math.Max(1, (int)(availableWidth / 488));

            // 创建列定义和列容器
            var columnStacks = new List<StackPanel>();
            for (int i = 0; i < columnCount; i++)
            {
                MainPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                
                var stackPanel = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Margin = new Thickness(0, 0, i < columnCount - 1 ? 8 : 0, 0) // 最后一列无右边距
                };
                Grid.SetColumn(stackPanel, i);
                MainPanel.Children.Add(stackPanel);
                columnStacks.Add(stackPanel);
            }

            // 将卡片轮询分配到各列（实现高度平衡）
            int cardIndex = 0;
            foreach (var category in hardwareInfo)
            {
                if (category.Value.Count == 0) continue;

                // 创建卡片容器 - 固定宽度，自适应高度
                var cardBorder = new Border
                {
                    Width = 480, // 卡片固定宽度
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Margin = new Thickness(0, 0, 0, 8), // 只有下边距
                    Padding = new Thickness(16, 14, 16, 14),
                    VerticalAlignment = VerticalAlignment.Top
                };

                // 设置主题感知的背景和边框颜色
                cardBorder.SetResourceReference(Border.BackgroundProperty, "CardBackgroundFillColorDefaultBrush");
                cardBorder.SetResourceReference(Border.BorderBrushProperty, "CardStrokeColorDefaultBrush");

                // 添加阴影效果（浅色主题下更明显）
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

                // 标题区域
                var headerPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 0, 0, 12)
                };

                // 标题图标
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

                // 标题文本
                var headerText = new TextBlock
                {
                    Text = category.Key,
                    FontSize = 16,
                    FontWeight = FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontFamily = new FontFamily("Microsoft YaHei UI, Segoe UI")
                };
                headerText.SetResourceReference(TextBlock.ForegroundProperty, "AccentTextFillColorPrimaryBrush");
                headerPanel.Children.Add(headerText);

                containerStack.Children.Add(headerPanel);

                // 分隔线
                var separator = new Border
                {
                    Height = 1,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                separator.SetResourceReference(Border.BackgroundProperty, "DividerStrokeColorDefaultBrush");
                containerStack.Children.Add(separator);

                // 内容容器 - 自适应高度，不使用滚动条
                var contentStack = new StackPanel
                {
                    Margin = new Thickness(2, 0, 0, 0)
                };

                // 添加属性项
                foreach (var property in category.Value)
                {
                    if (string.IsNullOrWhiteSpace(property)) continue;

                    bool isIndented = property.StartsWith("    ");
                    string displayText = isIndented ? property.TrimStart() : property;

                    var itemPanel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Margin = new Thickness(isIndented ? 16 : 0, 2, 0, 2)
                    };

                    // 如果是缩进项，添加一个小圆点
                    if (isIndented)
                    {
                        var bullet = new TextBlock
                        {
                            Text = "●",
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
                        FontSize = isIndented ? 12.5 : 13,
                        FontWeight = isIndented ? FontWeights.Normal : FontWeights.Medium,
                        Margin = new Thickness(0),
                        TextWrapping = TextWrapping.Wrap,
                        FontFamily = new FontFamily("Consolas, Microsoft YaHei UI, Segoe UI")
                    };
                    
                    // 设置主题感知的文本颜色
                    if (isIndented)
                    {
                        textBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
                    }
                    else
                    {
                        textBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");
                    }

                    // 鼠标悬停效果
                    textBlock.MouseEnter += (s, e) =>
                    {
                        var hoverBrush = Application.Current.TryFindResource("SubtleFillColorSecondaryBrush") as Brush
                                        ?? new SolidColorBrush(Color.FromArgb(25, 0, 120, 215));
                        textBlock.Background = hoverBrush;
                        textBlock.Cursor = Cursors.Hand;
                    };
                    textBlock.MouseLeave += (s, e) =>
                    {
                        textBlock.Background = Brushes.Transparent;
                        textBlock.Cursor = Cursors.Arrow;
                    };

                    // 添加右键菜单
                    var contextMenu = new ContextMenu();
                    var copyMenuItem = new MenuItem
                    {
                        Header = "复制此项",
                        FontFamily = new FontFamily("Microsoft YaHei UI")
                    };
                    copyMenuItem.Click += (s, e) =>
                    {
                        try
                        {
                            TextCopy.ClipboardService.SetText(displayText);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("无法复制内容: " + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    };
                    contextMenu.Items.Add(copyMenuItem);

                    // 复制整个分类
                    var copyAllMenuItem = new MenuItem
                    {
                        Header = "复制整个分类",
                        FontFamily = new FontFamily("Microsoft YaHei UI")
                    };
                    copyAllMenuItem.Click += (s, e) =>
            {
                try
                {
                            var allText = string.Join("\r\n", category.Value);
                            TextCopy.ClipboardService.SetText($"{category.Key}\r\n{allText}");
                }
                catch (Exception ex)
                {
                            MessageBox.Show("无法复制内容: " + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    };
                    contextMenu.Items.Add(copyAllMenuItem);

                    textBlock.ContextMenu = contextMenu;

                    itemPanel.Children.Add(textBlock);
                    contentStack.Children.Add(itemPanel);
                }

                containerStack.Children.Add(contentStack);
                cardBorder.Child = containerStack;
                
                // 将卡片轮询分配到各列
                int targetColumn = cardIndex % columnCount;
                columnStacks[targetColumn].Children.Add(cardBorder);
                cardIndex++;
            }
        }

        // 根据分类返回对应的 Segoe MDL2 Assets 图标
        private string GetCategoryIcon(string category)
        {
            return category switch
            {
                "计算机信息" => "\uE977",      // ComputerLaptop
                "操作系统信息" => "\uF4A5",    // WindowsLogo
                "处理器信息" => "\uE950",      // System
                "硬盘信息" => "\uEDA2",        // HardDrive
                "显卡信息" => "\uE7FC",        // Game
                "显示器信息" => "\uE7F4",      // TVMonitor
                "屏幕分辨率信息" => "\uF57D",  // ScreenCast
                "网卡信息" => "\uE968",        // NetworkTower
                "网络配置信息" => "\uE8A1",    // Plug
                "声卡信息" => "\uE767",        // Volume
                _ => "\uE8F1"                   // GenericScan
            };
        }

        // 获取网卡优先级（数字越小优先级越高）
        private int GetNetworkAdapterPriority(string adapterName, string pnpDeviceId, string manufacturer, bool isConnected)
        {
            if (string.IsNullOrEmpty(adapterName))
                return 1000;

            string nameLower = adapterName.ToLower();
            string pnpLower = pnpDeviceId.ToLower();
            string mfgLower = manufacturer.ToLower();

            // 基础优先级
            int basePriority;

            // 先检查虚拟网卡（最低优先级）- 通过多个维度判断
            if (IsVirtualAdapter(nameLower, pnpLower, mfgLower))
            {
                basePriority = 100; // 虚拟网卡最低优先级
            }
            // 检查蓝牙网卡
            else if (nameLower.Contains("bluetooth") || pnpLower.Contains("bth"))
            {
                basePriority = 80;
            }
            // 检查无线网卡
            else if (IsWirelessAdapter(nameLower, pnpLower))
            {
                basePriority = 50;
            }
            // 检查有线网卡
            else if (IsWiredAdapter(nameLower, pnpLower))
            {
                basePriority = 10;
            }
            else
            {
                // 其他未识别的网卡
                basePriority = 90;
            }

            // 已连接的网卡优先级更高（减去5）
            if (isConnected)
            {
                basePriority -= 5;
            }

            return basePriority;
        }

        // 判断是否为虚拟网卡
        private bool IsVirtualAdapter(string nameLower, string pnpLower, string mfgLower)
        {
            // 虚拟网卡关键词
            string[] virtualKeywords = {
                "virtual", "vmware", "virtualbox", "hyper-v", "vethernet",
                "vboxnet", "tap-windows", "openstack", "docker", "vnic",
                "tunnel", "loopback", "kdnic", "npcap", "vpn", "pptp",
                "l2tp", "ipsec", "nettap", "utun"
            };

            // PNP 设备 ID 中的虚拟网卡特征
            string[] virtualPnpKeywords = {
                "ven_1af4", // QEMU/KVM
                "root\\", // 虚拟设备通常以 ROOT 开头
                "netvsc", // Hyper-V
                "vmbus", // Hyper-V
                "vbox", // VirtualBox
                "vmware", // VMware
                "mstunnel", // Microsoft Tunnel
                "wan\\" // WAN Miniport
            };

            // 制造商中的虚拟网卡特征
            string[] virtualMfgKeywords = {
                "vmware", "oracle", "red hat", "qemu", "parallels", "citrix"
            };

            // 检查名称
            foreach (var keyword in virtualKeywords)
            {
                if (nameLower.Contains(keyword))
                    return true;
            }

            // 检查 PNP 设备 ID
            foreach (var keyword in virtualPnpKeywords)
            {
                if (pnpLower.Contains(keyword))
                    return true;
            }

            // 检查制造商
            foreach (var keyword in virtualMfgKeywords)
            {
                if (mfgLower.Contains(keyword))
                    return true;
            }

            return false;
        }

        // 判断是否为有线网卡
        private bool IsWiredAdapter(string nameLower, string pnpLower)
        {
            string[] wiredKeywords = {
                "ethernet", "gigabit", "realtek", "intel.*connection",
                "killer ethernet", "marvell", "broadcom", "qualcomm atheros ar",
                "rtl8", "i219", "i211", "i225", "e1000", "82579", "pcie gbe"
            };

            foreach (var keyword in wiredKeywords)
            {
                if (nameLower.Contains(keyword) && 
                    !nameLower.Contains("wireless") && 
                    !nameLower.Contains("wi-fi") &&
                    !nameLower.Contains("wifi"))
                {
                    return true;
                }
            }

            // PCI 以太网卡特征
            if (pnpLower.Contains("pci\\ven") && 
                (pnpLower.Contains("&cc_0200") || // 网络控制器 - 以太网
                 pnpLower.Contains("ven_10ec") || // Realtek
                 pnpLower.Contains("ven_8086")))  // Intel
            {
                return true;
            }

            return false;
        }

        // 判断是否为无线网卡
        private bool IsWirelessAdapter(string nameLower, string pnpLower)
        {
            string[] wirelessKeywords = {
                "wi-fi", "wifi", "wireless", "802.11", "wlan",
                "intel.*wi-fi", "intel.*wireless", "qualcomm", "broadcom.*802.11",
                "realtek.*wireless", "mediatek.*wi-fi", "killer.*wi-fi"
            };

            foreach (var keyword in wirelessKeywords)
            {
                if (nameLower.Contains(keyword.Replace(".*", "")))
                    return true;
            }

            // PCI 无线网卡特征
            if (pnpLower.Contains("pci\\ven") && 
                pnpLower.Contains("&cc_0280")) // 网络控制器 - 无线
            {
                return true;
            }

            return false;
        }

        private void Info_Click(object sender, RoutedEventArgs e)
        {
            iNKORE.UI.WPF.Modern.Controls.MessageBox.Show(
                "配置提取基于 WMI (Windows Management Instrumentation)\r\n\r\n" +
                "获取时会发生短暂卡顿，这是正常现象，请耐心等待。\r\n\r\n" +
                "所有信息实时获取，无需保存文件。", 
                "系统配置检测说明", 
                MessageBoxButton.OK, 
                MessageBoxImage.Question);
        }
    }
}
