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
    /// TestTools.xaml 的交互逻辑
    /// </summary>
    public partial class TestTools : Page
    {
        private readonly ExeHelper _exeHelper = new ExeHelper();
        string AppPath = Directory.GetCurrentDirectory();
        string Tools_Path = @"Software Package\TestTools\";

        public TestTools()
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

        private void Prime95_Click(object sender, RoutedEventArgs e)
        {
            _exeHelper.HandleMouseClick(Tools_Path,"Prime95", "p95.win32/prime95", "p95.win64/prime95");
        }

        private void IntelBurnTest_Click(object sender, RoutedEventArgs e)
        {
            _exeHelper.HandleMouseClick(Tools_Path,"IntelBurnTest", "IntelBurnTestV2");
        }

        private void FurMark_Click(object sender, RoutedEventArgs e)
        {
            _exeHelper.HandleMouseClick(Tools_Path,"FurMark", "FurMark_win32/FurMark_GUI", "FurMark_win64/FurMark_GUI");
        }

        private void MemTest_Click(object sender, RoutedEventArgs e)
        {
            _exeHelper.HandleMouseClick(Tools_Path,"MemTest", "MemTest");
        }
    }
}
