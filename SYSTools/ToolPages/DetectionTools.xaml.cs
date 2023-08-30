using System.Diagnostics;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SYSTools.Dialog;

namespace SYSTools.ToolPages
{
    /// <summary>
    /// DetectionTools.xaml 的交互逻辑
    /// </summary>
    public partial class DetectionTools : Page
    {
        string AppPath = Directory.GetCurrentDirectory();
        string DetectionTools_Path = @"Software Package\DetectionTools\";
        ProgramFailed ProgramFailed_Dialog = new ProgramFailed();

        public DetectionTools()
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
            if (!DirExist(Path.Combine(AppPath, DetectionTools_Path)))
            {
                Directory.CreateDirectory(Path.Combine(AppPath, DetectionTools_Path));
            }
        }

        public void TextBlock_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DirExist(Path.Combine(AppPath, DetectionTools_Path)))
            {
                Process.Start("explorer.exe", Path.Combine(AppPath, DetectionTools_Path));
            }
        }
        public void HandleMouseClick(string ToolName, string ExeName)
        {
            string ExePath = Path.Combine(AppPath, DetectionTools_Path, ToolName, ExeName + ".exe");
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
        private void Aida64_Click(object sender, RoutedEventArgs e)
        {
            HandleMouseClick("Aida64", "Aida64");
        }

        private void CPUZ_Click(object sender, RoutedEventArgs e)
        {
            HandleMouseClick("CPUZ", "CPUZ");
        }

        private void GPUZ_Click(object sender, RoutedEventArgs e)
        {
            HandleMouseClick("GPUZ", "GPUZ");
        }

        private void HWinfo_Click(object sender, RoutedEventArgs e)
        {
            HandleMouseClick("HWinfo", "HWinfo");
        }

        private void HWmonitor_Click(object sender, RoutedEventArgs e)
        {
            HandleMouseClick("HWmonitor", "HWmonitor");
        }

    }
}
