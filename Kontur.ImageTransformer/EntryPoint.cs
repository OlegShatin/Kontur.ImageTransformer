using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using NHttp;
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


            /*using (var server = new AsyncHttpServer())
            {
                server.Start("http://+:8080/");
                Console.ReadKey(true);
            }*/
            using (var server = new HttpServer())
            {
                server.EndPoint = new IPEndPoint(IPAddress.Any, 8080);
                server.RequestReceived += (s, e) =>
                {
                    
                    using (var writer = new StreamWriter(e.Response.OutputStream))
                    {
                        e.Response.StatusCode = 200;
                        writer.Write("Hello world!");
                    }
                };

                server.Start();


                Process.Start(String.Format("http://{0}/", server.EndPoint));

                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }

        }
    }
}
