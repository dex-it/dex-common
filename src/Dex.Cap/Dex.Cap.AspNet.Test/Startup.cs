using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Dex.Cap.Outbox.AspNetScheduler;
using Dex.Cap.Outbox.Ef.Extensions;
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
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

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

            // configure resource
            var serviceName = "dex.cap.aspnet.test";
            var serviceVersion = "1.0";
            var resourceBuilder =
                ResourceBuilder
                    .CreateDefault()
                    .AddService(serviceName: serviceName, serviceVersion: serviceVersion)
                    .AddAttributes(new Dictionary<string, object>
                    {
                        ["environment.name"] = "test",
                        ["team.name"] = "backend"
                    });

            // add telemetry exporter
            services.AddOpenTelemetryMetrics(builder =>
            {
                builder.SetResourceBuilder(resourceBuilder);
                builder.AddConsoleExporter();
                builder.AddMeter("outbox"); // sample to export
            });

            services.AddDbContext<TestDbContext>(builder => { builder.UseNpgsql(Configuration.GetConnectionString("DefaultConnection")); });

            services.AddOutbox<TestDbContext>();
            services.AddScoped<IOutboxMessageHandler<TestOutboxCommand>, TestCommandHandler>();
            services.RegisterOutboxScheduler(periodSeconds: 1);
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

            async void Callback()
            {
                using var scope = app.ApplicationServices.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
                await db.Database.EnsureDeletedAsync();
                await db.Database.EnsureCreatedAsync();

                await Task.Run(async () =>
                    {
                        var iter = 10;
                        while (!lifetime.ApplicationStopping.IsCancellationRequested && iter-- > 0)
                        {
                            using var scope2 = app.ApplicationServices.CreateScope();
                            for (var i = 0; i < 10; i++)
                            {
                                var client = scope2.ServiceProvider.GetRequiredService<IOutboxService<TestDbContext>>();
                                var db2 = scope2.ServiceProvider.GetRequiredService<TestDbContext>();
                                await client.EnqueueAsync(Guid.NewGuid(), new TestOutboxCommand { Args = "hello world" });
                                await db2.SaveChangesAsync();
                            }

                            await Task.Delay(500);
                        }
                    })
                    .ConfigureAwait(false);
            }

            lifetime.ApplicationStarted.Register(Callback);
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