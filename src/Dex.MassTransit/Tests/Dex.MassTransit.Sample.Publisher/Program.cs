﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Dex.MassTransit.Rabbit;
using Dex.MassTransit.Sample.Domain;
using Dex.MassTransit.Sample.Domain.Bus;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;

namespace Dex.MassTransit.Sample.Publisher
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Task.Run(() => CreatePublisherHostBuilder(args).Build().Run()).ContinueWith(CatchException);

            Console.WriteLine("Press Enter to exit");
            Console.ReadLine();

            static void CatchException(Task task)
            {
                if (task.IsFaulted)
                {
                    Debugger.Break();
                    Console.WriteLine(task.Exception);
                }
            }
        }

        private static void ConfigureRabbitMqOptions(RabbitMqOptions rabbitMqOptions)
        {
            //rabbitMqOptions.Port = 49158;
        }

        private static void ConfigureOtherRabbitMqOptions(OtherRabbitMqOptions otherRabbitMqOptions)
        {
            otherRabbitMqOptions.Host = "localhost";
            otherRabbitMqOptions.Port = 5672;
        }

        private static IHostBuilder CreatePublisherHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((_, services) =>
                {
                    services.AddLogging(builder =>
                    {
                        builder.AddOpenTelemetry(options => options.AddConsoleExporter());
#pragma warning disable CS0618 // Type or member is obsolete
                        builder.AddConsole(options => options.IncludeScopes = true);
#pragma warning restore CS0618 // Type or member is obsolete
                    });

                    // register services
                    services.Configure<RabbitMqOptions>(ConfigureRabbitMqOptions);
                    services.AddMassTransit(configurator =>
                    {
                        configurator.RegisterBus((context, _) =>
                        {
                            // send endpoint 
                            context.RegisterSendEndPoint<HelloMessageDto>();
                        });
                    });

                    services.Configure<OtherRabbitMqOptions>(ConfigureOtherRabbitMqOptions);
                    services.AddMassTransit<IOtherRabbitMqBus>(configurator =>
                    {
                        configurator.RegisterBus<OtherRabbitMqOptions>((context, _) =>
                        {
                            context.RegisterSendEndPoint<OtherRabbitMqOptions, OtherMessageDto>();
                        });
                    });

                    services.AddHostedService<HelloMessageGeneratorHostedService>();
                    // services.AddHostedService<OtherBusGeneratorHostedService>();
                });
    }
}