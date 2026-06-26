using System;
using System.IO;
using System.Text;
using System.Threading;

namespace smartClass.Services
{
    /// <summary>
    /// 线程安全的文件日志服务。日志写入 app 目录下的 error.log，超过 1MB 自动轮转。
    /// </summary>
    public static class LogService
    {
        private static readonly string LogFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log");
        private static readonly string LogFileOld = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.old.log");
        private static readonly object _lock = new object();
        private const long MaxFileSize = 1_048_576; // 1 MB

        public static void Log(string message)
        {
            try
            {
                var log = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}\r\n";
                lock (_lock)
                {
                    EnsureDirectory();
                    RotateIfNeeded();
                    File.AppendAllText(LogFile, log, Encoding.UTF8);
                }
            }
            catch
            {
                // 日志记录本身失败不应影响主程序
            }
        }

        public static void Log(Exception ex, string context = null)
        {
            var msg = context == null
                ? $"异常: {ex}"
                : $"[{context}] 异常: {ex}";
            Log(msg);
        }

        /// <summary>
        /// 记录错误并返回给调用方用于 UI 提示
        /// </summary>
        public static void LogError(string context, string detail = null)
        {
            var msg = detail == null
                ? $"[错误] {context}"
                : $"[错误] {context}: {detail}";
            Log(msg);
        }

        private static void EnsureDirectory()
        {
            var dir = Path.GetDirectoryName(LogFile);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        private static void RotateIfNeeded()
        {
            try
            {
                if (File.Exists(LogFile))
                {
                    var info = new FileInfo(LogFile);
                    if (info.Length > MaxFileSize)
                    {
                        if (File.Exists(LogFileOld))
                            File.Delete(LogFileOld);
                        File.Move(LogFile, LogFileOld);
                    }
                }
            }
            catch
            {
                // 轮转失败不阻止写入
            }
        }
    }
}
