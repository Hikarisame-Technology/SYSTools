using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Media;
using LibreHardwareMonitor.Hardware;
using System.Linq;
using System.Diagnostics;
using SYSTools.Services;

namespace SYSTools.Pages
{
    /// <summary>
    /// HardwareMonitor.xaml 的交互逻辑
    /// </summary>
    public partial class HardwareMonitor : Page
    {
        private readonly HardwareMonitorService hardwareService;
        private readonly Computer computer; // 从服务获取的 Computer 引用
        private readonly DispatcherTimer timer;
        private const int RefreshInterval = 2; // 刷新间隔（秒）
        private readonly Dictionary<string, Dictionary<string, TextBlock>> sensorTextBlocks = new Dictionary<string, Dictionary<string, TextBlock>>();
        private bool isInitialized = false;

        public HardwareMonitor()
        {
            InitializeComponent();
            
            // 使用共享的硬件监控服务
            hardwareService = HardwareMonitorService.Instance;
            hardwareService.Initialize();
            computer = hardwareService.GetComputer();

            // 初始化定时器
            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(RefreshInterval)
            };
            timer.Tick += Timer_Tick;

            // 注册页面事件
            Loaded += HardwareMonitor_Loaded;
            Unloaded += HardwareMonitor_Unloaded;
            IsVisibleChanged += HardwareMonitor_IsVisibleChanged;
            
            // 监听窗口大小变化
            this.SizeChanged += (s, e) =>
            {
                if (isInitialized)
                {
                    Dispatcher.BeginInvoke(new Action(() => BuildUI()), 
                        System.Windows.Threading.DispatcherPriority.Background);
                }
            };
        }

        private void HardwareMonitor_Loaded(object sender, RoutedEventArgs e)
        {
            if (!isInitialized)
            {
                BuildUI();
                isInitialized = true;
            }
            StartMonitoring();
        }

        private void HardwareMonitor_Unloaded(object sender, RoutedEventArgs e)
        {
            StopMonitoring();
        }

        private void HardwareMonitor_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is bool isVisible)
            {
                if (isVisible)
                {
                    if (!isInitialized)
                    {
                        BuildUI();
                        isInitialized = true;
                    }
                    StartMonitoring();
                }
                else
                {
                    StopMonitoring();
                }
            }
        }

        private void StartMonitoring()
        {
            try
            {
                UpdateSensorValues();
                timer.Start();
                Debug.WriteLine("Hardware monitoring started");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error starting monitoring: {ex}");
            }
        }

        private void StopMonitoring()
        {
            timer.Stop();
            Debug.WriteLine("Hardware monitoring stopped");
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                UpdateSensorValues();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating sensor values: {ex}");
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateSensorValues();
        }

        private void BuildUI()
        {
            MainPanel.Children.Clear();
            MainPanel.ColumnDefinitions.Clear();
            sensorTextBlocks.Clear();

            // 计算可用宽度和列数
            double availableWidth = this.ActualWidth - 70;
            if (availableWidth < 400) availableWidth = 1000; // 默认宽度
            
            int columnCount = Math.Max(1, (int)(availableWidth / 400)); // 每列最小400px

            // 创建列
            var columnStacks = new List<StackPanel>();
            for (int i = 0; i < columnCount; i++)
            {
                MainPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                
                var stackPanel = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Margin = new Thickness(0, 0, i < columnCount - 1 ? 8 : 0, 0)
                };
                Grid.SetColumn(stackPanel, i);
                MainPanel.Children.Add(stackPanel);
                columnStacks.Add(stackPanel);
            }

            hardwareService.Update();

            // 按硬件类型分组，过滤掉没有有效传感器的硬件
            var hardwareGroups = computer.Hardware
                .GroupBy(h => h.HardwareType)
                .Where(g => HasValidSensors(g.ToList())) // 只保留有数据的硬件类型
                .OrderBy(g => GetHardwareTypePriority(g.Key));

            int cardIndex = 0;
            foreach (var group in hardwareGroups)
            {
                var card = CreateHardwareCard(group.Key, group.ToList());
                if (card != null)
                {
                    int targetColumn = cardIndex % columnCount;
                    columnStacks[targetColumn].Children.Add(card);
                    cardIndex++;
                }
            }
        }

        // 检查硬件列表是否有有效的传感器数据
        private bool HasValidSensors(List<IHardware> hardwareList)
        {
            foreach (var hardware in hardwareList)
            {
                if (hardware.Sensors.Any(s => s.Value.HasValue))
                {
                    return true;
                }
            }
            return false;
        }

        private Border CreateHardwareCard(HardwareType hardwareType, List<IHardware> hardwareList)
        {
            if (hardwareList.Count == 0) return null;

            var cardBorder = new Border
            {
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(16, 14, 16, 14),
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Stretch // 自适应宽度
            };

            cardBorder.SetResourceReference(Border.BackgroundProperty, "CardBackgroundFillColorDefaultBrush");
            cardBorder.SetResourceReference(Border.BorderBrushProperty, "CardStrokeColorDefaultBrush");

            cardBorder.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Colors.Black,
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

            var iconText = new TextBlock
            {
                Text = GetHardwareIcon(hardwareType),
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
                Text = GetHardwareTypeName(hardwareType),
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

            // 硬件列表
            int validHardwareCount = 0;
            foreach (var hardware in hardwareList)
            {
                var hardwarePanel = CreateHardwarePanel(hardware);
                if (hardwarePanel != null)
                {
                    containerStack.Children.Add(hardwarePanel);
                    validHardwareCount++;
                }
            }

            // 如果没有有效的硬件面板，返回 null
            if (validHardwareCount == 0)
            {
                return null;
            }

            cardBorder.Child = containerStack;
            return cardBorder;
        }

        private StackPanel CreateHardwarePanel(IHardware hardware)
        {
            // 先检查是否有有效的传感器数据
            if (!hardware.Sensors.Any(s => s.Value.HasValue))
            {
                return null; // 没有数据则不创建面板
            }

            var panel = new StackPanel
            {
                Margin = new Thickness(0, 0, 0, 12)
            };

            // 硬件名称
            var nameText = new TextBlock
            {
                Text = hardware.Name,
                FontSize = 14,
                FontWeight = FontWeights.Medium,
                Margin = new Thickness(0, 0, 0, 8),
                FontFamily = new FontFamily("Microsoft YaHei UI, Segoe UI")
            };
            nameText.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");
            panel.Children.Add(nameText);

            // 传感器网格
            var sensorGrid = new Grid
            {
                Margin = new Thickness(8, 0, 0, 0)
            };
            sensorGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            sensorGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            int row = 0;
            var sensorsToDisplay = hardware.Sensors.Where(s => s.Value.HasValue).OrderBy(s => GetSensorPriority(s.SensorType));

            foreach (var sensor in sensorsToDisplay)
            {
                sensorGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var sensorNameText = new TextBlock
                {
                    Text = GetSensorDisplayName(sensor),
                    FontSize = 12.5,
                    Margin = new Thickness(0, 2, 10, 2),
                    FontFamily = new FontFamily("Consolas, Microsoft YaHei UI, Segoe UI")
                };
                sensorNameText.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
                Grid.SetRow(sensorNameText, row);
                Grid.SetColumn(sensorNameText, 0);
                sensorGrid.Children.Add(sensorNameText);

                var sensorValueText = new TextBlock
                {
                    Text = FormatSensorValue(sensor),
                    FontSize = 12.5,
                    FontWeight = FontWeights.Medium,
                    Margin = new Thickness(0, 2, 0, 2),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    FontFamily = new FontFamily("Consolas, Microsoft YaHei UI, Segoe UI")
                };
                
                // 根据传感器类型设置颜色
                ApplySensorColor(sensorValueText, sensor);
                
                Grid.SetRow(sensorValueText, row);
                Grid.SetColumn(sensorValueText, 1);
                sensorGrid.Children.Add(sensorValueText);

                // 保存引用以便更新
                string sensorKey = $"{hardware.Identifier}_{sensor.Identifier}";
                if (!sensorTextBlocks.ContainsKey(hardware.Identifier.ToString()))
                {
                    sensorTextBlocks[hardware.Identifier.ToString()] = new Dictionary<string, TextBlock>();
                }
                sensorTextBlocks[hardware.Identifier.ToString()][sensor.Identifier.ToString()] = sensorValueText;

                row++;
            }

            if (sensorGrid.RowDefinitions.Count > 0)
            {
                panel.Children.Add(sensorGrid);
            }

            return panel.Children.Count > 1 ? panel : null;
        }

        private void UpdateSensorValues()
        {
            hardwareService.Update();

            foreach (IHardware hardware in computer.Hardware)
            {
                if (!sensorTextBlocks.ContainsKey(hardware.Identifier.ToString()))
                    continue;

                foreach (ISensor sensor in hardware.Sensors)
                {
                    if (sensorTextBlocks[hardware.Identifier.ToString()].TryGetValue(sensor.Identifier.ToString(), out TextBlock textBlock))
                    {
                        textBlock.Text = FormatSensorValue(sensor);
                        ApplySensorColor(textBlock, sensor);
                    }
                }
            }
        }

        private void ApplySensorColor(TextBlock textBlock, ISensor sensor)
        {
            if (sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue)
            {
                float temp = sensor.Value.Value;
                
                if (temp < 60)
                {
                    // 绿色 - 安全温度
                    textBlock.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129)); // 绿色
                }
                else if (temp < 80)
                {
                    // 黄色 - 警告温度
                    textBlock.Foreground = new SolidColorBrush(Color.FromRgb(245, 158, 11)); // 黄色
                }
                else
                {
                    // 红色 - 危险温度
                    textBlock.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // 红色
                }
            }
            else if (sensor.SensorType == SensorType.Load && sensor.Value.HasValue)
            {
                float load = sensor.Value.Value;
                
                if (load < 60)
                {
                    textBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");
                }
                else if (load < 85)
                {
                    textBlock.Foreground = new SolidColorBrush(Color.FromRgb(245, 158, 11)); // 黄色
                }
                else
                {
                    textBlock.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // 红色
                }
            }
            else
            {
                textBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");
            }
        }

        private string FormatSensorValue(ISensor sensor)
        {
            if (!sensor.Value.HasValue)
                return "N/A";

            return sensor.SensorType switch
            {
                SensorType.Temperature => $"{sensor.Value:F1} °C",
                SensorType.Load => $"{sensor.Value:F0} %",
                SensorType.Fan => $"{sensor.Value:F0} RPM",
                SensorType.Flow => $"{sensor.Value:F0} L/h",
                SensorType.Control => $"{sensor.Value:F0} %",
                SensorType.Level => $"{sensor.Value:F0} %",
                SensorType.Power => $"{sensor.Value:F1} W",
                SensorType.Data => sensor.Value.Value >= 1024 ? $"{sensor.Value.Value / 1024:F2} TB" : $"{sensor.Value:F1} GB",
                SensorType.SmallData => sensor.Value.Value >= 1024 ? $"{sensor.Value.Value / 1024:F2} GB" : $"{sensor.Value:F0} MB",
                SensorType.Voltage => $"{sensor.Value:F2} V",
                SensorType.Clock => sensor.Value.Value >= 1000 ? $"{sensor.Value.Value / 1000:F2} GHz" : $"{sensor.Value:F0} MHz",
                SensorType.Throughput => $"{sensor.Value:F1} MB/s",
                SensorType.TimeSpan => $"{TimeSpan.FromSeconds((double)sensor.Value):hh\\:mm\\:ss}",
                SensorType.Energy => $"{sensor.Value:F2} Wh",
                SensorType.Factor => $"{sensor.Value:F2}",
                _ => $"{sensor.Value:F1}"
            };
        }

        private string GetSensorDisplayName(ISensor sensor)
        {
            // 简化传感器名称，添加中文翻译
            string baseName = sensor.Name;
            
            return sensor.SensorType switch
            {
                SensorType.Temperature => baseName.Contains("Package") ? "封装温度" : 
                                         baseName.Contains("Core") ? baseName.Replace("Core", "核心") :
                                         baseName.Contains("GPU") ? "GPU温度" : baseName,
                SensorType.Load => baseName.Contains("Total") ? "总使用率" :
                                  baseName.Contains("Core") ? baseName.Replace("Core", "核心") : baseName,
                SensorType.Power => baseName.Contains("Package") ? "封装功耗" :
                                   baseName.Contains("Cores") ? "核心功耗" :
                                   baseName.Contains("GPU") ? "GPU功耗" : baseName,
                SensorType.Clock => baseName.Contains("Core") ? baseName.Replace("Core", "核心") : baseName,
                SensorType.Data => baseName.Contains("Used") ? "已使用" :
                                  baseName.Contains("Available") ? "可用" : baseName,
                _ => baseName
            };
        }

        private int GetHardwareTypePriority(HardwareType type)
        {
            return type switch
            {
                HardwareType.Cpu => 1,
                HardwareType.GpuNvidia => 2,
                HardwareType.GpuAmd => 3,
                HardwareType.GpuIntel => 4,
                HardwareType.Memory => 5,
                HardwareType.Motherboard => 6,
                HardwareType.Storage => 7,
                HardwareType.Network => 8,
                HardwareType.Battery => 9,
                _ => 99
            };
        }

        private int GetSensorPriority(SensorType type)
        {
            return type switch
            {
                SensorType.Temperature => 1,
                SensorType.Load => 2,
                SensorType.Power => 3,
                SensorType.Clock => 4,
                SensorType.Voltage => 5,
                SensorType.Fan => 6,
                SensorType.Data => 7,
                _ => 99
            };
        }

        private string GetHardwareTypeName(HardwareType type)
        {
            return type switch
            {
                HardwareType.Cpu => "处理器 / CPU",
                HardwareType.GpuNvidia => "显卡 / NVIDIA GPU",
                HardwareType.GpuAmd => "显卡 / AMD GPU",
                HardwareType.GpuIntel => "显卡 / Intel GPU",
                HardwareType.Memory => "内存 / RAM",
                HardwareType.Motherboard => "主板 / Motherboard",
                HardwareType.Storage => "存储 / Storage",
                HardwareType.Network => "网络 / Network",
                HardwareType.Battery => "电池 / Battery",
                _ => type.ToString()
            };
        }

        private string GetHardwareIcon(HardwareType type)
        {
            return type switch
            {
                HardwareType.Cpu => "\uE950",        // System/Processor
                HardwareType.GpuNvidia => "\uE7FC", // Game
                HardwareType.GpuAmd => "\uE7FC",
                HardwareType.GpuIntel => "\uE7FC",
                HardwareType.Memory => "\uE7B8",     // AllApps
                HardwareType.Motherboard => "\uE977", // ComputerLaptop
                HardwareType.Storage => "\uEDA2",    // HardDrive
                HardwareType.Network => "\uE968",    // NetworkTower
                HardwareType.Battery => "\uE996",    // Battery
                _ => "\uE8F1"                        // GenericScan
            };
        }
    }
}
