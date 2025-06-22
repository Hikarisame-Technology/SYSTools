using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SYSTools.Helpers;
using SYSTools.Model;
using SYSTools.ViewModels;

using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;

namespace SYSTools.Pages
{
    public partial class CustomTool : Page
    {
        [DllImport("user32.dll")]
        private static extern bool ChangeWindowMessageFilter(uint message, uint dwFlag);
        private const uint WM_DROPFILES = 0x0233;
        private const uint WM_COPYDATA = 0x004A;
        private const uint WM_COPYGLOBALDATA = 0x0049;
        private const uint MSGFLT_ADD = 1;

        private LowLevelDragDrop _lowLevelDragDrop;
        
        public CustomTool()
        {
            InitializeComponent();
            DataContext = new CustomToolViewModel();
        }

        private void ToolItemContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            if (sender is ContextMenu menu)
            {
                menu.DataContext = this.DataContext;
            }
        }

        private void MainGrid_DragOver(object sender, DragEventArgs e)
        {
            try
            {
                // 简化检查逻辑
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    e.Effects = DragDropEffects.Copy;
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"拖拽悬停事件错误: {ex.Message}");
                e.Effects = DragDropEffects.None;
            }
        }

        private void MainGrid_DragLeave(object sender, DragEventArgs e)
        {
            try
            {
                
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"拖拽离开事件错误: {ex.Message}");
            }
        }

        private void MainGrid_Drop(object sender, DragEventArgs e)
        {
            try
            {                
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    var files = (string[])e.Data.GetData(DataFormats.FileDrop);                    
                    if (files != null && files.Length > 0)
                    {
                        var validFiles = files.Where(f => 
                        {
                            var ext = Path.GetExtension(f).ToLower();
                            return ext == ".exe" || ext == ".lnk";
                        }).ToArray();
                                               
                        if (validFiles.Length > 0)
                        {
                            ProcessDroppedFiles(validFiles);
                        }
                        else
                        {
                            MessageBox.Show("请拖拽exe或lnk文件", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    else
                    {
                        MessageBox.Show("未找到有效的文件进行添加", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"处理拖拽文件时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateDragOverlayText(IDataObject data)
        {
            try
            {
                var filePaths = DragDropHelper.ExtractFilePaths(data);
                if (filePaths.Length == 0) return;

                var message = filePaths.Length == 1 
                    ? $"添加工具: {Path.GetFileNameWithoutExtension(filePaths[0])}"
                    : $"批量添加 {filePaths.Length} 个工具";

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"更新拖拽覆盖层文本错误: {ex.Message}");
            }
        }

        private async void ProcessDroppedFiles(string[] files)
        {
            var viewModel = DataContext as CustomToolViewModel;
            if (viewModel == null) return;

            var successCount = 0;
            var failureCount = 0;

            foreach (var file in files)
            {
                try
                {
                    if (await ProcessSingleFile(file, viewModel))
                        successCount++;
                    else
                        failureCount++;
                }
                catch (Exception ex)
                {
                    failureCount++;
                }
            }

            if (files.Length > 1)
            {
                var message = $"处理完成！成功添加 {successCount} 个工具";
                if (failureCount > 0)
                    message += $"，失败 {failureCount} 个";
                
                MessageBox.Show(message, "批量添加结果", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async Task<bool> ProcessSingleFile(string filePath, CustomToolViewModel viewModel)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    return await ProcessFile(filePath, viewModel);
                }
                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private async Task<bool> ProcessFile(string filePath, CustomToolViewModel viewModel)
        {
            try
            {
                if (DragDropHelper.IsShortcutFile(filePath))
                {
                    return await AddShortcutFile(filePath, viewModel);
                }
                else if (DragDropHelper.IsExecutableFile(filePath))
                {
                    return await AddExecutableFile(filePath, viewModel);
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"处理文件错误: {ex.Message}");
                MessageBox.Show($"处理文件时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }

        private async Task<bool> AddExecutableFile(string exePath, CustomToolViewModel viewModel)
        {
            // 解析EXE文件
            try
            {
                var originalName = Path.GetFileNameWithoutExtension(exePath);
                var toolName = DragDropHelper.CreateSafeFileName(originalName);
                
                if (viewModel.ToolItems.Any(t => t.Name.Equals(toolName, StringComparison.OrdinalIgnoreCase)))
                {
                    var result = MessageBox.Show($"已存在名为 '{toolName}' 的工具，是否替换？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes)
                        return false;
                    
                    var existingTool = viewModel.ToolItems.FirstOrDefault(t => t.Name.Equals(toolName, StringComparison.OrdinalIgnoreCase));
                    if (existingTool != null)
                        viewModel.ToolItems.Remove(existingTool);
                }

                var toolItem = new ToolItem
                {
                    Name = toolName,
                    ExePath = exePath,
                    IconPath = exePath,
                    Arguments = ""
                };

                try
                {
                    toolItem.IconSource = IconHelper.LoadIcon(exePath);
                }
                catch (Exception iconEx)
                {
                    Debug.WriteLine($"加载图标失败: {iconEx.Message}");
                    toolItem.IconSource = null;
                }

                viewModel.ToolItems.Add(toolItem);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"添加可执行文件错误: {ex.Message}");
                MessageBox.Show($"添加可执行文件时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private async Task<bool> AddShortcutFile(string lnkPath, CustomToolViewModel viewModel)
        {
            // 解析快捷方式
            try
            {
                var targetPath = GetShortcutTarget(lnkPath);
                if (string.IsNullOrEmpty(targetPath) || !File.Exists(targetPath))
                {
                    MessageBox.Show($"无法解析快捷方式: {Path.GetFileName(lnkPath)}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                var toolName = Path.GetFileNameWithoutExtension(lnkPath);
                
                if (viewModel.ToolItems.Any(t => t.Name.Equals(toolName, StringComparison.OrdinalIgnoreCase)))
                {
                    var result = MessageBox.Show($"已存在名为 '{toolName}' 的工具，是否替换？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes)
                        return false;
                    
                    var existingTool = viewModel.ToolItems.FirstOrDefault(t => t.Name.Equals(toolName, StringComparison.OrdinalIgnoreCase));
                    if (existingTool != null)
                        viewModel.ToolItems.Remove(existingTool);
                }

                var toolItem = new ToolItem
                {
                    Name = toolName,
                    ExePath = targetPath,
                    IconPath = targetPath,
                    Arguments = ""
                };
                try
                {
                    toolItem.IconSource = IconHelper.LoadIcon(targetPath);
                }
                catch
                {
                    toolItem.IconSource = null;
                }

                viewModel.ToolItems.Add(toolItem);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"添加快捷方式文件错误: {ex.Message}");
                return false;
            }
        }

        private string GetShortcutTarget(string shortcutPath)
        {
            try
            {
                return GetShortcutTargetUsingShell(shortcutPath);
            }
            catch
            {
                return string.Empty;
            }
        }

        private string GetShortcutTargetUsingShell(string shortcutPath)
        {
            try
            {
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null) return string.Empty;

                dynamic shell = Activator.CreateInstance(shellType);
                if (shell == null) return string.Empty;

                var shortcut = shell.CreateShortcut(shortcutPath);
                if (shortcut == null) return string.Empty;

                string targetPath = shortcut.TargetPath;
                return targetPath ?? string.Empty;
            }
            catch
            {
                return TryParseShortcutSimple(shortcutPath);
            }
        }

        private string TryParseShortcutSimple(string shortcutPath)
        {
            try
            {
                var fileName = Path.GetFileNameWithoutExtension(shortcutPath);
                var result = MessageBox.Show(
                    $"无法自动解析快捷方式 '{fileName}'。\n是否浏览选择目标程序？",
                    "快捷方式解析失败",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    var openFileDialog = new Microsoft.Win32.OpenFileDialog
                    {
                        Title = $"选择 '{fileName}' 的目标程序",
                        Filter = "可执行文件|*.exe|所有文件|*.*",
                        InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
                    };

                    if (openFileDialog.ShowDialog() == true)
                    {
                        return openFileDialog.FileName;
                    }
                }
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {            
            this.AllowDrop = true;
            MainGrid.AllowDrop = true;

            try
            {
                ChangeWindowMessageFilter(WM_DROPFILES, MSGFLT_ADD);
                ChangeWindowMessageFilter(WM_COPYDATA, MSGFLT_ADD);
                ChangeWindowMessageFilter(WM_COPYGLOBALDATA, MSGFLT_ADD);
            }
            catch (Exception ex)
            {
            }

            try
            {
                var parentWindow = Window.GetWindow(this);
                if (parentWindow != null)
                {
                    _lowLevelDragDrop = new LowLevelDragDrop(parentWindow);
                    _lowLevelDragDrop.FilesDropped += OnLowLevelFilesDropped;
                    _lowLevelDragDrop.Enable();
                }
                else
                {

                }
            }
            catch (Exception ex)
            {

            }

        }

        private void OnLowLevelFilesDropped(string[] files)
        {
            try
            {                
                Dispatcher.Invoke(() =>
                {
                    var validFiles = files.Where(f => 
                    {
                        var ext = Path.GetExtension(f).ToLower();
                        return ext == ".exe" || ext == ".lnk";
                    }).ToArray();
                                        
                    if (validFiles.Length > 0)
                    {
                        ProcessDroppedFiles(validFiles);
                    }
                    else
                    {
                        MessageBox.Show("请拖拽exe或lnk文件", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                });
            }
            catch (Exception ex)
            {

            }
        }
    }
}
