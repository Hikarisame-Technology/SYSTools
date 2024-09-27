using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using SYSTools.Model;

namespace SYSTools.Pages
{
    /// <summary>
    /// Configuration.xaml 的交互逻辑
    /// </summary>
    public partial class Configuration : Page
    {
        public Action OnBackgroundChanged { get; set; }

        public Configuration()
        {
            InitializeComponent();
            LoadUserSettings();
            GlobalSettings.Instance.BackgroundImageBlurRadius = Properties
                .Settings
                .Default
                .BackgroundImageBlurRadius;
            DataContext = this;
        }

        // 背景图片设定按钮
        private void SelectBackgroundButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "Image files (*.jpg;*.png)|*.jpg;*.png";
            if (openFileDialog.ShowDialog() == true)
            {
                GlobalSettings.Instance.BackgroundImagePath = openFileDialog.FileName;
                SaveBackgroundImagePath(openFileDialog.FileName);
                LoadBackgroundImage(openFileDialog.FileName);
            }
        }

        // 背景图片清除按钮(设定为内置透明图片)
        private void DeleteBackgroundButton_Click(object sender, RoutedEventArgs e)
        {
            GlobalSettings.Instance.BackgroundImagePath = "pack://application:,,,/Resources/NoBackImage.png";
            SaveBackgroundImagePath("");
            LoadBackgroundImage("");
        }

        // 保存图片地址到Config文件
        private void SaveBackgroundImagePath(string imagePath)
        {
            Properties.Settings.Default.BackgroundImagePath = imagePath;
            Properties.Settings.Default.Save();
        }

        // 加载背景图片到预览窗口
        private void LoadBackgroundImage(string imagePath)
        {
            if (imagePath == "")
            {
                BackgroundPreview.Source = null;
            }
            else
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                bitmap.EndInit();
                BackgroundPreview.Source = bitmap;
            }
        }

        // 加载用户Config文件
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
