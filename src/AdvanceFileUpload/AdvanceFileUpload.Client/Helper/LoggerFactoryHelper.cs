using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvanceFileUpload.Client.Helper
{
    public static class LoggerFactoryHelper
    {
        public static ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
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
