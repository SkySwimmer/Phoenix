using System.Diagnostics;
using System.Text;

namespace Phoenix.Common.Logging.Impl
{
    public class FileLoggerImpl : Logger, ILoggerImplementationProvider
    {
        private string Source;
        private static StreamWriter FileWriter;

        public FileLoggerImpl() : this(null) { }
        public FileLoggerImpl(string source) {
            Source = source;

            if (FileWriter == null) {
                // Create log folder
                Directory.CreateDirectory("Logs");

                // Create log file
                FileWriter = new StreamWriter("Logs/" + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + ".log", true);
            }
        }

        public Logger CreateInstance(string name)
        {
            return new FileLoggerImpl(name);
        }

        private LogLevel level = LogLevel.GLOBAL;
        public override LogLevel Level { get => (level == LogLevel.GLOBAL ? Logger.GlobalLogLevel : level); set => level = value; }

        public override void Log(LogLevel level, string message)
        {
            if (Level != LogLevel.QUIET && Level >= level) {
                lock(FileWriter)
                {
                    string msg = "[" + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToString("HH:mm:ss") + "] [" + Source + "] [" + level.ToString() + "] " + GlobalMessagePrefix + message;
                    FileWriter.WriteLine(msg);
                    FileWriter.Flush();
                }
            }
        }

        public override void Log(LogLevel level, string message, Exception exception)
        {
            if (Level != LogLevel.QUIET && Level >= level) {
                string msg = "[" + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToString("HH:mm:ss") + "] [" + Source + "] [" + level.ToString() + "] " + GlobalMessagePrefix + message;
                FileWriter.WriteLine(msg);
                FileWriter.WriteLine("Exception: " + exception.GetType().FullName + (exception.Message != null ? ": " + exception.Message : ""));
                Exception? e = exception.InnerException;
                while (e != null)
                {
                    FileWriter.WriteLine("Caused by: " + exception.GetType().FullName + (e.Message != null ? ": " + e.Message : ""));
                    e = e.InnerException;
                }
                FileWriter.WriteLine(exception.StackTrace);
                FileWriter.Flush();
            }
        }
    }
}
