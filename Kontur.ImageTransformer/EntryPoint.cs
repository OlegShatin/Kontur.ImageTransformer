using System;
using System.Net;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace Kontur.ImageTransformer
{
    public class EntryPoint
    {
        public static void Main(string[] args)
        {
            
            var config = new LoggingConfiguration();
            var fileTarget = new FileTarget();
            config.AddTarget("file", fileTarget);
            fileTarget.Layout = @"${date:format=HH\:mm\:ss} ${pad:padding=5:inner=${level:uppercase=true}} ${logger} Message: ${message}";
            fileTarget.FileName = @"${basedir}/logs/${date:format=dd-MM-yyyy}.log";
            var rule = new LoggingRule("*", LogLevel.Trace, fileTarget);
            config.LoggingRules.Add(rule);
            LogManager.Configuration = config;


            using (var server = new AsyncHttpServer())
            {
                server.Start();
                Console.ReadKey(true);
            }
            
        }
    }
}
