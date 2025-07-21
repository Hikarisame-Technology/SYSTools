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
    /// DiskTools.xaml 的交互逻辑
    /// </summary>
    public partial class DiskTools : Page
    {
        private readonly ExeHelper _exeHelper = new ExeHelper();
        string AppPath = Directory.GetCurrentDirectory();
        string Tools_Path = @"Software Package\DiskTools\";

        public DiskTools()
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

        private void AS_SSD_Click(object sender, RoutedEventArgs e)
        {
            _exeHelper.HandleMouseClick(Tools_Path,"AS SSD Benchmark", "AS SSD Benchmark");
        }

        private void CrystalDiskInfo_Click(object sender, RoutedEventArgs e)
        {
            _exeHelper.HandleMouseClick(Tools_Path,"CrystalDiskInfo", "DiskInfo32S", "DiskInfo64S");
        }

        private void CrystalDiskMark_Click(object sender, RoutedEventArgs e)
        {
            _exeHelper.HandleMouseClick(Tools_Path,"CrystalDiskMark", "DiskMark32S", "DiskMark64S");
        }

        private void DiskBenchmark_Click(object sender, RoutedEventArgs e)
        {
            _exeHelper.HandleMouseClick(Tools_Path,"DiskBenchmark", "DiskBenchmark");
        }

        private void DiskGenius_Click(object sender, RoutedEventArgs e)
        {
            _exeHelper.HandleMouseClick(Tools_Path,"DiskGenius", "DiskGenius_x86/DiskGenius", "DiskGenius_x64/DiskGenius");
        }

        private void HDTune_Click(object sender, RoutedEventArgs e)
        {
            _exeHelper.HandleMouseClick(Tools_Path,"HDTune", "HDTune");
        }

        private void LLFTOOL_Click(object sender, RoutedEventArgs e)
        {
            _exeHelper.HandleMouseClick(Tools_Path,"LLFTOOL", "LLFTOOL");
        }

        private void PartAssist_Click(object sender, RoutedEventArgs e)
        {
            _exeHelper.HandleMouseClick(Tools_Path,"PartAssist", "PartAssist");
        }

        private void SSDZ_Click(object sender, RoutedEventArgs e)
        {
            _exeHelper.HandleMouseClick(Tools_Path,"SSDZ", "SSD-Z");
        }

        private void Victoria_Click(object sender, RoutedEventArgs e)
        {
            _exeHelper.HandleMouseClick(Tools_Path,"Victoria", "Victoria");
        }

        private void H2TestW_Click(object sender, RoutedEventArgs e)
        {
            _exeHelper.HandleMouseClick(Tools_Path,"H2TestW", "H2TestW");
        }

    }
}
