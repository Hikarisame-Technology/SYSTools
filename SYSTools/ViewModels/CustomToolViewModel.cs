using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using SYSTools.Model;
using SYSTools.Helpers;
using SYSTools.Dialog;
using System;
using System.Windows;

namespace SYSTools.ViewModels
{
    public class CustomToolViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<ToolItem> _toolItems = new ObservableCollection<ToolItem>();

        public ObservableCollection<ToolItem> ToolItems
        {
            get => _toolItems;
            set
            {
                if (_toolItems != value)
                {
                    _toolItems = value;
                    OnPropertyChanged(nameof(ToolItems));
                }
            }
        }

        public ICommand AddToolCommand { get; }
        public ICommand DeleteToolCommand { get; }
        public ICommand EditToolCommand { get; }

        public CustomToolViewModel()
        {
            AddToolCommand = new RelayCommand(_ => AddTool());
            DeleteToolCommand = new RelayCommand(p => DeleteTool(p as ToolItem));
            EditToolCommand = new RelayCommand(p => EditTool(p as ToolItem));
        }

        private async void AddTool()
        {
            AddToolDialog dialog = new AddToolDialog();
            var result = await dialog.ShowAsync();
            if (result == iNKORE.UI.WPF.Modern.Controls.ContentDialogResult.Primary)
            {
                var item = new ToolItem
                {
                    Name = dialog.ToolName,
                    IconPath = dialog.IconPath,
                    ExePath = dialog.ExePath,
                    Arguments = dialog.Arguments
                };

                if (dialog.UseExeIcon)
                {
                    item.IconSource = IconHelper.LoadIcon(dialog.ExePath);
                }
                else if (!string.IsNullOrWhiteSpace(dialog.IconPath))
                {
                    try
                    {
                        item.IconSource = new System.Windows.Media.Imaging.BitmapImage(new Uri(dialog.IconPath));
                    }
                    catch
                    {
                    }
                }
                ToolItems.Add(item);
            }
        }
        private void DeleteTool(ToolItem? item)
        {
            if (item == null) return;
            var confirm = iNKORE.UI.WPF.Modern.Controls.MessageBox.Show($"确定要删除工具 '{item.Name}' 吗？", "确认删除", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (confirm == MessageBoxResult.OK)
            {
                ToolItems.Remove(item);
            }
        }
        private async void EditTool(ToolItem? item)
        {
            if (item == null) return;
            var confirm = iNKORE.UI.WPF.Modern.Controls.MessageBox.Show($"是否要修改工具 '{item.Name}'？", "确认修改", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.OK) return;
            var dialog = new AddToolDialog(item);
            var result = await dialog.ShowAsync();
            if (result == iNKORE.UI.WPF.Modern.Controls.ContentDialogResult.Primary)
            {
                item.Name = dialog.ToolName;
                item.ExePath = dialog.ExePath;
                item.Arguments = dialog.Arguments;
                if (dialog.UseExeIcon)
                {
                    item.IconPath = dialog.ExePath;
                    item.IconSource = IconHelper.LoadIcon(dialog.ExePath);
                }
                else
                {
                    item.IconPath = dialog.IconPath;
                    try { item.IconSource = new System.Windows.Media.Imaging.BitmapImage(new Uri(dialog.IconPath)); }
                    catch { }
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 