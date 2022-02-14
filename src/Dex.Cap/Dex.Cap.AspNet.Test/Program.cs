using Dex.Cap.Outbox.Ef;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Dex.Cap.AspNet.Test
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args)
                .AddOutboxMetrics(metrics => { metrics.WhereContext("Application"); })
                .Build()
                .Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
