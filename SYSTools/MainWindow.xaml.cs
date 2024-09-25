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
        //private Frame Home_Page = new Frame() { Content = new Home() };
        //private Frame FastDetection = new Frame() { Content = new Test() };
        //private Frame DetectionTools = new Frame() { Content = new DetectionTools() };
        //private Frame TestTools = new Frame() { Content = new TestTools() };
        //private Frame DiskTools = new Frame() { Content = new DiskTools() };
        //private Frame PeripheralsTools = new Frame() { Content = new PeripheralsTools() };
        //private Frame RepairingTools = new Frame() { Content = new RepairingTools() };
        //private Frame WindowsTools = new Frame() { Content = new WindowsTools() };
        //private Frame WSATools = new Frame() { Content = new WSATools() };
        //private Frame ConfigurationPage = new Frame() { Content = new Configuration() };
        //private Frame AboutPage = new Frame() { Content = new About() };

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
            File.Delete("Info.xml");
            //程序启动数量限制
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
            Title = "SYSTools Ver" + (Application.ResourceAssembly.GetName().Version.ToString());
            //设置默认启动Page页
            CurrentPage.Navigate(Home_Page, new DrillInNavigationTransitionInfo());
        }

        private void NavigationTriggered(
            NavigationView sender,
            NavigationViewItemInvokedEventArgs args
        )
        {
            if (args.IsSettingsInvoked)
                NavigateTo(typeof(int), args.RecommendedNavigationTransitionInfo);
            else if (args.InvokedItemContainer != null)
                NavigateTo(
                    Type.GetType(args.InvokedItemContainer.Tag.ToString()),
                    args.RecommendedNavigationTransitionInfo
                );
        }

        private void NavigateTo(Type navPageType, NavigationTransitionInfo transitionInfo)
        {
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
            LoadUserSettings();
        }

        private void LoadUserSettings()
        {
            string savedImagePath = Properties.Settings.Default.BackgroundImagePath;
            double savedBlurRadius = Properties.Settings.Default.BackgroundImageBlurRadius;
            if (!string.IsNullOrWhiteSpace(savedImagePath) && File.Exists(savedImagePath))
            {
                LoadBackgroundImage(savedImagePath);
                LoadBackgroundImageBlurRadius(savedBlurRadius);
            }
            else
            {
                LoadBackgroundImage("pack://application:,,,/Resources/NoBackImage.png");
                LoadBackgroundImageBlurRadius(savedBlurRadius);
            }
        }

        private void LoadBackgroundImage(string imagePath)
        {
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
            bitmap.EndInit();
            BackImage.Source = bitmap;
        }

        private void LoadBackgroundImageBlurRadius(double RadiusInt)
        {
            var blurEffect = new BlurEffect
            {
                Radius = RadiusInt // 获取模糊度
            };
            BackImage.Effect = blurEffect; // 应用模糊效果
        }

        private void Dark_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
        }

        private void Light_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
        }
    }
}
