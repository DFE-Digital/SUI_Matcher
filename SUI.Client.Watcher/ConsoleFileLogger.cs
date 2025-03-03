//namespace SUI.Client.Watcher;

//public class ConsoleFileLogger : ILogger
//{
//    private readonly TextWriter _output;
//    private readonly AppConfig _config;

//    public ConsoleFileLogger(TextWriter output, AppConfig config)
//    {
//        _output = output;
//        _config = config;
//    }

//    public void Log(string message)
//    {
//        string logFileName = Path.Combine(_config.LogDirectory, $"log-{DateTime.Now:yyyy-MM-dd}.txt");
//        string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
//        _output.WriteLine(logEntry);
//        File.AppendAllText(logFileName, logEntry + Environment.NewLine);
//    }
//}
