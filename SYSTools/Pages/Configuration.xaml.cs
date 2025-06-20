using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using SYSTools.Model;
using System.Linq;
using System.Windows.Media;

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
                // 获取系统语言
                string systemLanguage = System.Globalization.CultureInfo.CurrentUICulture.Name.ToLower();
                
                // 设置默认语言，如果系统是中文则使用中文，否则使用英文
                string defaultLanguage = systemLanguage.StartsWith("zh-") ? "zh-CN" : "en";
                AppSettings.Instance.Language = defaultLanguage;
                
                // 设置默认背景不透明度为100
                AppSettings.Instance.BackgroundImageOpacity = 100;
                
                // 应用语言设置
                ApplyLanguageChange(defaultLanguage);
            }

            // 根据当前语言设置选中对应的选项
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

        // 提取公共的语言切换逻辑到单独的方法
        private void ApplyLanguageChange(string languageCode)
        {
            // 切换语言资源
            System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(languageCode);
            
            // 通知所有使用ResourceExtension的绑定更新
            Model.ResourceExtension.NotifyLanguageChanged();
            
            // 刷新所有窗口以应用新的语言
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
