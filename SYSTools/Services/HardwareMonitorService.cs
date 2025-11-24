using System;
using System.Diagnostics;
using System.Linq;
using LibreHardwareMonitor.Hardware;

namespace SYSTools.Services
{
    /// <summary>
    /// 硬件监控服务 - 单例模式
    /// 提供统一的硬件数据访问接口
    /// </summary>
    public sealed class HardwareMonitorService : IDisposable
    {
        private static readonly Lazy<HardwareMonitorService> _instance = 
            new Lazy<HardwareMonitorService>(() => new HardwareMonitorService());
        
        public static HardwareMonitorService Instance => _instance.Value;
        
        private readonly Computer computer;
        private bool isInitialized = false;
        private bool isDisposed = false;
        
        // 硬件缓存
        private IHardware cpuHardware;
        private IHardware memoryHardware;
        private IHardware gpuHardware;
        
        private HardwareMonitorService()
        {
            computer = new Computer
            {
                IsCpuEnabled = true,
                IsMemoryEnabled = true,
                IsGpuEnabled = true,
                IsStorageEnabled = true,
                IsMotherboardEnabled = true,
                IsNetworkEnabled = true,
                IsBatteryEnabled = true
            };
        }
        
        /// <summary>
        /// 初始化硬件监控
        /// </summary>
        public void Initialize()
        {
            if (isInitialized) return;
            
            try
            {
                computer.Open();
                Update();
                
                // 缓存常用硬件引用
                cpuHardware = computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);
                
                // 获取物理内存硬件，排除虚拟内存
                memoryHardware = computer.Hardware.FirstOrDefault(h => 
                    h.HardwareType == HardwareType.Memory && 
                    !h.Name.Contains("Virtual"));
                
                // 如果没有找到，则使用任意 Memory 硬件（向后兼容）
                if (memoryHardware == null)
                {
                    memoryHardware = computer.Hardware.FirstOrDefault(h => 
                        h.HardwareType == HardwareType.Memory);
                }
                
                gpuHardware = computer.Hardware.FirstOrDefault(h => 
                    h.HardwareType == HardwareType.GpuNvidia || 
                    h.HardwareType == HardwareType.GpuAmd || 
                    h.HardwareType == HardwareType.GpuIntel);
                
                isInitialized = true;

                // 详细的初始化调试信息
                //Debug.WriteLine("=== HardwareMonitorService Initialized ===");
                //Debug.WriteLine($"CPU: {(cpuHardware != null ? cpuHardware.Name : "Not found")}");
                //Debug.WriteLine($"Memory: {(memoryHardware != null ? memoryHardware.Name : "Not found")}");
                //Debug.WriteLine($"GPU: {(gpuHardware != null ? $"{gpuHardware.Name} ({gpuHardware.HardwareType})" : "Not found")}");

                // 列出所有内存硬件用于调试
                //Debug.WriteLine("All Memory Hardware:");
                //foreach (var hw in computer.Hardware.Where(h => h.HardwareType == HardwareType.Memory))
                //{
                //    Debug.WriteLine($"  - {hw.Name}");
                //}
                //Debug.WriteLine("=========================================");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing HardwareMonitorService: {ex}");
            }
        }
        
        /// <summary>
        /// 更新所有硬件传感器数据
        /// </summary>
        public void Update()
        {
            if (!isInitialized) return;
            
            try
            {
                computer.Accept(new UpdateVisitor());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating hardware sensors: {ex}");
            }
        }
        
        /// <summary>
        /// 获取 Computer 实例（用于高级访问）
        /// </summary>
        public Computer GetComputer() => computer;
        
        #region CPU 相关
        
        /// <summary>
        /// 获取 CPU 总使用率
        /// </summary>
        public float GetCpuUsage()
        {
            try
            {
                if (cpuHardware == null) return 0;
                
                var loadSensor = cpuHardware.Sensors
                    .FirstOrDefault(s => s.SensorType == SensorType.Load && 
                                        (s.Name.Contains("Total") || s.Name.Contains("CPU Total")));
                
                return loadSensor?.Value ?? 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting CPU usage: {ex}");
                return 0;
            }
        }
        
        /// <summary>
        /// 获取 CPU 温度
        /// </summary>
        public float GetCpuTemperature()
        {
            try
            {
                if (cpuHardware == null) return 0;
                
                var tempSensor = cpuHardware.Sensors
                    .FirstOrDefault(s => s.SensorType == SensorType.Temperature && 
                                        (s.Name.Contains("Package") || s.Name.Contains("Core Average")));
                
                return tempSensor?.Value ?? 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting CPU temperature: {ex}");
                return 0;
            }
        }
        
        /// <summary>
        /// 获取 CPU 功耗
        /// </summary>
        public float GetCpuPower()
        {
            try
            {
                if (cpuHardware == null) return 0;
                
                var powerSensor = cpuHardware.Sensors
                    .FirstOrDefault(s => s.SensorType == SensorType.Power && 
                                        s.Name.Contains("Package"));
                
                return powerSensor?.Value ?? 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting CPU power: {ex}");
                return 0;
            }
        }
        
        #endregion
        
        #region 内存相关
        
        /// <summary>
        /// 获取内存使用率（基于物理内存，而非虚拟内存）
        /// </summary>
        public float GetMemoryUsage()
        {
            try
            {
                if (memoryHardware == null) return 0;
                
                // 获取所有 Load 类型的内存传感器用于调试
                var allLoadSensors = memoryHardware.Sensors
                    .Where(s => s.SensorType == SensorType.Load)
                    .ToList();
                
                // 调试输出：打印所有可用的内存负载传感器
                //Debug.WriteLine($"Memory Load Sensors ({memoryHardware.Name}):");
                //foreach (var sensor in allLoadSensors)
                //{
                //    Debug.WriteLine($"  - {sensor.Name}: {sensor.Value}%");
                //}
                
                // 按优先级查找传感器
                // 1. 明确查找 "Memory" 且不包含 "Virtual"
                var loadSensor = allLoadSensors.FirstOrDefault(s => 
                    s.Name.Equals("Memory", StringComparison.OrdinalIgnoreCase) ||
                    (s.Name.Contains("Memory") && !s.Name.Contains("Virtual")));
                
                // 2. 如果没找到，尝试查找包含 "Used" 但不包含 "Virtual" 的
                if (loadSensor == null)
                {
                    loadSensor = allLoadSensors.FirstOrDefault(s => 
                        s.Name.Contains("Used") && !s.Name.Contains("Virtual"));
                }
                
                // 3. 如果还是没找到，返回第一个不包含 "Virtual" 的传感器
                if (loadSensor == null)
                {
                    loadSensor = allLoadSensors.FirstOrDefault(s => 
                        !s.Name.Contains("Virtual"));
                }
                
                var result = loadSensor?.Value ?? 0;
                //Debug.WriteLine($"Selected Memory Sensor: {loadSensor?.Name ?? "None"}, Value: {result}%");
                
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting memory usage: {ex}");
                return 0;
            }
        }
        
        /// <summary>
        /// 获取已使用内存大小（GB）
        /// </summary>
        public float GetMemoryUsed()
        {
            try
            {
                if (memoryHardware == null) return 0;
                
                var usedSensor = memoryHardware.Sensors
                    .FirstOrDefault(s => s.SensorType == SensorType.Data && s.Name.Contains("Used"));
                
                return usedSensor?.Value ?? 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting memory used: {ex}");
                return 0;
            }
        }
        
        /// <summary>
        /// 获取可用内存大小（GB）
        /// </summary>
        public float GetMemoryAvailable()
        {
            try
            {
                if (memoryHardware == null) return 0;
                
                var availableSensor = memoryHardware.Sensors
                    .FirstOrDefault(s => s.SensorType == SensorType.Data && s.Name.Contains("Available"));
                
                return availableSensor?.Value ?? 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting memory available: {ex}");
                return 0;
            }
        }
        
        #endregion
        
        #region GPU 相关
        
        /// <summary>
        /// 检查是否有 GPU
        /// </summary>
        public bool HasGpu() => gpuHardware != null;
        
        /// <summary>
        /// 获取 GPU 信息（调试用）
        /// </summary>
        public string GetGpuInfo()
        {
            if (gpuHardware == null) return "No GPU detected";
            
            var info = $"GPU: {gpuHardware.Name} ({gpuHardware.HardwareType})\n";
            info += "Sensors:\n";
            
            foreach (var sensor in gpuHardware.Sensors.OrderBy(s => s.SensorType))
            {
                if (sensor.Value.HasValue)
                {
                    info += $"  [{sensor.SensorType}] {sensor.Name}: {sensor.Value}\n";
                }
            }
            
            return info;
        }
        
        /// <summary>
        /// 获取 GPU 核心使用率
        /// </summary>
        public float GetGpuUsage()
        {
            try
            {
                if (gpuHardware == null) return 0;
                
                // 获取所有 Load 类型的传感器
                var loadSensors = gpuHardware.Sensors
                    .Where(s => s.SensorType == SensorType.Load && s.Value.HasValue)
                    .ToList();
                
                if (loadSensors.Count == 0) return 0;
                
                // 调试输出：打印所有可用的负载传感器
                //Debug.WriteLine($"GPU Load Sensors ({gpuHardware.Name}):");
                // foreach (var sensor in loadSensors)
                // {
                //     Debug.WriteLine($"  - {sensor.Name}: {sensor.Value}%");
                // }
                
                // 按优先级查找传感器
                // 1. GPU Core / Core Load（最常见）
                var coreSensor = loadSensors.FirstOrDefault(s => 
                    s.Name.Contains("Core") || 
                    s.Name.Contains("GPU Core") ||
                    s.Name.Equals("GPU Core", StringComparison.OrdinalIgnoreCase));
                if (coreSensor != null) return coreSensor.Value ?? 0;
                
                // 2. D3D 3D（适用于某些 GPU）
                var d3dSensor = loadSensors.FirstOrDefault(s => 
                    s.Name.Contains("D3D") || 
                    s.Name.Contains("3D"));
                if (d3dSensor != null) return d3dSensor.Value ?? 0;
                
                // 3. GPU 使用率
                var gpuSensor = loadSensors.FirstOrDefault(s => 
                    s.Name.Contains("GPU") && !s.Name.Contains("Memory"));
                if (gpuSensor != null) return gpuSensor.Value ?? 0;
                
                // 4. 如果都没找到，返回第一个非零的负载传感器
                var nonZeroSensor = loadSensors.FirstOrDefault(s => s.Value > 0);
                if (nonZeroSensor != null) return nonZeroSensor.Value ?? 0;
                
                // 5. 最后返回第一个传感器（可能为0）
                return loadSensors.First().Value ?? 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting GPU usage: {ex}");
                return 0;
            }
        }
        
        /// <summary>
        /// 获取 GPU 温度
        /// </summary>
        public float GetGpuTemperature()
        {
            try
            {
                if (gpuHardware == null) return 0;
                
                var tempSensor = gpuHardware.Sensors
                    .FirstOrDefault(s => s.SensorType == SensorType.Temperature && 
                                        s.Name.Contains("GPU Core"));
                
                return tempSensor?.Value ?? 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting GPU temperature: {ex}");
                return 0;
            }
        }
        
        /// <summary>
        /// 获取 GPU 显存使用率
        /// </summary>
        public float GetGpuMemoryUsage()
        {
            try
            {
                if (gpuHardware == null) return 0;
                
                var loadSensor = gpuHardware.Sensors
                    .FirstOrDefault(s => s.SensorType == SensorType.Load && 
                                        s.Name.Contains("Memory"));
                
                return loadSensor?.Value ?? 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting GPU memory usage: {ex}");
                return 0;
            }
        }
        
        #endregion
        
        #region IDisposable
        
        public void Dispose()
        {
            if (isDisposed) return;
            
            try
            {
                computer?.Close();
                isDisposed = true;
                Debug.WriteLine("HardwareMonitorService disposed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error disposing HardwareMonitorService: {ex}");
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// UpdateVisitor 用于更新硬件传感器数据
    /// </summary>
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

