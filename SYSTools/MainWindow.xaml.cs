using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using SYSTools.Dialog;
using SYSTools.Pages;
using SYSTools.ToolPages;

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
        private Frame WindowsTools = new Frame() { Content = new WindowsTools() };
        private Frame WindowsActivation = new Frame() { Content = new Activation() };
        private Frame AboutPage = new Frame() { Content = new About() };
        string AppPath = Directory.GetCurrentDirectory();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            File.Delete("Info.xml");
            //程序启动数量限制
            string appName = Process.GetCurrentProcess().ProcessName;
            int processTotal = Process.GetProcessesByName(appName).Length;
            if (processTotal > 1)
            {
                MessageBox.Show("有一个同名进程正在运行！", "程序冲突!", MessageBoxButton.OK);
                Close();
            }
            Title = "SYSTools Ver" + (Application.ResourceAssembly.GetName().Version.ToString());
            //设置默认启动Page页
            FrameContent.Content = Home_Page;
        }

        private void Home_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            FrameContent.Content = Home_Page;
        }

        private void Fast_Detection_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            FrameContent.Content = FastDetection;
        }

        private void Detection_Tools_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            FrameContent.Content = DetectionTools;
        }

        private void Test_Tools_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            FrameContent.Content = TestTools;
        }

        private void Disk_Tools_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            FrameContent.Content = DiskTools;
        }

        private void Peripherals_Tools_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            FrameContent.Content = PeripheralsTools;
        }

        private void Repairing_Tools_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            FrameContent.Content = RepairingTools;
        }

        private void Adb_Tools_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            FrameContent.Content = AdbTools;
        }

        private void WindowsTools_Page_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            FrameContent.Content = WindowsTools;
        }

        private void Windows_Activation_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            FrameContent.Content = WindowsActivation;
        }

        private void About_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            FrameContent.Content = AboutPage;
        }

        private void About_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            //鼠标右键双击事件
            if (e.ChangedButton == MouseButton.Right && e.ClickCount == 2)
            {
                //判断彩蛋&文件夹是否触发 (背景替换功能预计写入设置项内)
                BackImageNew BackNew = new BackImageNew();
                BackImageOld Backold = new BackImageOld();
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

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            //判断是否有文件夹（后期会修改）
            if (Directory.Exists(AppPath + "\\Config\\BackImage"))
            {
                LoadBackgroundImage();
            }
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
            BitmapImage bitmap = new BitmapImage();
            string imagePath = FindFile(AppPath + "\\Config\\BackImage", "jpg") ?? FindFile(AppPath + "\\Config\\BackImage", "png");
            if (imagePath != null)
            {
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath);
                bitmap.EndInit();
                BackImage.Source = bitmap;
            }
        }

        public void BackText()
        {
            //彩蛋文本写入
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
