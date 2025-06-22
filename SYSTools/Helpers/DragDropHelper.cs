using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace SYSTools.Helpers
{
    /// <summary>
    /// 拖拽操作帮助类
    /// </summary>
    public static class DragDropHelper
    {
        /// <summary>
        /// 支持的可执行文件扩展名
        /// </summary>
        public static readonly string[] ExecutableExtensions = 
        {
            ".exe"
        };

        /// <summary>
        /// 支持的快捷方式扩展名
        /// </summary>
        public static readonly string[] ShortcutExtensions = 
        {
            ".lnk"
        };

        /// <summary>
        /// 检查拖拽数据是否包含有效文件
        /// </summary>
        public static bool IsValidDragData(IDataObject data)
        {
            try
            {
                if (data.GetDataPresent(DataFormats.FileDrop))
                {
                    var files = (string[])data.GetData(DataFormats.FileDrop);
                    return files?.Any(IsValidFileForTool) == true;
                }

                // 检查是否为文本拖拽（可能是路径）
                if (data.GetDataPresent(DataFormats.Text))
                {
                    var text = (string)data.GetData(DataFormats.Text);
                    return IsValidFileForTool(text);
                }

                // 检查是否为Unicode文本拖拽
                if (data.GetDataPresent(DataFormats.UnicodeText))
                {
                    var text = (string)data.GetData(DataFormats.UnicodeText);
                    return IsValidFileForTool(text);
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 从拖拽数据中提取文件路径
        /// </summary>
        public static string[] ExtractFilePaths(IDataObject data)
        {
            var filePaths = new List<string>();

            try
            {
                // 优先处理文件拖拽
                if (data.GetDataPresent(DataFormats.FileDrop))
                {
                    var files = (string[])data.GetData(DataFormats.FileDrop);
                    if (files != null)
                    {
                        filePaths.AddRange(files.Where(IsValidFileForTool));
                    }
                }

                // 处理文本拖拽（可能是路径）
                if (filePaths.Count == 0 && data.GetDataPresent(DataFormats.Text))
                {
                    var text = (string)data.GetData(DataFormats.Text);
                    if (IsValidFileForTool(text))
                    {
                        filePaths.Add(text);
                    }
                }

                // 处理Unicode文本拖拽
                if (filePaths.Count == 0 && data.GetDataPresent(DataFormats.UnicodeText))
                {
                    var text = (string)data.GetData(DataFormats.UnicodeText);
                    if (IsValidFileForTool(text))
                    {
                        filePaths.Add(text);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"提取文件路径错误: {ex.Message}");
            }

            return filePaths.ToArray();
        }

        /// <summary>
        /// 检查文件是否适合作为工具
        /// </summary>
        public static bool IsValidFileForTool(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                    return false;

                // 清理路径（移除引号等）
                filePath = CleanFilePath(filePath);

                // 只检查文件，不处理文件夹
                if (!File.Exists(filePath))
                    return false;

                // 检查文件扩展名，只支持exe和lnk文件
                var extension = Path.GetExtension(filePath).ToLowerInvariant();
                
                // 只允许exe和lnk文件
                return extension == ".exe" || extension == ".lnk";
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 清理文件路径（移除引号、空白字符等）
        /// </summary>
        public static string CleanFilePath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return filePath;

            // 移除前后空白字符
            filePath = filePath.Trim();

            // 移除前后引号
            if (filePath.StartsWith("\"") && filePath.EndsWith("\""))
            {
                filePath = filePath.Substring(1, filePath.Length - 2);
            }

            return filePath;
        }

        /// <summary>
        /// 检查是否为快捷方式文件
        /// </summary>
        public static bool IsShortcutFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return ShortcutExtensions.Contains(extension);
        }

        /// <summary>
        /// 检查是否为可执行文件
        /// </summary>
        public static bool IsExecutableFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return ExecutableExtensions.Contains(extension);
        }





        /// <summary>
        /// 创建安全的文件名（移除非法字符）
        /// </summary>
        public static string CreateSafeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "新工具";

            var invalidChars = Path.GetInvalidFileNameChars();
            var safeName = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());
            
            return string.IsNullOrWhiteSpace(safeName) ? "新工具" : safeName;
        }

        /// <summary>
        /// 显示拖拽效果提示
        /// </summary>
        public static DragDropEffects GetDragEffect(IDataObject data, DragDropEffects allowedEffects)
        {
            try
            {
                if (IsValidDragData(data))
                {
                    if ((allowedEffects & DragDropEffects.Copy) == DragDropEffects.Copy)
                        return DragDropEffects.Copy;
                    
                    if ((allowedEffects & DragDropEffects.Move) == DragDropEffects.Move)
                        return DragDropEffects.Move;
                    
                    if ((allowedEffects & DragDropEffects.Link) == DragDropEffects.Link)
                        return DragDropEffects.Link;
                }
                
                return DragDropEffects.None;
            }
            catch
            {
                return DragDropEffects.None;
            }
        }
    }
} 