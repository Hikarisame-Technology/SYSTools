using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
        
        private bool _isDraggingTool = false;
        private Point _dragStartPoint;
        private ToolItem _draggedToolItem;
        private Border _draggedBorder;
        private Border _currentHoverBorder;
        
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
                // 只处理文件拖拽
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
                    Debug.WriteLine(ex.Message);
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
                Debug.WriteLine(ex.Message);
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
            // 解析快捷方式路径和参数
            try
            {
                string targetPath = null;
                string arguments = string.Empty;
                // 尝试通过 WScript.Shell 获取快捷方式属性
                try
                {
                    Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                    if (shellType != null)
                    {
                        dynamic shell = Activator.CreateInstance(shellType);
                        dynamic shortcut = shell.CreateShortcut(lnkPath);
                        if (shortcut != null)
                        {
                            targetPath = shortcut.TargetPath as string;
                            arguments = shortcut.Arguments as string ?? string.Empty;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[CustomTool] COM快捷方式解析失败: {ex.Message}");
                }
                // 回退到原有方法解析目标路径
                if (string.IsNullOrEmpty(targetPath) || !File.Exists(targetPath))
                {
                    targetPath = GetShortcutTarget(lnkPath);
                }
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
                    Arguments = arguments
                };
                try
                {
                    toolItem.IconSource = IconHelper.LoadIcon(targetPath);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[CustomTool] 加载工具图标失败: {ex.Message}");
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
            catch (Exception ex)
            {
                Debug.WriteLine($"[CustomTool] GetShortcutTarget 解析失败: {ex.Message}");
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
            catch (Exception ex)
            {
                Debug.WriteLine($"[CustomTool] GetShortcutTargetUsingShell 失败: {ex.Message}");
                return TryParseShortcutSimple(shortcutPath);
            }
        }

        private string TryParseShortcutSimple(string shortcutPath)
        {
            try
            {
                var fileName = Path.GetFileNameWithoutExtension(shortcutPath);
                var result = MessageBox.Show(
                    $"无法自动解析快捷方式 '{fileName}'。\\n是否浏览选择目标程序？",
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
            catch (Exception ex)
            {
                Debug.WriteLine($"[CustomTool] TryParseShortcutSimple 失败: {ex.Message}");
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
                Debug.WriteLine(ex.Message);
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
                Debug.WriteLine(ex.Message);
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
                Debug.WriteLine(ex.Message);
            }
        }

        private void ToolItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.OriginalSource is Button button)
                {
                    return;
                }
                
                if (sender is Border border && border.DataContext is ToolItem toolItem)
                {
                    Debug.WriteLine($"MouseLeftButtonDown: {toolItem.Name}");
                    _dragStartPoint = e.GetPosition(border);
                    _draggedToolItem = toolItem;
                    _draggedBorder = border;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void ToolItem_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (e.LeftButton == MouseButtonState.Pressed && 
                    sender is Border border && 
                    border.DataContext is ToolItem toolItem &&
                    _draggedToolItem == toolItem)
                {
                    Point currentPosition = e.GetPosition(border);
                    
                    if (!_isDraggingTool &&
                        (Math.Abs(currentPosition.X - _dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                         Math.Abs(currentPosition.Y - _dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance))
                    {
                        _isDraggingTool = true;
                        _draggedBorder = border;
                        border.CaptureMouse();
                        border.Cursor = Cursors.Hand;
                        border.Opacity = 0.7;
                    }

                    if (_isDraggingTool)
                    {
                        Point screenPoint = border.PointToScreen(currentPosition);
                        Point pagePoint = this.PointFromScreen(screenPoint);
                        
                        var hitElement = this.InputHitTest(pagePoint) as FrameworkElement;
                        var targetBorder = FindParentBorder(hitElement);
                        
                        if (targetBorder != null && targetBorder != border && targetBorder.DataContext is ToolItem)
                        {
                            if (_currentHoverBorder != targetBorder)
                            {
                                if (_currentHoverBorder != null)
                                {
                                    _currentHoverBorder.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0xCC, 0x20, 0x20, 0x20));
                                    _currentHoverBorder.BorderThickness = new Thickness(1);
                                }

                                _currentHoverBorder = targetBorder;
                                _currentHoverBorder.BorderBrush = System.Windows.Media.Brushes.DodgerBlue;
                                _currentHoverBorder.BorderThickness = new Thickness(3);
                            }
                        }
                        else if (_currentHoverBorder != null)
                        {
                            _currentHoverBorder.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0xCC, 0x20, 0x20, 0x20));
                            _currentHoverBorder.BorderThickness = new Thickness(1);
                            _currentHoverBorder = null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"工具项鼠标移动事件错误: {ex.Message}");
            }
        }

        private void ToolItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (_isDraggingTool && sender is Border border)
                {
                    if (_currentHoverBorder != null && _currentHoverBorder.DataContext is ToolItem targetItem)
                    {
                        var draggedItem = _draggedToolItem;
                        if (draggedItem != null && draggedItem != targetItem)
                        {
                            var viewModel = DataContext as CustomToolViewModel;
                            if (viewModel != null)
                            {
                                ReorderToolItems(viewModel, draggedItem, targetItem);
                            }
                        }
                    }
                    ResetDragState();
                }
                else if (!_isDraggingTool)
                {
                    _draggedToolItem = null;
                    _draggedBorder = null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"工具项鼠标释放事件错误: {ex.Message}");
                ResetDragState();
            }
        }

        private void ResetDragState()
        {
            try
            {
                if (_currentHoverBorder != null)
                {
                    _currentHoverBorder.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0xCC, 0x20, 0x20, 0x20));
                    _currentHoverBorder.BorderThickness = new Thickness(1);
                    _currentHoverBorder = null;
                }
                
                if (_draggedBorder != null)
                {
                    _draggedBorder.ReleaseMouseCapture();
                    _draggedBorder.Cursor = Cursors.Hand;
                    _draggedBorder.Opacity = 1.0;
                }
                
                _isDraggingTool = false;
                _draggedToolItem = null;
                _draggedBorder = null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"重置拖拽状态错误: {ex.Message}");
            }
        }

        private Border FindParentBorder(FrameworkElement element)
        {
            try
            {
                var current = element;
                while (current != null)
                {
                    if (current is Border border && border.DataContext is ToolItem)
                    {
                        return border;
                    }
                    current = current.Parent as FrameworkElement ?? 
                              System.Windows.Media.VisualTreeHelper.GetParent(current) as FrameworkElement;
                }
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CustomTool] FindParentBorder 查找父级失败: {ex.Message}");
                return null;
            }
        }

        private void ReorderToolItems(CustomToolViewModel viewModel, ToolItem draggedItem, ToolItem targetItem)
        {
            try
            {
                int draggedIndex = viewModel.ToolItems.IndexOf(draggedItem);
                int targetIndex = viewModel.ToolItems.IndexOf(targetItem);

                if (draggedIndex != -1 && targetIndex != -1 && draggedIndex != targetIndex)
                {
                    viewModel.ToolItems.Move(draggedIndex, targetIndex);
                    viewModel.SaveCustomToolsManually();
                }
                else
                {

                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"重新排列工具项错误: {ex.Message}");
                MessageBox.Show($"重新排列工具项时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
