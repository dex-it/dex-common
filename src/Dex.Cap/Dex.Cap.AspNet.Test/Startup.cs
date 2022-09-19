using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.ConsoleTest;
using Dex.Cap.Outbox.AspNetScheduler;
using Dex.Cap.Outbox.Ef;
using Dex.Cap.Outbox.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dex.Cap.AspNet.Test
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(lb =>
            {
                lb.ClearProviders();
                lb.AddConsole();
                lb.AddConfiguration(Configuration.GetSection("Logging"));
            });

            services.AddDbContext<TestDbContext>(builder => { builder.UseNpgsql(Configuration.GetConnectionString("DefaultConnection")); });

            services.AddOutbox<TestDbContext>();
            services.AddScoped<IOutboxMessageHandler<TestOutboxCommand>, TestCommandHandler>();
            services.RegisterOutboxScheduler(periodSeconds: 30, cleanupDays: 30);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime lifetime)
        {
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints
                    .MapHealthChecks("/health", new HealthCheckOptions
                    {
                        ResponseWriter = HealthReportResponseWriter
                    });
            });


            lifetime.ApplicationStarted.Register(async () =>
            {
                using var scope = app.ApplicationServices.CreateScope();
                var client = scope.ServiceProvider.GetRequiredService<IOutboxService>();
                await client.EnqueueAsync(new TestOutboxCommand { Args = "hello world" }, CancellationToken.None);

                var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
                await db.Database.EnsureCreatedAsync();
                await db.SaveChangesAsync();
            });
        }

        private static async Task HealthReportResponseWriter(HttpContext context, HealthReport report)
        {
            var jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                report.Status,
                report.TotalDuration.TotalSeconds,
                Entities = report.Entries.Select(x => new
                {
                    x.Key,
                    x.Value.Status
                })
            },
                jsonSerializerOptions));
        }
    }
}