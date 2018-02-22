using System;
using System.Net;
using System.Threading.Tasks;
using Aardwolf;
using Kontur.ImageTransformer.Controllers;
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
            fileTarget.Layout =
                @"${date:format=HH\:mm\:ss} ${pad:padding=5:inner=${level:uppercase=true}} ${logger} Message: ${message}";
            fileTarget.FileName = @"${basedir}/logs/${date:format=dd-MM-yyyy}.log";
            var rule = new LoggingRule("*", LogLevel.Trace, fileTarget);
            config.LoggingRules.Add(rule);
            LogManager.Configuration = config;


            /*var server = new AsyncHttpServer();
            server.Run("http://+:8080/");
            Console.ReadKey(true);*/

            var host = new HttpAsyncHost(new Handler(), 4);
            host.Run("http://+:8080/");
        }
    }

    class Handler : IHttpAsyncHandler
    {
        public Task<IHttpResponseAction> Execute(IHttpRequestContext state)
        {
            return Task.Run<IHttpResponseAction>(() =>  new Resp());
        }

        class Resp : IHttpResponseAction
        {
            public Task Execute(IHttpRequestResponseContext context)
            {
                
                return Task.Run(() => context.Response.StatusCode = 200);
                
            }
        }
    }
}