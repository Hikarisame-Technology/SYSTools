using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
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
            this.Loaded += Configuration_Loaded;
        }
        private void Configuration_Loaded(object sender, RoutedEventArgs e)
        {
            LoadUserSettings();
        }
        private void SaveBackgroundImagePath(string imagePath)
        {
            Properties.Settings.Default.BackgroundImagePath = imagePath;
            Properties.Settings.Default.Save();
        }

        private void SelectBackgroundButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.jpg;*.png)|*.jpg;*.png";
            if (openFileDialog.ShowDialog() == true)
            {
                SaveBackgroundImagePath(openFileDialog.FileName);
                LoadBackgroundImage(openFileDialog.FileName);
                UpdateBackgroundImage(openFileDialog.FileName);
            }
        }

        private void UpdateBackgroundImage(string imagePath)
        {
            SaveBackgroundImagePath(imagePath);
            LoadBackgroundImage(imagePath);
            OnBackgroundChanged?.Invoke();
        }

        private void LoadBackgroundImage(string imagePath)
        {
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
            bitmap.EndInit();
            BackgroundPreview.Source = bitmap;
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
