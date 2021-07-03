using System;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Dex.Cap.Outbox.QuartzHandler
{
    public static class MicrosoftDependencyInjectionExtensions
    {
        public static void RegisterOutboxInQuartz(this IServiceCollection services, int periodSeconds = 30)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            services.AddQuartz(q =>
            {
                q.UseMicrosoftDependencyInjectionJobFactory();
                q.UseInMemoryStore();

                // job OutboxJob
                q.AddJob<OutboxHandlerJob>(c =>
                {
                    c.StoreDurably();
                    c.RequestRecovery();
                });
                q.ScheduleJob<OutboxHandlerJob>(c =>
                {
                    c.StartNow()
                        .WithSimpleSchedule(b => { b.WithInterval(TimeSpan.FromSeconds(periodSeconds)).RepeatForever(); });
                });
            });
            services.AddQuartzServer(options =>
            {
                // when shutting down we want jobs to complete gracefully
                options.WaitForJobsToComplete = true;
            });
        }
    }
}