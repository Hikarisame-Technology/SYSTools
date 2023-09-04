using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SYSTools.Dialog;

namespace SYSTools.Pages
{
    /// <summary>
    /// RepairingTools.xaml 的交互逻辑
    /// </summary>
    public partial class RepairingTools : Page
    {
        string AppPath = Directory.GetCurrentDirectory();
        string RepairingTools_Path = @"Software Package\RepairingTools\";
        ProgramFailed ProgramFailed_Dialog = new ProgramFailed();

        public RepairingTools()
        {
            InitializeComponent();
        }

        public bool FileExist(string Str_File)
        {
            // 用于查找文件是否存在
            return File.Exists(Str_File);
        }

        public bool DirExist(string Str_Path)
        {
            // 用于查找文件夹是否存在
            return Directory.Exists(Str_Path);
        }

        public void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (!DirExist(Path.Combine(AppPath, RepairingTools_Path)))
            {
                Directory.CreateDirectory(Path.Combine(AppPath, RepairingTools_Path));
            }
        }

        public void TextBlock_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DirExist(Path.Combine(AppPath, RepairingTools_Path)))
            {
                Process.Start("explorer.exe", Path.Combine(AppPath, RepairingTools_Path));
            }
        }
        public void HandleMouseClick(string ToolName, string ExeName)
        {
            string ExePath = Path.Combine(AppPath, RepairingTools_Path, ToolName, ExeName + ".exe");
            if (FileExist(ExePath))
            {
                try
                {
                    Process.Start(ExePath);
                }
                catch (Exception e)
                {
                    // Handle the exception if needed
                    // MessageBox.Show(e.Message);
                }
            }
            else
            {
                ProgramFailed_Dialog.ShowAsync();
            }
        }

        private void DirectX_Click(object sender, RoutedEventArgs e)
        {
            HandleMouseClick("DirectX", "DirectXInstall");
        }

        private void VisualC_Click(object sender, RoutedEventArgs e)
        {
            HandleMouseClick("Visual-C-Runtimes", "VisualCInstall");
        }
    }
}
