using System;
using Automatica.Core.Cloud.EF.Helper;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

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
                host.Services.Migrate();

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
