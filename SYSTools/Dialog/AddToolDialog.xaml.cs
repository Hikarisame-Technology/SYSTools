using System;
using System.Windows;
using iNKORE.UI.WPF.Modern.Controls;
using Microsoft.Win32;

namespace SYSTools.Dialog
{
    public partial class AddToolDialog : ContentDialog
    {
        public bool UseExeIcon => UseExeIconRadio.IsChecked == true;
        public bool UseCustomIcon => UseCustomIconRadio.IsChecked == true;

        public string ToolName => NameTextBox.Text;
        public string IconPath => IconTextBox.Text;
        public string ExePath => ExeTextBox.Text;
        public string Arguments => ArgsTextBox.Text;

        public AddToolDialog()
        {
            InitializeComponent();
        }
        public AddToolDialog(SYSTools.Model.ToolItem item) : this()
        {
            NameTextBox.Text = item.Name;
            ExeTextBox.Text = item.ExePath;
            ArgsTextBox.Text = item.Arguments;
            // 设置图标来源
            if (item.IconPath == item.ExePath)
            {
                UseExeIconRadio.IsChecked = true;
            }
            else
            {
                UseCustomIconRadio.IsChecked = true;
                IconTextBox.Text = item.IconPath;
            }
            IconOption_Checked(null, null);
        }

        private void BrowseIcon_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "选择图标",
                Filter = "图像文件|*.png;*.jpg;*.ico|所有文件|*.*"
            };
            if (dlg.ShowDialog() == true)
            {
                IconTextBox.Text = dlg.FileName;
            }
        }

        private void BrowseExe_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "选择可执行文件",
                Filter = "可执行文件|*.exe|所有文件|*.*"
            };
            if (dlg.ShowDialog() == true)
            {
                ExeTextBox.Text = dlg.FileName;
            }
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (string.IsNullOrWhiteSpace(ToolName) || string.IsNullOrWhiteSpace(ExePath))
            {
                iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("名称和可执行文件为必填项", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                args.Cancel = true;
                return;
            }
            if (UseExeIconRadio.IsChecked == true)
            {
                IconTextBox.Text = ExePath;
            }
        }

        private void IconOption_Checked(object sender, RoutedEventArgs e)
        {
            bool isCustom = UseCustomIconRadio.IsChecked == true;
            IconTextBox.IsEnabled = isCustom;
            BrowseIconButton.IsEnabled = isCustom;
        }
    }
}