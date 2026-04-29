using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using SYSTools.Dialog;
using SYSTools.Helpers;
using SYSTools.Model;
using SYSTools.Properties;
using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;
using System.IO;

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
        public ICommand ModifyToolCommand { get; }
        public ICommand OpenFolderCommand { get; }

        public CustomToolViewModel()
        {
            AddToolCommand = new RelayCommand(_ => AddTool());
            DeleteToolCommand = new RelayCommand(p => DeleteTool(p as ToolItem));
            ModifyToolCommand = new RelayCommand(p => ModifyTool(p as ToolItem));
            OpenFolderCommand = new RelayCommand(p => OpenFolder(p as ToolItem));
            LoadCustomTools();
            ToolItems.CollectionChanged += (s, e) => SaveCustomTools();
        }

        private async void AddTool()
        {
            AddToolDialog dialog = new AddToolDialog();
            var result = await ContentDialogHelper.ShowAsync(dialog);
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
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[CustomToolViewModel] 加载图标失败: {ex.Message}");
                    }
                }
                ToolItems.Add(item);
            }
        }

        private async void DeleteTool(ToolItem? item)
        {
            if (item == null) return;
            
            string message = string.Format(Lang.ConfirmDeleteMessage, item.Name);
            var confirm = await ContentDialogHelper.ShowConfirmationAsync(
                Lang.ConfirmDeleteTitle, 
                message, 
                "确定", 
                "取消"
            );
            
            if (confirm == iNKORE.UI.WPF.Modern.Controls.ContentDialogResult.Primary)
            {
                ToolItems.Remove(item);
            }
        }

        private async void ModifyTool(ToolItem? item)
        {
            if (item == null) return;
            
            string message = string.Format(Lang.ConfirmModifyMessage, item.Name);
            var confirm = await ContentDialogHelper.ShowConfirmationAsync(
                Lang.ConfirmModifyTitle, 
                message, 
                "确定", 
                "取消"
            );
            
            if (confirm != iNKORE.UI.WPF.Modern.Controls.ContentDialogResult.Primary) return;
            
            var dialog = new AddToolDialog(item);
            var result = await ContentDialogHelper.ShowAsync(dialog);
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
                    try
                    {
                        item.IconSource = new System.Windows.Media.Imaging.BitmapImage(new Uri(dialog.IconPath));
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[CustomToolViewModel] 修改工具时加载图标失败: {ex.Message}");
                    }
                }
            }
        }

        private void OpenFolder(ToolItem item)
        {
            if (item == null) return;
            try
            {
                var folder = Path.GetDirectoryName(item.ExePath);
                if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = folder,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法打开目录 '{item.Name}': {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void LoadCustomTools()
        {
            try
            {
                var savedTools = Model.CustomToolSettings.Instance.LoadCustomTools();
                ToolItems.Clear();
                foreach (var tool in savedTools)
                {
                    ToolItems.Add(tool);
                }
                
                Debug.WriteLine($"已载入 {savedTools.Count} 个自定义工具");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"载入自定义工具失败: {ex.Message}");
            }
        }

        private void SaveCustomTools()
        {
            try
            {
                CustomToolSettings.Instance.SaveCustomTools(ToolItems);
                Debug.WriteLine($"已保存 {ToolItems.Count} 个自定义工具");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"保存自定义工具失败: {ex.Message}");
            }
        }

        public void SaveCustomToolsManually()
        {
            SaveCustomTools();
        }

        public void ReloadCustomTools()
        {
            LoadCustomTools();
        }
    }
} 