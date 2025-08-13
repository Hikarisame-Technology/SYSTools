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
    /// UncategorizedTools.xaml 的交互逻辑
    /// </summary>
    public partial class UncategorizedTools : Page
    {
        private readonly ExeHelper _exeHelper = new ExeHelper();
        string AppPath = Directory.GetCurrentDirectory();
        string Tools_Path = @"Software Package\UncategorizedTools\";        

        public UncategorizedTools()
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

        public void TextBlock_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DirExist(Path.Combine(AppPath, Tools_Path)))
            {
                Process.Start("explorer.exe", Path.Combine(AppPath, Tools_Path));
            }
        }

        private void SnappyDriverInstaller_Click(object sender, RoutedEventArgs e)
        {
            _exeHelper.HandleMouseClick(Tools_Path, "SnappyDriverInstaller", "SDI", "SDI_x64");
        }

        private void Rufus_Click(object sender, RoutedEventArgs e)
        {
            _exeHelper.HandleMouseClick(Tools_Path, "Rufus", "rufus");
        }

        private void Etcher_Click(object sender, RoutedEventArgs e)
        {
            _exeHelper.HandleMouseClick(Tools_Path, "balenaEtcher", "balenaEtcher");
        }

        private void Ventoy_Click(object sender, RoutedEventArgs e)
        {
            _exeHelper.HandleMouseClick(Tools_Path, "Ventoy", "Ventoy2Disk", "Ventoy2Disk_X64");
        }
    }
}
