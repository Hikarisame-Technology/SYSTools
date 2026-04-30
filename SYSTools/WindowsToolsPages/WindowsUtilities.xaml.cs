using iNKORE.UI.WPF.Modern.Controls;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using SYSTools.Helpers;
using SYSTools.Properties;

namespace SYSTools.WindowsToolsPages
{
    /// <summary>
    /// WindowsToolsPages.xaml 的交互逻辑
    /// </summary>
    public partial class WindowsUtilities : System.Windows.Controls.Page
    {
        private bool isInitializing = true;

        // 简化的本地化辅助方法
        private static string T(string key, string fallback = "")
        {
            return Lang.ResourceManager.GetString(key,
                System.Globalization.CultureInfo.CurrentUICulture) ?? fallback;
        }

        public WindowsUtilities()
        {
            InitializeComponent();
            InitializeSettings();
        }

        private void InitializeSettings()
        {
            // 初始化文件夹选项状态
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"))
            {
                if (key != null)
                {
                    ShowFileExt.IsChecked = (int)key.GetValue("HideFileExt", 1) == 0;
                    ShowHidden.IsChecked = (int)key.GetValue("Hidden", 0) == 1;
                    ShowSuper.IsChecked = (int)key.GetValue("ShowSuperHidden", 0) == 1;
                    HideArrow.IsChecked = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Icons")?.GetValue("29") != null;
                    byte[] linkValue = (byte[])Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer")?.GetValue("link");
                    HideText.IsChecked = linkValue != null && linkValue.Length == 4 &&
                        linkValue[0] == 0x00 && linkValue[1] == 0x00 &&
                        linkValue[2] == 0x00 && linkValue[3] == 0x00;
                    HideUAC.IsChecked = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Icons")?.GetValue("77") != null;
                }
            }

            // 初始化右键菜单风格
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32"))
            {
                MenuStyle.SelectedIndex = (key == null) ? 1 : 0;
            }

            // 初始化资源管理器默认打开位置
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"))
            {
                if (key != null)
                {
                    // LaunchTo=2 时选择"快速访问"，LaunchTo=1 时选择"此电脑"
                    ExplorerDefault.SelectedIndex = (int)key.GetValue("LaunchTo", 1) == 2 ? 1 : 0;
                }
            }

            // 初始化电源计划选择
            InitializePowerConfig();

            isInitializing = false;
        }

        private void ShowFileExt_Toggled(object sender, RoutedEventArgs e)
        {
            if (isInitializing) return;

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", true))
            {
                if (key != null)
                {
                    key.SetValue("HideFileExt", ShowFileExt.IsChecked == true ? 0 : 1, RegistryValueKind.DWord);
                    ShowMessage("设置已更改，需要重启资源管理器生效");
                }
            }
        }

        private void ShowHidden_Toggled(object sender, RoutedEventArgs e)
        {
            if (isInitializing) return;

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", true))
            {
                if (key != null)
                {
                    key.SetValue("Hidden", ShowHidden.IsChecked == true ? 1 : 0, RegistryValueKind.DWord);
                    ShowMessage("设置已更改，需要重启资源管理器生效");
                }
            }
        }

        private void ShowSuper_Toggled(object sender, RoutedEventArgs e)
        {
            if (isInitializing) return;

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", true))
            {
                if (key != null)
                {
                    key.SetValue("ShowSuperHidden", ShowSuper.IsChecked == true ? 1 : 0, RegistryValueKind.DWord);
                    ShowMessage("设置已更改，需要重启资源管理器生效");
                }
            }
        }

        private void HideArrow_Toggled(object sender, RoutedEventArgs e)
        {
            if (isInitializing) return;

            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Icons", true)
                          ?? Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Icons"))
            {
                if (key != null)
                {
                    if (HideArrow.IsChecked == true)
                    {
                        key.SetValue("29", "%systemroot%\\system32\\imageres.dll,197", RegistryValueKind.String);
                    }
                    else
                    {
                        key.DeleteValue("29", false);
                    }
                    ShowMessage("设置已更改，需要重启资源管理器生效");
                }
            }
        }

        private void HideText_Toggled(object sender, RoutedEventArgs e)
        {
            if (isInitializing) return;

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer", true))
            {
                if (key != null)
                {
                    if (HideText.IsChecked == true)
                    {
                        key.SetValue("link", new byte[] { 0x00, 0x00, 0x00, 0x00 }, RegistryValueKind.Binary);
                    }
                    else
                    {
                        key.SetValue("link", new byte[] { 0x19, 0x00, 0x00, 0x00 }, RegistryValueKind.Binary);
                    }
                    ShowMessage("设置已更改，需要重启资源管理器生效");
                }
            }
        }

        private void HideUAC_Toggled(object sender, RoutedEventArgs e)
        {
            if (isInitializing) return;

            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Icons", true)
                          ?? Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Icons"))
            {
                if (key != null)
                {
                    if (HideUAC.IsChecked == true)
                    {
                        key.SetValue("77", "%systemroot%\\system32\\imageres.dll,197", RegistryValueKind.String);
                    }
                    else
                    {
                        key.DeleteValue("77", false);
                    }
                    ShowMessage("设置已更改，需要重启资源管理器生效");
                }
            }
        }

        private void MenuStyle_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isInitializing) return;

            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32", true))
            {
                if (key != null)
                {
                    if (MenuStyle.SelectedIndex == 0) // Win10风格
                    {
                        key.SetValue("", "", RegistryValueKind.String);
                    }
                    else // Win11风格
                    {
                        Registry.CurrentUser.DeleteSubKey(@"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32", false);
                    }
                    ShowMessage("设置已更改，需要重启资源管理器生效");
                }
            }
        }

        private void ExplorerDefault_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isInitializing) return;

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", true))
            {
                if (key != null)
                {
                    // 选择"此电脑"(SelectedIndex=0)时设置LaunchTo=1，选择"快速访问"(SelectedIndex=1)时设置LaunchTo=2
                    key.SetValue("LaunchTo", ExplorerDefault.SelectedIndex == 0 ? 1 : 2, RegistryValueKind.DWord);
                    ShowMessage("设置已更改，需要重启资源管理器生效");
                }
            }
        }

        //快捷启动分类
        private void CMD_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("cmd") { WorkingDirectory = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile) });
        }

        private void PowerShell_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("powershell") { WorkingDirectory = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile) });
        }

        private void Regedit_Click(object sender, RoutedEventArgs e)
        {
            //注册表
            Process.Start("regedit");
        }

        private void Control_Click(object sender, RoutedEventArgs e)
        {
            //控制面板
            Process.Start("control");
        }

        private void compmgmt_Click(object sender, RoutedEventArgs e)
        {
            //计算机管理
            Process.Start("compmgmt.msc");
        }

        private void Eventvwr_Click(object sender, RoutedEventArgs e)
        {
            //事件查看器
            Process.Start("eventvwr");
        }

        private void Devmgmt_Click(object sender, RoutedEventArgs e)
        {
            //设备管理器
            Process.Start("devmgmt.msc");
        }

        private void Gpedit_Click(object sender, RoutedEventArgs e)
        {
            //组策略
            Process.Start("gpedit.msc");
        }
        private void Taskschd_Click(object sender, RoutedEventArgs e)
        {
            //计划任务
            Process.Start("taskschd.msc");
        }

        private void GodMode_Click(object sender, RoutedEventArgs e)
        {
            //上帝模式
            Process.Start("shell:::{ED7BA470-8E54-465E-825C-99712043E01C}");
        }

        private void Winver_Click(object sender, RoutedEventArgs e)
        {
            //关于Windows
            Process.Start("winver");
        }
        private void SystemProperties_Click(object sender, RoutedEventArgs e)
        {
            // 系统属性
            Process.Start("control.exe", "system");
        }
        private void PerfMon_Click(object sender, RoutedEventArgs e)
        {
            // 性能监视器
            Process.Start("perfmon.exe");
        }
        private void ResMon_Click(object sender, RoutedEventArgs e)
        {
            // 资源监视器
            Process.Start("resmon.exe");
        }
        private void TaskMgr_Click(object sender, RoutedEventArgs e)
        {
            // 任务管理器
            Process.Start("taskmgr.exe");
        }
        private void MSConfig_Click(object sender, RoutedEventArgs e)
        {
            // 系统配置
            Process.Start("msconfig.exe");
        }
        private void DisplaySettings_Click(object sender, RoutedEventArgs e)
        {
            // 显示设置
            Process.Start("desk.cpl");
        }

        private void Explorer_Restart_Click(object sender, RoutedEventArgs e)
        {
            // 重启资源管理器
            Process[] processes = Process.GetProcessesByName("explorer");
            foreach (Process process in processes)
            {
                process.Kill();
            }
            Process.Start("explorer.exe");
        }
        //网络工具分类
        private void ClearDNS_Click(object sender, RoutedEventArgs e)
        {
            string output = RunCommand("ipconfig /flushdns");
            ShowMessage(output);
        }

        private void ResetWS_Click(object sender, RoutedEventArgs e)
        {
            string output = RunCommand("netsh winsock reset");
            ShowMessage(output);
        }

        private void ResetWS_LSP_Click(object sender, RoutedEventArgs e)
        {
            string output = RunCommand("netsh winsock reset catalog");
            ShowMessage(output);
        }

        private void ResetTCP_Click(object sender, RoutedEventArgs e)
        {
            string output = RunCommand("netsh int ip reset");
            ShowMessage(output);
        }

        // 电源计划
        private void InitializePowerConfig()
        {
            string output = RunCommand("powercfg /list");
            string[] lines = output.Split('\n');
            string currentGuid = RunCommand("powercfg /getactivescheme").Split('\n')[0].Trim();
            
            // 检查是否已存在卓越性能模式
            bool hasUltimate = false;
            string ultimateGuid = "";
            
            foreach (string line in lines)
            {
                if (line.Contains("电源方案 GUID"))
                {
                    if (line.Contains("卓越性能"))
                    {
                        hasUltimate = true;
                        Match match = Regex.Match(line, @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");
                        if (match.Success)
                        {
                            ultimateGuid = match.Value;
                        }
                    }
                    
                    if (line.Contains(currentGuid))
                    {
                        if (line.Contains("(节能)") || line.Contains("最佳能效"))
                            PowerConfig.SelectedIndex = 0;
                        else if (line.Contains("(平衡)") || line.Contains("平衡"))
                            PowerConfig.SelectedIndex = 1;
                        else if (line.Contains("(高性能)") || line.Contains("最佳性能"))
                            PowerConfig.SelectedIndex = 2;
                        else if (line.Contains("卓越性能"))
                            PowerConfig.SelectedIndex = 3;
                    }
                }
            }

            // 如果不存在卓越性能模式，创建一个
            if (!hasUltimate)
            {
                string createOutput = RunCommand("powercfg -duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61");
                Match match = Regex.Match(createOutput, @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");
                if (match.Success)
                {
                    ultimateGuid = match.Value;
                    // 重命名为卓越性能
                    RunCommand($"powercfg -changename {ultimateGuid} 卓越性能");
                }
            }
        }

        private void PowerConfig_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isInitializing) return;

            string output = RunCommand("powercfg /list");
            string scheme = "";

            switch (PowerConfig.SelectedIndex)
            {
                case 0: // 最佳能效
                    scheme = "a1841308-3541-4fab-bc81-f71556f20b4a";
                    break;
                case 1: // 平衡
                    scheme = "381b4222-f694-41f0-9685-ff5bb260df2e";
                    break;
                case 2: // 最佳性能
                    scheme = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";
                    break;
                case 3: // 卓越性能
                    // 查找现有的卓越性能方案
                    string[] lines = output.Split('\n');
                    foreach (string line in lines)
                    {
                        if (line.Contains("卓越性能"))
                        {
                            Match match = Regex.Match(line, @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");
                            if (match.Success)
                            {
                                scheme = match.Value;
                                break;
                            }
                        }
                    }
                    
                    // 如果没有找到卓越性能方案，创建一个
                    if (string.IsNullOrEmpty(scheme))
                    {
                        string createOutput = RunCommand("powercfg -duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61");
                        Match match = Regex.Match(createOutput, @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");
                        if (match.Success)
                        {
                            scheme = match.Value;
                            // 重命名为卓越性能
                            RunCommand($"powercfg -changename {scheme} 卓越性能");
                        }
                    }
                    break;
            }

            if (!string.IsNullOrEmpty(scheme))
            {
                string setOutput = RunCommand($"powercfg /setactive {scheme}");
                ShowMessage(T("WinUtil_PowerSchemeChanged", "电源计划已更改: ") + setOutput);
            }
        }

        private void PowerSetting_Click(object sender, RoutedEventArgs e)
        {
            //控制面板 -> 电源设置
            Process.Start("control","powercfg.cpl");
        }

        private void PowerOptions_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "control.exe",
                    Arguments = "powercfg.cpl,,3",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                ShowMessage(T("WinUtil_PowerEditError", "无法打开电源计划编辑页面: ") + ex.Message);
            }
        }

        private void Advanced_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("SystemPropertiesAdvanced.exe");     // 高级系统设置
        }

        private void Performance_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("SystemPropertiesPerformance.exe");  // 性能选项
        }

        // cmd命令执行
        static string RunCommand(string command)
        {
            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c chcp 65001 > nul && {command}", // 切换到UTF-8代码页
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                    StandardErrorEncoding = System.Text.Encoding.UTF8
                }
            };
            
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            
            return string.IsNullOrEmpty(error) ? output.Trim() : error.Trim();
        }

        // 窗口提示
        private async void ShowMessage(string message)
        {
            await ContentDialogHelper.ShowTextContentAsync(
                T("WinUtil_DialogTitle", "命令执行结果"),
                message,
                null,
                T("WinUtil_Confirm", "确定")
            );
        }
    }
}

