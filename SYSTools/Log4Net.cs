using log4net;
using log4net.Appender;
using System;
using System.IO;
public class SYSToolsLog
{
    private static string filepath = AppDomain.CurrentDomain.BaseDirectory + @"\Log\";

    private static readonly ILog logComm = LogManager.GetLogger("SYSToolsLog");

    static SYSToolsLog()
    {
        log4net.Config.XmlConfigurator.Configure(new FileInfo("Log4Net.config"));

        if (!Directory.Exists(filepath))
        {
            Directory.CreateDirectory(filepath);
        }
    }

    private static void WriteLog(string msg, bool isWrite, Action<object> action)
    {
        if (isWrite)
        {
            string filename = $"SYSToolsLog_{action.Method.Name}_{DateTime.Now.ToString("yyyy/MM/dd HH-mm-ss")}.log";
            var repository = LogManager.GetRepository();

            #region MyRegion
            var appenders = repository.GetAppenders();
            if (appenders.Length > 0)
            {
                RollingFileAppender targetApder = null;
                foreach (var Apder in appenders)
                {
                    if (Apder.Name == "SYSToolsLog")
                    {
                        targetApder = Apder as RollingFileAppender;
                        break;
                    }
                }
                if (targetApder.Name == "SYSToolsLog")//如果是文件输出类型日志，则更改输出路径
                {
                    if (targetApder != null)
                    {
                        if (!targetApder.File.Contains(filename))
                        {
                            targetApder.File = @"Log\" + filename;
                            targetApder.ActivateOptions();
                        }
                    }
                }
            }

            #endregion
            action(msg);
        }
    }
    public static void LogError(string msg, bool isWrite)
    {
        WriteLog(msg, isWrite, logComm.Error);
    }
    public static void LogInfo(string msg, bool isWrite)
    {
        WriteLog(msg, isWrite, logComm.Info);
    }
    public static void LogWarn(string msg, bool isWrite)
    {
        WriteLog(msg, isWrite, logComm.Warn);
    }
}