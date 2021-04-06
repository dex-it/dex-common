using System;
using System.Threading;
using System.Threading.Tasks;
using GreenPipes;
using MassTransit;
using Microsoft.Extensions.Hosting;

namespace Dex.MassTransit.Extensions
{
    public class MassTransitSender : IMassTransitSender
    {
        private readonly IHostApplicationLifetime _lifetime;
        private readonly ISendEndpointProvider _sendEndpointProvider;
        private readonly IPublishEndpoint _publishEndpoint;

        // TODO
        private ILogId _logId;

        public MassTransitSender(IHostApplicationLifetime lifetime, ISendEndpointProvider sendEndpointProvider, IPublishEndpoint publishEndpoint)
        {
            _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
            _sendEndpointProvider = sendEndpointProvider ?? throw new ArgumentNullException(nameof(sendEndpointProvider));
            _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
        }

        // send
        public Task Send<T>(T message) where T : class
        {
            return Send(message, CreateDefaultCancellationToken());
        }

        public Task Send<T>(T message, CancellationToken cancellationToken) where T : class
        {
            return Send((object) message, cancellationToken);
        }

        public Task Send(object message)
        {
            return Send(message, CreateDefaultCancellationToken());
        }

        public Task Send(object message, CancellationToken cancellationToken)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            return _sendEndpointProvider.Send(message,
                Pipe.Execute<SendContext>(c => c.ConversationId = _logId.LogId), cancellationToken);
        }

        // publish
        public Task Publish<T>(T message) where T : class
        {
            return Publish(message, CreateDefaultCancellationToken());
        }

        public Task Publish<T>(T message, CancellationToken cancellationToken) where T : class
        {
            return Publish((object) message, cancellationToken);
        }

        public Task Publish(object message)
        {
            return Publish(message, CreateDefaultCancellationToken());
        }

        public Task Publish(object message, CancellationToken cancellationToken)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            return _publishEndpoint.Publish(message,
                Pipe.Execute<PublishContext>(c => c.ConversationId = _logId.LogId), cancellationToken);
        }

        private CancellationToken CreateDefaultCancellationToken()
        {
            return CancellationTokenSource.CreateLinkedTokenSource(_lifetime.ApplicationStopping, _lifetime.ApplicationStopped).Token;
        }
    }
}