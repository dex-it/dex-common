using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Dex.Cap.OnceExecutor.AspNetScheduler;
using Dex.Cap.OnceExecutor.Ef.Extensions;
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

namespace Dex.Cap.AspNet.Test;

public class Startup(IConfiguration configuration)
{
    private IConfiguration Configuration { get; } = configuration;

    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web) {Converters = {new JsonStringEnumConverter()}};

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(lb =>
        {
            lb.ClearProviders();
            lb.AddConsole();
            lb.AddConfiguration(Configuration.GetSection("Logging"));
        });

        // configure resource
        const string serviceName = "dex.cap.aspnet.test";
        const string serviceVersion = "1.0";
        ResourceBuilder
            .CreateDefault()
            .AddService(serviceName: serviceName, serviceVersion: serviceVersion)
            .AddAttributes(new Dictionary<string, object>
            {
                ["environment.name"] = "test",
                ["team.name"] = "backend"
            });

        // add telemetry exporter
        services.AddOpenTelemetry()
            .ConfigureResource(builder =>
            {
                builder.AddService("outbox"); // sample to export
            }).WithMetrics(builder => builder.AddConsoleExporter());


        services.AddDbContext<TestDbContext>(builder => { builder.UseNpgsql(Configuration.GetConnectionString("DefaultConnection")); });

        services.AddOutbox<TestDbContext>();
        services.AddScoped<IOutboxMessageHandler<TestOutboxCommand>, TestCommandHandler>();
        services.RegisterOutboxScheduler(periodSeconds: 1);

        services.AddOnceExecutor<TestDbContext>();
        services.RegisterOnceExecutorScheduler(periodSeconds: 1);
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

        lifetime.ApplicationStarted.Register(Callback);

        return;

        async void Callback()
        {
            try
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
                                var client = scope2.ServiceProvider
                                    .GetRequiredService<IOutboxService>();
                                var db2 = scope2.ServiceProvider.GetRequiredService<TestDbContext>();
                                await client.EnqueueAsync(new TestOutboxCommand {Args = "hello world"});
                                await SaveChangesWithRetryAsync(db2);
                            }

                            await Task.Delay(500);
                        }
                    })
                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Debug.Fail(e.Message, e.StackTrace);
            }
        }

        async Task SaveChangesWithRetryAsync(DbContext db)
        {
            var attempt = 0;
            const int maxRetries = 10;
            const int delayMilliseconds = 100;

            while (attempt < maxRetries)
            {
                try
                {
                    await db.SaveChangesAsync();
                    break;
                }
                catch (Exception ex)
                {
                    attempt++;
                    Console.WriteLine($"SaveChangesAsync failed. Attempt {attempt} of {maxRetries}. Error: {ex.Message}");

                    // Ждём перед повторной попыткой
                    await Task.Delay(delayMilliseconds);
                }
            }
        }
    }

    private static Task HealthReportResponseWriter(HttpContext context, HealthReport report) => context
        .Response
        .WriteAsync(JsonSerializer.Serialize(new
        {
            report.Status,
            report.TotalDuration.TotalSeconds,
            Entities = report.Entries.Select(x => new
            {
                x.Key,
                x.Value.Status
            })
        }, JsonSerializerOptions));
}