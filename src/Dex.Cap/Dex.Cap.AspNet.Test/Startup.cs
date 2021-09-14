using System.Threading;
using Dex.Cap.ConsoleTest;
using Dex.Cap.Outbox.Ef;
using Dex.Cap.Outbox.Interfaces;
using Dex.Cap.Outbox.Scheduler;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
            services.AddDbContext<TestDbContext>(builder =>
            {
                builder.UseNpgsql(Configuration.GetConnectionString("DefaultConnection"));
            });

            services.AddOutbox<TestDbContext>();
            services.AddScoped<IOutboxMessageHandler<TestOutboxCommand>, TestCommandHandler>();
            services.RegisterOutboxScheduler(periodSeconds: 30, cleanupDays: 30);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime lifetime)
        {
            lifetime.ApplicationStarted.Register(async () => 
            {
                using var scope = app.ApplicationServices.CreateScope();
                var client = scope.ServiceProvider.GetRequiredService<IOutboxService>();
                await client.EnqueueAsync(new TestOutboxCommand { Args = "hello world" }, CancellationToken.None);

                var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
                db.Database.EnsureCreated();
                await db.SaveChangesAsync();
            });
        }
    }
}
