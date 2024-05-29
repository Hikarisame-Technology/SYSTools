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
        private Frame WindowsTools = new Frame() { Content = new WindowsTools() };
        private Frame WSATools = new Frame() { Content = new WSATools() };
        private Frame ConfigurationPage = new Frame() { Content = new Configuration() };
        private Frame AboutPage = new Frame() { Content = new About() };
        string AppPath = Directory.GetCurrentDirectory();

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
            ConfigurationPage.Content = new Configuration
            {
                // 设置OnBackgroundChanged委托的实现
                OnBackgroundChanged = () =>
                {
                    LoadUserSettings();
                }
            };
            FrameContent.Content = ConfigurationPage;
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

        private void Windows_Tools_Page_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            FrameContent.Content = WindowsTools;
        }

        private void WSA_Tools_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            FrameContent.Content = WSATools;
        }

        private void Software_Configuration_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            FrameContent.Content = ConfigurationPage;
        }

        private void About_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            FrameContent.Content = AboutPage;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadUserSettings();
        }
        private void SettingsPage_BackgroundChanged(object sender, EventArgs e)
        {
            LoadUserSettings();
        }

        private void LoadBackgroundImage(string imagePath)
        {
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
            bitmap.EndInit();
            BackImage.Source = bitmap;
        }

        private void LoadUserSettings()
        {
            string savedImagePath = Properties.Settings.Default.BackgroundImagePath;
            if (!string.IsNullOrWhiteSpace(savedImagePath) && File.Exists(savedImagePath))
            {
                LoadBackgroundImage(savedImagePath);
            }
        }

    }
}
