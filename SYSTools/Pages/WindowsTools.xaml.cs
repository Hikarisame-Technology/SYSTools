using iNKORE.UI.WPF.Modern.Controls;
using System.Diagnostics;
using System.Windows;

namespace SYSTools.Pages
{
    /// <summary>
    /// WindowsTools.xaml 的交互逻辑
    /// </summary>
    public partial class WindowsTools : System.Windows.Controls.Page
    {
        public WindowsTools()
        {
            InitializeComponent();
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
            Process.Start("regedit");
        }

        private void Control_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("control");
        }

        private void Eventvwr_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("eventvwr");
        }

        private void Devmgmt_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("devmgmt.msc");
        }

        private void Gpedit_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("gpedit.msc");
        }

        private void GodMode_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("shell:::{ED7BA470-8E54-465E-825C-99712043E01C}");
        }

        private void Winver_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("winver");
        }
        //网络工具分类
        private void ClearDNS_Click(object sender, RoutedEventArgs e)
        {
            string command = "ipconfig /flushdns";
            string output = RunCommand(command);
            DisplayNoWifiDialog(output);
        }

        private void ResetWS_Click(object sender, RoutedEventArgs e)
        {
            string command = "netsh winsock reset";
            string output = RunCommand(command);
            DisplayNoWifiDialog(output);
        }

        private void ResetWS_LSP_Click(object sender, RoutedEventArgs e)
        {
            string command = "netsh winsock reset catalog";
            string output = RunCommand(command);
            DisplayNoWifiDialog(output);
        }

        private void ResetTCP_Click(object sender, RoutedEventArgs e)
        {
            string command = "netsh int ip reset";
            string output = RunCommand(command);
            DisplayNoWifiDialog(output);
        }

        static string RunCommand(string command)
        {
            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {command}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output;
        }
        private async void DisplayNoWifiDialog(string output)
        {
            ContentDialog noWifiDialog = new ContentDialog
            {
                Title = "运行完成",
                Content = output,
                CloseButtonText = "Ok"
            };

            ContentDialogResult result = await noWifiDialog.ShowAsync();
        }

    }
}
