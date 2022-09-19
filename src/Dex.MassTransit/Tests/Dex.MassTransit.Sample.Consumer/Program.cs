using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Dex.MassTransit.Rabbit;
using Dex.MassTransit.Sample.Consumer.Consumers;
using Dex.MassTransit.Sample.Domain;
using Dex.MassTransit.Sample.Domain.Bus;
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
        
        private static void ConfigureOtherRabbitMqOptions(OtherRabbitMqOptions otherRabbitMqOptions)
        {
            otherRabbitMqOptions.Host = "localhost";
            otherRabbitMqOptions.Port = 5673;
            otherRabbitMqOptions.VHost = "winlineClub";
        }

        private static IHostBuilder CreateConsumerHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((_, services) =>
                {
                    services.AddLogging(builder =>
                    {
                        builder.AddOpenTelemetry(options => options
                            .AddConsoleExporter());
                    });

                    // register services
                    //services.Configure<RabbitMqOptions>(ConfigureRabbitMqOptions);
                    services.Configure<OtherRabbitMqOptions>(ConfigureOtherRabbitMqOptions);
                    services.AddSingleton<MassTransitTelemetryLogger>();

                    services.AddSingleton<ITestPasswordService, TestPasswordService>();

                    services.AddMassTransit(configurator =>
                    {
                        //configurator.AddConsumer<HelloConsumer>();
                        configurator.AddConsumer<HelloConsumer2>();
                    
                        configurator.RegisterBus((context, factoryConfigurator) =>
                        {
                            // recieve endpoint
                            //context.RegisterReceiveEndpoint<HelloConsumer, HelloMessageDto>(factoryConfigurator, createSeparateQueue: true);
                            context.RegisterReceiveEndpoint<HelloConsumer2, HelloMessageDto>(factoryConfigurator);
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
                    
                    services.AddMassTransit<IOtherRabbitMqBus>(configurator =>
                    {
                        configurator.AddConsumer<OtherConsumer>();
                        
                        configurator.RegisterBus<OtherRabbitMqOptions>((context, factoryConfig) =>
                        {
                            context.RegisterReceiveEndpoint<OtherConsumer, OtherMessageDto, OtherRabbitMqOptions>(factoryConfig);
                        });
                    });

                    services.AddHostedService<MetricTraceExporterHostedService>();
                });
    }
}