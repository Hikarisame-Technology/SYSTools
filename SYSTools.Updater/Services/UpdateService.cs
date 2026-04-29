using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using SYSTools.Updater.Utils;

namespace SYSTools.Updater.Services
{
    public class UpdateService
    {
        private readonly ILogger _logger;
        private readonly string _zipPath;
        private readonly string _targetPath;
        private readonly bool _isToolkitUpdate;
        private string _backupPath;

        public UpdateService(string zipPath, string targetPath, bool isToolkitUpdate, ILogger logger)
        {
            _zipPath = zipPath;
            _targetPath = targetPath;
            _isToolkitUpdate = isToolkitUpdate;
            _logger = logger;
        }

        public async Task StartUpdate()
        {
            try
            {
                ValidateParameters();
                await WaitForMainProcess();
                await CreateBackup();
                await ExtractUpdate();
                await CleanupAndStart();
            }
            catch (Exception ex)
            {
                _logger.LogError($"更新失败: {ex.Message}");
                File.WriteAllText("update_error.log", ex.ToString());
                throw;
            }
        }

        private void ValidateParameters()
        {
            if (!File.Exists(_zipPath))
            {
                throw new FileNotFoundException("更新包文件不存在");
            }

            if (!Directory.Exists(_targetPath))
            {
                throw new DirectoryNotFoundException("目标目录不存在");
            }
        }

        private async Task WaitForMainProcess()
        {
            if (!_isToolkitUpdate)
            {
                _logger.UpdateStatus("等待主程序退出...");
                await Task.Delay(1000);
                foreach (var process in Process.GetProcessesByName("SYSTools"))
                {
                    process.WaitForExit();
                }
            }
        }

        private async Task CreateBackup()
        {
            if (_isToolkitUpdate)
            {
                _logger.Log("工具包更新，跳过备份");
                return;
            }

            _logger.UpdateStatus("正在创建备份...");
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            _backupPath = Path.Combine(_targetPath, $"backup_{timestamp}");
            _logger.Log($"创建备份: {_backupPath}");

            await Task.Run(() => FileUtils.CreateBackup(_targetPath, _backupPath, _logger));
        }

        private async Task ExtractUpdate()
        {
            _logger.UpdateStatus("正在更新文件...");
            await Task.Run(() => FileUtils.ExtractUpdate(_zipPath, _targetPath, _logger));
        }

        private async Task CleanupAndStart()
        {
            _logger.UpdateStatus("正在清理临时文件...");
            try
            {
                File.Delete(_zipPath);
                _logger.Log("已删除临时文件");

                if (!_isToolkitUpdate)
                {
                    await StartMainProgram();
                }
                else
                {
                    _logger.UpdateStatus("工具包更新完成");
                    _logger.Log("工具包更新成功");
                    await Task.Delay(2000);
                }

                await Task.Delay(1000);
            }
            catch (Exception ex)
            {
                throw new Exception($"清理过程出错: {ex.Message}");
            }
        }

        private async Task StartMainProgram()
        {
            string mainExe = Path.Combine(_targetPath, "SYSTools.exe");
            _logger.UpdateStatus("正在启动程序...");

            try
            {
                var mainProcess = Process.Start(new ProcessStartInfo
                {
                    FileName = mainExe,
                    UseShellExecute = true
                });

                if (mainProcess != null)
                {
                    await Task.Delay(3000);
                    if (!mainProcess.HasExited)
                    {
                        await CleanupBackups();
                    }
                    else
                    {
                        throw new Exception("程序启动失败，保留备份文件");
                    }
                }
                else
                {
                    throw new Exception("程序启动失败，保留备份文件");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"启动程序失败: {ex.Message}");
            }
        }

        private async Task CleanupBackups()
        {
            _logger.UpdateStatus("正在清理备份...");
            try
            {
                if (Directory.Exists(_backupPath))
                {
                    Directory.Delete(_backupPath, true);
                    _logger.Log("当前备份文件清理完成");
                }

                _logger.UpdateStatus("正在清理历史备份...");
                FileUtils.CleanHistoryBackups(_targetPath, _logger);
            }
            catch (Exception ex)
            {
                _logger.Log($"清理备份失败: {ex.Message}");
            }
        }
    }
} 