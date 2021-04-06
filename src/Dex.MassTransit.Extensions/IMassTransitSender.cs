using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Dex.MassTransit.Extensions
{
    public interface IMassTransitSender
    {
        Task Send<T>(T message) where T : class;
        Task Send([NotNull] object message);
        Task Send([NotNull] object message, CancellationToken cancellationToken);
        Task Send<T>([NotNull] T message, CancellationToken cancellationToken) where T : class;
        Task Publish<T>([NotNull] T message, CancellationToken cancellationToken) where T : class;
        Task Publish<T>(T message) where T : class;
        Task Publish([NotNull] object message);
        Task Publish([NotNull] object message, CancellationToken cancellationToken);
    }
}