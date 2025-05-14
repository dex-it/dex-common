using System.Diagnostics.CodeAnalysis;
using Dex.Cap.Common.Interfaces;

namespace Dex.Cap.OnceExecutor.Memory;

[SuppressMessage("Design", "CA1040:Не используйте пустые интерфейсы")]
public interface IOnceExecutorMemoryTransactionOptions : ITransactionOptions
{
}