using System;
using System.IO;
using System.Net;
using Kontur.ImageTransformer.Controllers;
using NLog;
using NLog.Config;
using NLog.Targets;
using SAEAHTTPD;

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
            var rule = new LoggingRule("*", LogLevel.Info, fileTarget);
            config.LoggingRules.Add(rule);
            LogManager.Configuration = config;


            HttpServer server = new HttpServer(10, 20, 100 * 1024);
            server.OnHttpRequest += server_OnHttpRequest;
            server.Start(new IPEndPoint(IPAddress.Any, 8080));
            

        }
        public static void server_OnHttpRequest(object sender, HttpRequestArgs e)
        {
            var controller = new ImageController(e.Request, e.Response);
            using (e.Response.OutputStream)
            {
                controller.HandleRequest();
            }
        }
        static void server_OnHttpRequestOK(object sender, HttpRequestArgs e)
        {
            e.Response.Status = SAEAHTTPD.HttpStatusCode.OK;
            e.Response.ReasonPhrase = "OK";
            using (TextWriter writer = new StreamWriter(e.Response.OutputStream))
            {
                writer.Write("Hello, world!");
            }
        }
    }
}
