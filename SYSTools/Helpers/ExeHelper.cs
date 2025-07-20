using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;


namespace SYSTools.Helpers
{
    public class ExeHelper
    {
        string AppPath = Directory.GetCurrentDirectory();

        public bool FileExist(string Str_File)
        {
            return File.Exists(Str_File);
        }

        public void HandleMouseClick(string Tools_Path, string ToolName, string ExeName_x86, string ExeName_x64 = null)
        {
            var exeName = (Environment.Is64BitOperatingSystem && !string.IsNullOrWhiteSpace(ExeName_x64)) ? ExeName_x64 : ExeName_x86;
            string ExePath = Path.Combine(AppPath, Tools_Path, ToolName, exeName + ".exe");
            if (FileExist(ExePath))
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = ExePath,
                        UseShellExecute = true,
                        WorkingDirectory = Path.GetDirectoryName(ExePath) ?? string.Empty
                    };
                    Process.Start(psi);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ExePath);
                    iNKORE.UI.WPF.Modern.Controls.MessageBox.Show(
                        "请检查程序包内是否存在该工具, 或工具存放位置是否正确 \r\n 或检查杀毒软件是否拦截.",
                        "找不到工具启动文件",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show(ExePath);
                iNKORE.UI.WPF.Modern.Controls.MessageBox.Show(
                    "请检查程序包内是否存在该工具 \r\n 或工具存放位置是否正确",
                    "无法打开该工具",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
    }

}
