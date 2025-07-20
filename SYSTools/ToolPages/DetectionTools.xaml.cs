using iNKORE.UI.WPF.Modern.Common.IconKeys;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SYSTools.Helpers;

namespace SYSTools.ToolPages
{
    /// <summary>
    /// DetectionTools.xaml 的交互逻辑
    /// </summary>
    public partial class DetectionTools : Page
    {
        private readonly ExeHelper _exeHelper = new ExeHelper();
        string AppPath = Directory.GetCurrentDirectory();
        string Tools_Path = @"Software Package\DetectionTools\";

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
            if (!DirExist(Path.Combine(AppPath, Tools_Path)))
            {
                Directory.CreateDirectory(Path.Combine(AppPath, Tools_Path));
            }
        }

        // 顶部文版右键打开文件夹
        public void TextBlock_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DirExist(Path.Combine(AppPath, Tools_Path)))
            {
                Process.Start("explorer.exe", Path.Combine(AppPath, Tools_Path));
            }
        }

        private void Aida64_Click(object sender, RoutedEventArgs e)
        {
            _exeHelper.HandleMouseClick(Tools_Path,"Aida64", "Aida64");
        }

        private void CPUZ_Click(object sender, RoutedEventArgs e)
        {
            _exeHelper.HandleMouseClick(Tools_Path,"CPUZ", "CPUZ");
        }

        private void GPUZ_Click(object sender, RoutedEventArgs e)
        {
            _exeHelper.HandleMouseClick(Tools_Path,"GPUZ", "GPUZ");
        }

        private void HWinfo_Click(object sender, RoutedEventArgs e)
        {
            _exeHelper.HandleMouseClick(Tools_Path,"HWinfo", "HWinfo");
        }

        private void HWmonitor_Click(object sender, RoutedEventArgs e)
        {
            _exeHelper.HandleMouseClick(Tools_Path,"HWmonitor", "HWmonitor");
        }

    }
}
