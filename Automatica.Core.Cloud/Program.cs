using System;
using System.IO;
using System.Reflection;
using Automatica.Core.Cloud.EF.Helper;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Cloud
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("starting AutomaticaCoreCloud...");
            try
            {
                var host = CreateWebHostBuilder(args).Build();
                host.Migrate();

                host.Run();

            }
            catch (Exception e)
            {
                Console.WriteLine($"{e}");
            }
            Console.WriteLine("stopping..");
        }


        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
            .UseKestrel(options =>
            {
                options.Limits.MaxRequestBodySize = null;
            });
    }
                // .UseUrls($"http://*:6000/");
}
