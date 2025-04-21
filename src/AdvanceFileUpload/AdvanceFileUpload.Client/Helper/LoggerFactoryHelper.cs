using Microsoft.Extensions.Logging;

namespace AdvanceFileUpload.Client.Helper
{
    public static class LoggerFactoryHelper
    {
        private static ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
        });

        public static ILogger<T> CreateLogger<T>()
        {
            loggerFactory.AddFile("D:\\Temp\\log\\log.txt");
            return loggerFactory.CreateLogger<T>();
        }
    }
}
