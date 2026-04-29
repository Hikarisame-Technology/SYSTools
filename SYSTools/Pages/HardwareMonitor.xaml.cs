using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Media;
using LibreHardwareMonitor.Hardware;
using System.Linq;
using System.Diagnostics;

namespace SYSTools.Pages
{
    /// <summary>
    /// HardwareMonitor.xaml 的交互逻辑
    /// </summary>
    public partial class HardwareMonitor : Page
    {
        private readonly Computer computer;
        private readonly DispatcherTimer timer;
        private const int RefreshInterval = 2; // 刷新间隔（秒）
        private readonly Dictionary<string, Dictionary<string, TextBlock>> sensorTextBlocks = new Dictionary<string, Dictionary<string, TextBlock>>();
        private bool isInitialized = false;
        private bool _isLanguageSubscribed = false;

        public HardwareMonitor()
        {
            InitializeComponent();
            
            // 初始化硬件监控
            computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = true,
                IsMotherboardEnabled = true,
                IsStorageEnabled = true,
                IsNetworkEnabled = true,
                IsBatteryEnabled = true
            };
            
            try
            {
                computer.Open();
                computer.Accept(new UpdateVisitor());
                Debug.WriteLine("Hardware monitoring initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing hardware monitoring: {ex}");
                var errorMessage = Properties.Lang.ResourceManager.GetString("HardwareMonitorInitError", System.Globalization.CultureInfo.CurrentUICulture);
                var errorTitle = Properties.Lang.ResourceManager.GetString("ErrorTitle", System.Globalization.CultureInfo.CurrentUICulture);
                
                iNKORE.UI.WPF.Modern.Controls.MessageBox.Show(
                    string.Format(errorMessage ?? "Hardware monitor initialization error: {0}", ex.Message),
                    errorTitle ?? "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

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
            // 订阅语言变化事件（仅在首次订阅）
            if (!_isLanguageSubscribed)
            {
                SYSTools.Helpers.LocalizationManager.Instance.PropertyChanged += OnLanguageChanged;
                _isLanguageSubscribed = true;
            }
            StartMonitoring();
        }

        private void HardwareMonitor_Unloaded(object sender, RoutedEventArgs e)
        {
            StopMonitoring();
        }

        private void OnLanguageChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // 语言切换后重建 UI 以更新传感器名称
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (isInitialized)
                {
                    BuildUI();
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
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

            computer.Accept(new UpdateVisitor());

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

            // 网络设备：隐藏所有吞吐量为0的闲置设备
            if (hardware.HardwareType == HardwareType.Network && IsNetworkDeviceIdle(hardware))
            {
                return null;
            }

            var panel = new StackPanel
            {
                Margin = new Thickness(0, 0, 0, 12)
            };

            // 硬件名称
            var nameText = new TextBlock
            {
                Text = GetLocalizedHardwareName(hardware),
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
            computer.Accept(new UpdateVisitor());

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
                SensorType.Throughput => FormatThroughput(sensor.Value.Value),
                SensorType.TimeSpan => $"{TimeSpan.FromSeconds((double)sensor.Value):hh\\:mm\\:ss}",
                SensorType.Energy => $"{sensor.Value:F2} Wh",
                SensorType.Factor => $"{sensor.Value:F2}",
                _ => $"{sensor.Value:F1}"
            };
        }

        /// <summary>
        /// 格式化吞吐量（自动换算 B/s、KB/s、MB/s、GB/s）
        /// </summary>
        private static string FormatThroughput(float value)
        {
            if (value >= 1024 * 1024 * 1024)
                return $"{value / (1024 * 1024 * 1024):F2} GB/s";
            if (value >= 1024 * 1024)
                return $"{value / (1024 * 1024):F1} MB/s";
            if (value >= 1024)
                return $"{value / 1024:F1} KB/s";
            return $"{value:F1} B/s";
        }

        private static readonly (SensorType Type, string Pattern, string ResKey)[] SensorNameMaps =
        {
            // 温度 / Temperature
            (SensorType.Temperature, "package", "Sensor_Package_Temp"),
            (SensorType.Temperature, "core max", "Sensor_Core_Temp"),
            (SensorType.Temperature, "core", "Sensor_Core_Temp"),
            (SensorType.Temperature, "hotspot", "Sensor_Hotspot_Temp"),
            (SensorType.Temperature, "gpu memory", "Sensor_Memory_Temp"),
            (SensorType.Temperature, "gpu", "Sensor_GPU_Temp"),
            (SensorType.Temperature, "memory", "Sensor_Memory_Temp"),
            (SensorType.Temperature, "temp warning", "Sensor_Temp_Warning"),
            (SensorType.Temperature, "temperature warning", "Sensor_Temp_Warning"),
            (SensorType.Temperature, "temp critical", "Sensor_Temp_Critical"),
            (SensorType.Temperature, "temperature critical", "Sensor_Temp_Critical"),
            (SensorType.Temperature, "temp", "Sensor_Temperature"),
            // 负载 / Load
            (SensorType.Load, "cpu memory", "Sensor_CPU_Memory"),
            (SensorType.Load, "cpu platform", "Sensor_CPU_Platform"),
            (SensorType.Load, "network utilization", "Sensor_Network_Utilization"),
            (SensorType.Load, "used space", "Sensor_Used_Space"),
            (SensorType.Load, "read activity", "Sensor_Read_Activity"),
            (SensorType.Load, "write activity", "Sensor_Write_Activity"),
            (SensorType.Load, "total", "Sensor_Total_Load"),
            (SensorType.Load, "d3d 3d", "Sensor_D3D_3D"),
            (SensorType.Load, "d3d overlay", "Sensor_D3D_Overlay"),
            (SensorType.Load, "d3d video decode", "Sensor_D3D_VideoDecode"),
            (SensorType.Load, "d3d copy", "Sensor_D3D_Copy"),
            (SensorType.Load, "d3d security", "Sensor_D3D_Security"),
            (SensorType.Load, "d3d video encode", "Sensor_D3D_VideoEncode"),
            (SensorType.Load, "d3d nvenc", "Sensor_D3D_NVEnc"),
            (SensorType.Load, "d3d vr", "Sensor_D3D_VR"),
            (SensorType.Load, "core", "Sensor_Core_Load"),
            (SensorType.Load, "gpu", "Sensor_GPU_Load"),
            (SensorType.Load, "memory", "Sensor_Memory_Load"),
            (SensorType.Load, "ram", "Sensor_Memory_Load"),
            // 功耗 / Power
            (SensorType.Power, "package", "Sensor_Package_Power"),
            (SensorType.Power, "core", "Sensor_Core_Power"),
            (SensorType.Power, "gpu", "Sensor_GPU_Power"),
            (SensorType.Power, "cpu memory", "Sensor_CPU_Memory"),
            (SensorType.Power, "cpu platform", "Sensor_CPU_Platform"),
            // 频率 / Clock
            (SensorType.Clock, "core", "Sensor_Core_Clock"),
            (SensorType.Clock, "memory", "Sensor_Memory_Clock"),
            (SensorType.Clock, "mem", "Sensor_Memory_Clock"),
            (SensorType.Clock, "gpu", "Sensor_GPU_Clock"),
            // 电压 / Voltage
            (SensorType.Voltage, "vcore", "Sensor_VCore"),
            (SensorType.Voltage, "vdd", "Sensor_VCore"),
            // 风扇 / Fan
            (SensorType.Fan, "gpu", "Sensor_GPU_Fan"),
            (SensorType.Fan, "cpu", "Sensor_CPU_Fan"),
            (SensorType.Fan, "pump", "Sensor_Pump_Fan"),
            (SensorType.Fan, "water", "Sensor_Pump_Fan"),
            (SensorType.Fan, "aio", "Sensor_Pump_Fan"),
            (SensorType.Control, "gpu fan", "Sensor_GPU_Fan"),
            // 吞吐量 / Throughput
            (SensorType.Throughput, "pcie rx", "Sensor_PCIe_Rx"),
            (SensorType.Throughput, "pcie tx", "Sensor_PCIe_Tx"),
            (SensorType.Throughput, "download", "Sensor_Download"),
            (SensorType.Throughput, "upload", "Sensor_Upload"),
            (SensorType.Throughput, "read rate", "Sensor_Read_Rate"),
            (SensorType.Throughput, "write rate", "Sensor_Write_Rate"),
            // 数据 / Data (兼容 Data 和 SmallData)
            (SensorType.Data, "cpu memory", "Sensor_CPU_Memory"),
            (SensorType.Data, "cpu platform", "Sensor_CPU_Platform"),
            (SensorType.Data, "virtual memory", "Sensor_Virtual_Memory"),
            (SensorType.Data, "total memory", "Sensor_Total_Memory"),
            (SensorType.Data, "data uploaded", "Sensor_Data_Uploaded"),
            (SensorType.Data, "data downloaded", "Sensor_Data_Downloaded"),
            (SensorType.Data, "used space", "Sensor_Used_Space"),
            (SensorType.Data, "available spare threshold", "Sensor_Available_Spare_Threshold"),
            (SensorType.Data, "available space threshold", "Sensor_Available_Spare_Threshold"),
            (SensorType.Data, "available spare", "Sensor_Available_Spare"),
            (SensorType.Data, "available space", "Sensor_Available_Spare"),
            (SensorType.Data, "percentage used", "Sensor_Percentage_Used"),
            (SensorType.Data, "read activity", "Sensor_Read_Activity"),
            (SensorType.Data, "write activity", "Sensor_Write_Activity"),
            (SensorType.Data, "read rate", "Sensor_Read_Rate"),
            (SensorType.Data, "write rate", "Sensor_Write_Rate"),
            (SensorType.Data, "data read", "Sensor_Data_Read"),
            (SensorType.Data, "data written", "Sensor_Data_Written"),
            (SensorType.Data, "data write", "Sensor_Data_Written"),
            (SensorType.Data, "power on count", "Sensor_PowerOn_Count"),
            (SensorType.Data, "power-on count", "Sensor_PowerOn_Count"),
            (SensorType.Data, "power on hours", "Sensor_PowerOn_Hours"),
            (SensorType.Data, "power-on hours", "Sensor_PowerOn_Hours"),
            (SensorType.Data, "temp warning", "Sensor_Temp_Warning"),
            (SensorType.Data, "temperature warning", "Sensor_Temp_Warning"),
            (SensorType.Data, "temp critical", "Sensor_Temp_Critical"),
            (SensorType.Data, "temperature critical", "Sensor_Temp_Critical"),
            (SensorType.Data, "life", "Sensor_Life"),
            (SensorType.Data, "remaining", "Sensor_Life"),
            (SensorType.Data, "used", "Sensor_Used"),
            (SensorType.Data, "available", "Sensor_Available"),
            (SensorType.Data, "free", "Sensor_Available"),
            (SensorType.Data, "total", "Sensor_Total"),
            // Level (如 SSD 寿命、备用空间等)
            (SensorType.Level, "life", "Sensor_Life"),
            (SensorType.Level, "remaining", "Sensor_Life"),
            (SensorType.Level, "available spare threshold", "Sensor_Available_Spare_Threshold"),
            (SensorType.Level, "available spare", "Sensor_Available_Spare"),
            (SensorType.Level, "percentage used", "Sensor_Percentage_Used"),
            // Factor (如 SMART 属性)
            (SensorType.Factor, "power on count", "Sensor_PowerOn_Count"),
            (SensorType.Factor, "power-on count", "Sensor_PowerOn_Count"),
            (SensorType.Factor, "power on hours", "Sensor_PowerOn_Hours"),
            (SensorType.Factor, "power-on hours", "Sensor_PowerOn_Hours"),
            (SensorType.Factor, "temp warning", "Sensor_Temp_Warning"),
            (SensorType.Factor, "temperature warning", "Sensor_Temp_Warning"),
            (SensorType.Factor, "temp critical", "Sensor_Temp_Critical"),
            (SensorType.Factor, "temperature critical", "Sensor_Temp_Critical"),
            // SmallData 回退到 Data 的通用条目
            (SensorType.SmallData, "cpu memory", "Sensor_CPU_Memory"),
            (SensorType.SmallData, "cpu platform", "Sensor_CPU_Platform"),
            (SensorType.SmallData, "used space", "Sensor_Used_Space"),
            (SensorType.SmallData, "read activity", "Sensor_Read_Activity"),
            (SensorType.SmallData, "write activity", "Sensor_Write_Activity"),
            (SensorType.SmallData, "read rate", "Sensor_Read_Rate"),
            (SensorType.SmallData, "write rate", "Sensor_Write_Rate"),
            (SensorType.SmallData, "data read", "Sensor_Data_Read"),
            (SensorType.SmallData, "data written", "Sensor_Data_Written"),
            (SensorType.SmallData, "data write", "Sensor_Data_Written"),
            (SensorType.SmallData, "power on count", "Sensor_PowerOn_Count"),
            (SensorType.SmallData, "power-on count", "Sensor_PowerOn_Count"),
            (SensorType.SmallData, "power on hours", "Sensor_PowerOn_Hours"),
            (SensorType.SmallData, "power-on hours", "Sensor_PowerOn_Hours"),
            (SensorType.SmallData, "life", "Sensor_Life"),
            (SensorType.SmallData, "remaining", "Sensor_Life"),
            (SensorType.SmallData, "temp warning", "Sensor_Temp_Warning"),
            (SensorType.SmallData, "temperature warning", "Sensor_Temp_Warning"),
            (SensorType.SmallData, "temp critical", "Sensor_Temp_Critical"),
            (SensorType.SmallData, "temperature critical", "Sensor_Temp_Critical"),
            (SensorType.SmallData, "available spare threshold", "Sensor_Available_Spare_Threshold"),
            (SensorType.SmallData, "available spare", "Sensor_Available_Spare"),
            (SensorType.SmallData, "percentage used", "Sensor_Percentage_Used"),
            (SensorType.SmallData, "used", "Sensor_Used"),
            (SensorType.SmallData, "available", "Sensor_Available"),
            (SensorType.SmallData, "free", "Sensor_Available"),
            (SensorType.SmallData, "total", "Sensor_Total"),
        };

        private static readonly (string Pattern, string ResKey)[] HardwareNameMaps =
        {
            ("cpu memory", "Sensor_CPU_Memory"),
            ("cpu platform", "Sensor_CPU_Platform"),
            ("virtual memory", "Sensor_Virtual_Memory"),
            ("total memory", "Sensor_Total_Memory"),
        };

        private static string LookupResource(ISensor sensor, (SensorType Type, string Pattern, string ResKey)[] maps)
        {
            string n = sensor.Name.ToLowerInvariant();
            foreach (var (type, pattern, resKey) in maps)
            {
                if (type == sensor.SensorType && n.Contains(pattern))
                {
                    string localized = Properties.Lang.ResourceManager.GetString(resKey,
                        System.Globalization.CultureInfo.CurrentUICulture);
                    if (!string.IsNullOrEmpty(localized))
                        return localized;
                }
            }
            return null;
        }

        private string GetSensorDisplayName(ISensor sensor)
        {
            string localized = LookupResource(sensor, SensorNameMaps);
            if (localized != null)
                return localized;

            // 调试：记录未匹配的传感器名称和类型
            System.Diagnostics.Debug.WriteLine($"[LHM] 未匹配: Type={sensor.SensorType} Name='{sensor.Name}'");
            return sensor.Name;
        }

        private static string GetLocalizedHardwareName(IHardware hardware)
        {
            string n = hardware.Name.ToLowerInvariant();
            foreach (var (pattern, resKey) in HardwareNameMaps)
            {
                if (n.Contains(pattern))
                {
                    string localized = Properties.Lang.ResourceManager.GetString(resKey,
                        System.Globalization.CultureInfo.CurrentUICulture);
                    if (!string.IsNullOrEmpty(localized))
                        return localized;
                }
            }
            return hardware.Name;
        }

        private string GetHardwareTypeName(HardwareType type)
        {
            string key = $"HardwareType_{type}";
            string localized = Properties.Lang.ResourceManager.GetString(key,
                System.Globalization.CultureInfo.CurrentUICulture);
            return !string.IsNullOrEmpty(localized) ? localized : type.ToString();
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
                SensorType.Throughput => 7,
                SensorType.Data => 8,
                _ => 99
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

        /// <summary>
        /// 判断网络设备是否处于闲置状态（所有吞吐量传感器都是 0）
        /// </summary>
        private static bool IsNetworkDeviceIdle(IHardware hardware)
        {
            var throughputSensors = hardware.Sensors.Where(s => s.SensorType == SensorType.Throughput).ToList();
            if (throughputSensors.Count == 0)
                return false; // 没有吞吐量传感器，按原有逻辑显示

            // 如果所有吞吐量传感器都是 0 或无效，视为闲置
            return throughputSensors.All(s => !s.Value.HasValue || s.Value.Value == 0f);
        }
    }

    public class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer)
        {
            computer.Traverse(this);
        }

        public void VisitHardware(IHardware hardware)
        {
            try
            {
                hardware.Update();
                foreach (IHardware subHardware in hardware.SubHardware)
                {
                    subHardware.Accept(this);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating hardware {hardware.Name}: {ex}");
            }
        }

        public void VisitSensor(ISensor sensor) { }

        public void VisitParameter(IParameter parameter) { }
    }
}
