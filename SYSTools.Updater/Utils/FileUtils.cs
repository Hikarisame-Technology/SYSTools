using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using SYSTools.Updater.Services;

namespace SYSTools.Updater.Utils
{
    public static class FileUtils
    {
        private static readonly string[] ExcludedFolders = new[] { "Software Package", "backup_" };

        public static void CreateBackup(string sourcePath, string backupPath, ILogger logger)
        {
            if (!Directory.Exists(backupPath))
                Directory.CreateDirectory(backupPath);

            var files = Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories)
                               .Where(file => !ExcludedFolders.Any(excluded => 
                                   file.Contains(Path.DirectorySeparatorChar + excluded) || 
                                   file.StartsWith(excluded)));

            int totalFiles = files.Count();
            int processedFiles = 0;
            int lastReportedProgress = -1;

            foreach (string file in files)
            {
                try
                {
                    string relativePath = GetRelativePath(file, sourcePath);
                    string backupFile = Path.Combine(backupPath, relativePath);
                    string backupDir = Path.GetDirectoryName(backupFile);

                    if (!Directory.Exists(backupDir))
                        Directory.CreateDirectory(backupDir);

                    File.Copy(file, backupFile, true);
                    processedFiles++;
                    
                    double progress = (double)processedFiles / totalFiles * 100;
                    int currentProgress = (int)progress;
                    
                    // 只在进度变化超过10%时更新UI
                    if (currentProgress - lastReportedProgress >= 10)
                    {
                        logger.UpdateProgress(progress);
                        lastReportedProgress = currentProgress;
                    }
                }
                catch (Exception ex)
                {
                    logger.Log($"备份文件失败: {file}, 错误: {ex.Message}");
                }
            }
            
            logger.UpdateProgress(100);
            logger.Log($"备份完成: {processedFiles} 个文件");
        }

        public static void ExtractUpdate(string zipPath, string targetPath, ILogger logger)
        {
            using (var archive = ZipFile.OpenRead(zipPath))
            {
                int totalEntries = archive.Entries.Count;
                int processedEntries = 0;
                long totalSize = archive.Entries.Sum(entry => entry.Length);
                long processedSize = 0;

                // 批量更新UI，减少频率
                int lastReportedProgress = -1;
                int filesProcessedSinceLastLog = 0;

                foreach (var entry in archive.Entries)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(entry.Name))
                        {
                            string destinationPath = Path.Combine(targetPath, CleanPath(entry.FullName));
                            string destinationDir = Path.GetDirectoryName(destinationPath);

                            if (!Directory.Exists(destinationDir))
                                Directory.CreateDirectory(destinationDir);

                            if (File.Exists(destinationPath))
                            {
                                SmartDeleteFile(destinationPath);
                            }

                            entry.ExtractToFile(destinationPath, true);
                            
                            processedEntries++;
                            processedSize += entry.Length;
                            filesProcessedSinceLastLog++;
                            
                            double progress = ((double)processedSize / totalSize) * 100;
                            int currentProgress = (int)progress;
                            
                            // 只在进度变化超过5%或每处理10个文件时更新UI
                            if (currentProgress - lastReportedProgress >= 5 || filesProcessedSinceLastLog >= 10)
                            {
                                logger.UpdateProgress(progress);
                                logger.Log($"正在更新文件... ({processedEntries}/{totalEntries}) - {currentProgress}%");
                                lastReportedProgress = currentProgress;
                                filesProcessedSinceLastLog = 0;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Log($"更新文件失败: {entry.FullName}, 错误: {ex.Message}");
                    }
                }
                
                // 确保最终进度为100%
                logger.UpdateProgress(100);
                logger.Log($"文件解压完成: {processedEntries} 个文件");
            }
        }

        public static void CleanHistoryBackups(string targetPath, ILogger logger)
        {
            try
            {
                var backupFolders = Directory.GetDirectories(targetPath, "backup_*");
                foreach (var folder in backupFolders)
                {
                    try
                    {
                        Directory.Delete(folder, true);
                        logger.Log($"清理历史备份: {Path.GetFileName(folder)}");
                    }
                    catch (Exception ex)
                    {
                        logger.Log($"清理历史备份失败: {Path.GetFileName(folder)}, 错误: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Log($"查找历史备份失败: {ex.Message}");
            }
        }

        private static void SmartDeleteFile(string path)
        {
            try
            {
                // 先尝试简单删除
                if (File.Exists(path))
                {
                    File.Delete(path);
                    return;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FileUtils] SmartDeleteFile 简单删除失败: {ex.Message}");
            }

            try
            {
                // 尝试设置文件属性后删除
                if (File.Exists(path))
                {
                    File.SetAttributes(path, FileAttributes.Normal);
                    File.Delete(path);
                    return;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FileUtils] SmartDeleteFile 属性+删除失败: {ex.Message}");
            }

            try
            {
                // 最后的手段：尝试查找并结束占用进程
                if (File.Exists(path))
                {
                    KillProcessesUsingFile(path);
                    System.Threading.Thread.Sleep(100);
                    File.Delete(path);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FileUtils] SmartDeleteFile 终极删除失败: {ex.Message}");
            }
        }

        private static void KillProcessesUsingFile(string filePath)
        {
            try
            {
                string fileName = Path.GetFileName(filePath);
                var processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(fileName));
                
                foreach (var process in processes)
                {
                    try
                    {
                        if (!process.HasExited && process.Id != Process.GetCurrentProcess().Id)
                        {
                            process.Kill();
                            process.WaitForExit(1000);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[FileUtils] 结束进程失败 ({process?.ProcessName ?? "unknown"}): {ex.Message}");
                    }
                    finally
                    {
                        try
                        {
                            process?.Dispose();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[FileUtils] 释放进程资源失败: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FileUtils] KillProcessesUsingFile 查找/处理进程失败: {ex.Message}");
            }
        }

        public static string CleanPath(string path)
        {
            string invalidChars = Regex.Escape(new string(Path.GetInvalidPathChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);
            return Regex.Replace(path, invalidRegStr, "_");
        }

        private static string GetRelativePath(string fullPath, string basePath)
        {
            try
            {
                if (!basePath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                    basePath += Path.DirectorySeparatorChar;

                Uri baseUri = new Uri(basePath);
                Uri fullUri = new Uri(fullPath);
                Uri relativeUri = baseUri.MakeRelativeUri(fullUri);

                return Uri.UnescapeDataString(relativeUri.ToString().Replace('/', Path.DirectorySeparatorChar));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FileUtils] GetRelativePath 失败: {ex.Message}");
                return fullPath.Substring(basePath.Length).TrimStart(Path.DirectorySeparatorChar);
            }
        }
    }
}