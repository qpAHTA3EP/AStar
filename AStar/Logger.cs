using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AStar
{
    internal static class AStarLogger
    {
        public static readonly string LogPath = @".\Logs\AStar\"; 

        private static StringBuilder LogCache = new StringBuilder(1024);
        internal static string LogFilePath = string.Empty;

        private static CancellationTokenSource cancellationTokenSource;
        private static CancellationToken cancellationToken;
        private static Task backgroundLoggerTask;

        static AStarLogger()
        {
#if DEBUG || DEBUG_LOG
            Start(); 
#endif
        }

        internal static bool Active => backgroundLoggerTask?.Status == TaskStatus.Running;

        internal static void Start()
        {
            if (backgroundLoggerTask?.Status != TaskStatus.Running)
            {
                string dir = string.Concat(Astral.Controllers.Directories.LogsPath, '\\', nameof(AStar), '\\');
                LogFilePath = string.Concat(dir, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"), ".log");
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                cancellationTokenSource = new CancellationTokenSource();
                cancellationToken = cancellationTokenSource.Token;
                backgroundLoggerTask = Task.Factory.StartNew(Run, cancellationToken);
            }
        }

        public static void Stop()
        {
            cancellationTokenSource.Cancel();
        }

        public static void WriteLine(string Text, bool toAstral = false)
        {
            if(Active)
                WriteLine(LogType.Log, Text, toAstral);
        }

        public static void WriteLine(LogType logType, string Text, bool toAstral = false)
        {
            if (Active)
            {
                try
                {
                    LogCache.Append(DateTime.Now.ToString(@"[HH-mm-ss.ffffff]"));
                    switch(logType)
                    {
                        case LogType.Log:
                            LogCache.Append(" [LOG] ");
                            if (toAstral)
                                Astral.Logger.WriteLine(Astral.Logger.LogType.Log, Text);
                            break;
                        case LogType.Debug:
                            LogCache.Append(" [DBG] ");
                            if (toAstral)
                                Astral.Logger.WriteLine(Astral.Logger.LogType.Debug, Text);
                            break;
                        case LogType.Error:
                            LogCache.Append(" [ERR] ");
                            if (toAstral)
                                Astral.Logger.WriteLine(Astral.Logger.LogType.Debug, Text);
                            break;
                    }
                    LogCache.AppendLine(Text);
                }
                catch { }
            }
        }

        private static void Run()
        {
            if (!string.IsNullOrEmpty(LogFilePath))
            {
                while (Active && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        if (LogCache.Length > 0)
                        {
                            File.AppendAllText(LogFilePath, LogCache.ToString());
                            LogCache.Clear();
                        }
                    }
                    catch (Exception ex)
                    {
                        Astral.Logger.WriteLine(Astral.Logger.LogType.Debug, ex.ToString());
                    }
                    Thread.Sleep(200);
                }
            }
        }
    }

    internal enum LogType
    {
        /// <summary>
        /// Обычное сообщение
        /// </summary>
        Log,
        /// <summary>
        /// Отладочное сообщение
        /// </summary>
        Debug,
        /// <summary>
        /// Сообщение об ошибке
        /// </summary>
        Error
    }
}
