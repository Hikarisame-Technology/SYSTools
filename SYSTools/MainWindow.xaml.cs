using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SYSTools.Pages;
using SYSTools.ToolPages;
using SYSTools.Dialog;
using System.IO;

namespace SYSTools
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    ///     

    public partial class MainWindow : Window
    {
        private Frame Home_Page = new Frame() { Content = new Home() };
        private Frame FastDetection = new Frame() { Content = new Test() };
        private Frame DetectionTools = new Frame() { Content = new DetectionTools() };
        private Frame TestTools = new Frame() { Content = new TestTools() };
        private Frame DiskTools = new Frame() { Content = new DiskTools() };
        private Frame PeripheralsTools = new Frame() { Content = new PeripheralsTools() };
        private Frame RepairingTools = new Frame() { Content = new RepairingTools() };
        private Frame AdbTools = new Frame() { Content = new AdbTools() };
        private Frame ChangeWindows = new Frame() { Content = new WindowsTools() };
        private Frame WindowsActivation = new Frame() { Content = new Activation() };
        private Frame AboutPage = new Frame() { Content = new About() };

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            LoadBackgroundImage();
        }

        private string FindFile(string directoryPath, string fileExtension)
        {
            string[] files = Directory.GetFiles(directoryPath, "*." + fileExtension);
            if (files.Length > 0)
            {
                return files[0]; // 返回第一个找到的文件
            }
            else
            {
                return null; // 没有找到文件
            }
        }

        private void LoadBackgroundImage()
        {
            //背景图片更换
            string appPath = Directory.GetCurrentDirectory();
            BitmapImage bitmap = new BitmapImage();
            string imagePath = FindFile(appPath + "\\Config\\BackImage", "jpg") ?? FindFile(appPath + "\\Config\\BackImage", "png");
            if (imagePath != null)
            {
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath);
                bitmap.EndInit();
                BackImage.Source = bitmap;
            }
        }

        private void About_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //鼠标右键双击事件
            if (e.ChangedButton == MouseButton.Right && e.ClickCount == 2)
            {
                //判断彩蛋&文件夹是否触发
                BackImageNew BackNew = new BackImageNew();
                BackImageOld Backold = new BackImageOld();
                string AppPath = Directory.GetCurrentDirectory();
                if (Directory.Exists(AppPath + "\\Config\\BackImage"))
                {
                    if (Directory.Exists(AppPath + "\\Config\\BackImage\\Null.txt"))
                    {
                        Backold.ShowAsync();
                    }
                    else
                    {
                        BackText();
                        Backold.ShowAsync();
                    }
                }
                else
                {
                    BackNew.ShowAsync();
                    Directory.CreateDirectory(AppPath + "\\Config\\BackImage");
                    BackText();
                }
            }
        }
        public void BackText()
        {
            //彩蛋文本写入
            string AppPath = Directory.GetCurrentDirectory();
            File.Create(AppPath + "\\Config\\BackImage\\Null.txt").Close();
            StreamWriter sw = new StreamWriter(AppPath + "\\Config\\BackImage\\Null.txt");
            sw.WriteLine("恭喜你找到一个神奇的彩蛋");
            sw.WriteLine("找一张你喜欢的格式为(.Jpg/.Png)的图片");
            sw.WriteLine("丢到这个文件夹里面");
            sw.WriteLine("不出意外应该会修改软件背景图片");
            sw.WriteLine("记得重启软件 我没加热更新(因为我不会 （；´д｀）ゞ)");
            sw.Close();
        }
    }
}
