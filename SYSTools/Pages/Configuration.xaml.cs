using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using SYSTools.Helpers;
using SYSTools.Model;

namespace SYSTools.Pages
{
    /// <summary>
    /// Configuration.xaml 的交互逻辑
    /// </summary>
    public partial class Configuration : Page
    {
        public Configuration()
        {
            InitializeComponent();
            LoadUserSettings();
            InitializeLanguageSelection();
            DataContext = this;
        }

        private void InitializeLanguageSelection()
        {
            // 检查是否是第一次启动
            if (string.IsNullOrEmpty(AppSettings.Instance.Language))
            {
                string systemLanguage = System.Globalization.CultureInfo.CurrentUICulture.Name.ToLower();
                string defaultLanguage = systemLanguage.StartsWith("zh-") ? "zh-CN" : "en";
                AppSettings.Instance.Language = defaultLanguage;
                ApplyLanguageChange(defaultLanguage);
            }
            string currentLanguage = AppSettings.Instance.Language;
            foreach (ComboBoxItem item in LanguageComboBox.Items)
            {
                if (item.Tag.ToString() == currentLanguage)
                {
                    LanguageComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LanguageComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string languageCode = selectedItem.Tag.ToString();
                AppSettings.Instance.Language = languageCode;
                ApplyLanguageChange(languageCode);
            }
        }

        private void ApplyLanguageChange(string languageCode)
        {
            var culture = new System.Globalization.CultureInfo(languageCode);
            System.Threading.Thread.CurrentThread.CurrentCulture = culture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
            LocalizationManager.Instance.CurrentCulture = culture;
            foreach (Window window in Application.Current.Windows)
            {
                if (window.Content is FrameworkElement content)
                {
                    content.Language = System.Windows.Markup.XmlLanguage.GetLanguage(languageCode);
                }
            }
        }

        // 背景图片设定按钮
        private void SelectBackgroundButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "Image files (*.jpg;*.png)|*.jpg;*.png";
            if (openFileDialog.ShowDialog() == true)
            {
                AppSettings.Instance.BackgroundImagePath = openFileDialog.FileName;
                LoadBackgroundImage(openFileDialog.FileName);
            }
        }

        // 背景图片清除按钮(设定为内置透明图片)
        private void DeleteBackgroundButton_Click(object sender, RoutedEventArgs e)
        {
            AppSettings.Instance.BackgroundImagePath = "pack://application:,,,/Resources/NoBackImage.png";
            AppSettings.Instance.BackgroundImageBlurRadius = 0;
            AppSettings.Instance.BackgroundImageOpacity = 100;
            LoadBackgroundImage("");
        }

        // 加载背景图片到预览窗口
        private void LoadBackgroundImage(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
            {
                BackgroundPreview.Source = null;
                return;
            }
            else
            {
                try
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                    bitmap.EndInit();
                    BackgroundPreview.Source = bitmap;
                }
                catch (Exception)
                {
                    BackgroundPreview.Source = null;
                }
            }
        }

        // 加载用户Config文件
        private void LoadUserSettings()
        {
            string savedImagePath = AppSettings.Instance.BackgroundImagePath;
            if (!string.IsNullOrWhiteSpace(savedImagePath) && File.Exists(savedImagePath))
            {
                LoadBackgroundImage(savedImagePath);
            }
        }

        private void BackgroundToggle_Toggled(object sender, RoutedEventArgs e)
        {
            BackImageSettingsExpander.IsExpanded = BackgroundToggle.IsOn;
        }

    }
}
