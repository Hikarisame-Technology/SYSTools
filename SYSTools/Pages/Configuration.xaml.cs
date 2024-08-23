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
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;

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

        private void SelectBackgroundButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "Image files (*.jpg;*.png)|*.jpg;*.png";
            if (openFileDialog.ShowDialog() == true)
            {
                SaveBackgroundImagePath(openFileDialog.FileName);
                LoadBackgroundImage(openFileDialog.FileName);
                UpdateBackgroundImage(openFileDialog.FileName);
            }
        }

        private void DeleteBackgroundButton_Click(object sender, RoutedEventArgs e)
        {
            SaveBackgroundImagePath("");
            LoadBackgroundImage("");
            UpdateBackgroundImage("");
        }

        private void SaveBackgroundImagePath(string imagePath)
        {
            Properties.Settings.Default.BackgroundImagePath = imagePath;
            Properties.Settings.Default.Save();
        }

        private void UpdateBackgroundImage(string imagePath)
        {
            if (imagePath == "")
            {
                SaveBackgroundImagePath(imagePath);
                LoadBackgroundImage(imagePath);
                OnBackgroundChanged?.Invoke();
            }
            else
            {
                SaveBackgroundImagePath(imagePath);
                LoadBackgroundImage(imagePath);
                OnBackgroundChanged?.Invoke();
            }
        }

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
