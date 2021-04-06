using System;
using System.Threading.Tasks;
using Dex.MassTransit.Extensions;
using Dex.MassTransit.Extensions.Options;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Dex.MassTransit.Sample.Test
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Task.Run(() => CreatePublisherHostBuilder(args).Build().Run()).ContinueWith(CatchException);
            Task.Run(() => CreateConsumerHostBuilder(args).Build().Run()).ContinueWith(CatchException);

            Console.WriteLine("Press Enter to exit");
            Console.ReadLine();

            void CatchException(Task task)
            {
                if (task.IsFaulted)
                {
                    Console.WriteLine(task.Exception);
                }
            }
        }

        private static void ConfigureRabbitMqOptions(RabbitMqOptions rabbitMqOptions)
        {
            rabbitMqOptions.Port = 49158;
        }
        
        private static IHostBuilder CreatePublisherHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    // register services

                    services.Configure<RabbitMqOptions>(ConfigureRabbitMqOptions);

                    services.AddMassTransit(configurator =>
                    {
                        configurator.RegisterBus((context, factoryConfigurator) =>
                        {
                            // send endpoint 
                            context.RegisterSendEndPoint<HelloMessage>();
                        });
                    });

                    services.AddMassTransitHostedService();
                    services.AddHostedService<HelloMessageGenerator>();
                });

        private static IHostBuilder CreateConsumerHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    // register services

                    services.Configure<RabbitMqOptions>(ConfigureRabbitMqOptions);

                    services.AddMassTransit(configurator =>
                    {
                        configurator.AddConsumer<HelloConsumer>();

                        configurator.RegisterBus((context, factoryConfigurator) =>
                        {
                            // recieve endpoint
                            context.RegisterReceiveEndpoint<HelloConsumer, HelloMessage>(factoryConfigurator);
                        });
                    });

                    services.AddMassTransitHostedService();
                });
    }
}