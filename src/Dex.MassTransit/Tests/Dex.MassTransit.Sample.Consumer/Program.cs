using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Dex.MassTransit.Rabbit;
using Dex.MassTransit.Sample.Domain;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;

namespace Dex.MassTransit.Sample.Consumer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Task.Run(() => CreateConsumerHostBuilder(args).Build().Run()).ContinueWith(CatchException);

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
            rabbitMqOptions.Password = "incorrect"; // умышленно ломаем пароль
            //rabbitMqOptions.VHost = "incorrect";
        }

        private static IHostBuilder CreateConsumerHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddLogging(builder =>
                    {
                        builder.AddOpenTelemetry(options => options
                            .AddConsoleExporter());
                    });

                    // register services
                    services.Configure<RabbitMqOptions>(ConfigureRabbitMqOptions);
                    services.AddSingleton<MassTransitTelemetryLogger>();

                    services.AddSingleton<ITestPasswordService, TestPasswordService>();

                    services.AddMassTransit(configurator =>
                    {
                        
                        //configurator.AddConsumer<HelloConsumer>();
                        configurator.AddConsumer<HelloConsumer2>();

                        configurator.RegisterBus((context, factoryConfigurator) =>
                        {
                            factoryConfigurator.ConfigureJsonSerializerOptions(options => 
                            {
                                // var optionsConverter = options.Converters.Where(c=> c is SystemTextJsonUriConverter).SingleOrDefault();
                                // options.Converters.Remove(optionsConverter);

                                return options;
                            });
                            // recieve endpoint
                            //context.RegisterReceiveEndpoint<HelloConsumer, HelloMessageDto>(factoryConfigurator, createSeparateQueue: true);
                            context.RegisterReceiveEndpoint<HelloConsumer2, HelloMessageDto>(factoryConfigurator, createSeparateQueue: true);
                        }, refreshConnectCallback: context =>
                        {
                            var testPasswordService = context.GetRequiredService<ITestPasswordService>();
                            return async factory =>
                            {
                                //factory.VirtualHost = "winlineClub";
                                factory.Password = await testPasswordService.GetAccessToken();
                            };
                        });
                    });

                    services.AddHostedService<MetricTraceExporterHostedService>();
                });
    }
}