using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using iNKORE.UI.WPF.Modern;
using iNKORE.UI.WPF.Modern.Controls;
using iNKORE.UI.WPF.Modern.Media.Animation;
using SYSTools.Model;
using SYSTools.Pages;
using SYSTools.ToolPages;
using Page = System.Windows.Controls.Page;

namespace SYSTools
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    ///

    public partial class MainWindow : Window
    {
        private readonly Page Home_Page = new Home();
        private readonly Page Test_Page = new Test();
        private readonly Page DetectionTools_Page = new DetectionTools();
        private readonly Page TestTools_Page = new TestTools();
        private readonly Page DiskTools_Page = new DiskTools();
        private readonly Page PeripheralsTools_Page = new PeripheralsTools();
        private readonly Page RepairingTools_Page = new RepairingTools();
        private readonly Page WindowsTools_Page = new WindowsTools();
        private readonly Page WSATools_Page = new WSATools();
        private readonly Page Configuration_Page = new Configuration();
        private readonly Page About_Page = new About();
        string AppPath = Directory.GetCurrentDirectory();

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
            GlobalSettings.Instance.PropertyChanged += OnSettingsPropertyChanged;
        }

        // 监听全局设置修改
        private void OnSettingsPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GlobalSettings.BackgroundImagePath))
            {
                if (!string.IsNullOrEmpty(GlobalSettings.Instance.BackgroundImagePath))
                {
                    BackImage.Source = new BitmapImage(
                        new Uri(
                            GlobalSettings.Instance.BackgroundImagePath,
                            UriKind.RelativeOrAbsolute
                        )
                    );
                }
            }
            else if (e.PropertyName == nameof(GlobalSettings.BackgroundImageBlurRadius))
            {
                LoadBackgroundImageBlurRadius(GlobalSettings.Instance.BackgroundImageBlurRadius);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 启动时删除Info.xml
            File.Delete("Info.xml");
            // 程序启动数量限制
            string appName = Process.GetCurrentProcess().ProcessName;
            int processTotal = Process.GetProcessesByName(appName).Length;
            if (processTotal > 1)
            {
                iNKORE.UI.WPF.Modern.Controls.MessageBox.Show(
                    "有一个同名进程正在运行！",
                    "程序冲突!",
                    MessageBoxButton.OK
                );
                Close();
            }
            TitleBarTextBlock.Text = "SYSTools Ver" + (Application.ResourceAssembly.GetName().Version.ToString());
            // 设置默认启动Page页
            CurrentPage.Navigate(Home_Page, new DrillInNavigationTransitionInfo());
        }

        private void NavigationTriggered(
            NavigationView sender,
            NavigationViewItemInvokedEventArgs args
        )
        {
            if (args.InvokedItemContainer != null)
                NavigateTo(
                    Type.GetType(args.InvokedItemContainer.Tag.ToString()),
                    args.RecommendedNavigationTransitionInfo
                );
        }

        private void NavigateTo(Type navPageType, NavigationTransitionInfo transitionInfo)
        {
            // 导航到目标页
            var preNavPageType = CurrentPage.Content.GetType();
            if (navPageType == preNavPageType)
                return;
            switch (navPageType)
            {
                case not null when navPageType == typeof(Home):
                    CurrentPage.Navigate(Home_Page);
                    break;
                case not null when navPageType == typeof(Test):
                    CurrentPage.Navigate(Test_Page);
                    break;
                case not null when navPageType == typeof(DetectionTools):
                    CurrentPage.Navigate(DetectionTools_Page);
                    break;
                case not null when navPageType == typeof(TestTools):
                    CurrentPage.Navigate(TestTools_Page);
                    break;
                case not null when navPageType == typeof(DiskTools):
                    CurrentPage.Navigate(DiskTools_Page);
                    break;
                case not null when navPageType == typeof(PeripheralsTools):
                    CurrentPage.Navigate(PeripheralsTools_Page);
                    break;
                case not null when navPageType == typeof(RepairingTools):
                    CurrentPage.Navigate(RepairingTools_Page);
                    break;
                case not null when navPageType == typeof(WindowsTools):
                    CurrentPage.Navigate(WindowsTools_Page);
                    break;
                case not null when navPageType == typeof(WSATools):
                    CurrentPage.Navigate(WSATools_Page);
                    break;
                case not null when navPageType == typeof(Configuration):
                    CurrentPage.Navigate(Configuration_Page);
                    break;
                case not null when navPageType == typeof(About):
                    CurrentPage.Navigate(About_Page);
                    break;
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 随时升级配置文件并保存到最新版本
            Properties.Settings.Default.Upgrade();
            Properties.Settings.Default.Save();
            // 加载用户配置
            LoadUserSettings();
        }

        private void LoadUserSettings()
        {
            // 启动程序时加载 user.config 文件
            string savedImagePath = Properties.Settings.Default.BackgroundImagePath;
            double savedBlurRadius = Properties.Settings.Default.BackgroundImageBlurRadius;
            if (!string.IsNullOrWhiteSpace(savedImagePath) && File.Exists(savedImagePath))
            {
                LoadBackgroundImage(savedImagePath);
                LoadBackgroundImageBlurRadius(savedBlurRadius);
            }
            else
            {
                // 读取不到Config的背景图片路径或路径为空则设定为全透明图片路径
                LoadBackgroundImage("pack://application:,,,/Resources/NoBackImage.png");
                LoadBackgroundImageBlurRadius(savedBlurRadius);
            }
        }

        private void LoadBackgroundImage(string imagePath)
        {
            // 加载背景图片
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
            bitmap.EndInit();
            BackImage.Source = bitmap;
        }

        private void LoadBackgroundImageBlurRadius(double RadiusInt)
        {
            // 加载背景模糊
            var blurEffect = new BlurEffect
            {
                // 获取模糊度
                Radius = RadiusInt 
            };
            // 为背景图片应用模糊效果
            BackImage.Effect = blurEffect; 
        }

        private void Dark_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //实验性功能 设定为暗色模式
            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
        }

        private void Light_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 实验性功能 设定为亮色模式
            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            // 结束线程
            Application.Current.Shutdown();
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            // 修改最小化窗口为隐藏
            if (WindowState == WindowState.Minimized)
            {
                Application.Current.MainWindow.Hide();
            }
        }
    }
}
