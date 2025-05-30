using System.Diagnostics.CodeAnalysis;
using Dex.Cap.Common.Interfaces;

namespace Dex.Cap.Outbox.Neo4j;

[SuppressMessage("Design", "CA1040:Не используйте пустые интерфейсы")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public interface INeo4jTransactionOptions : ITransactionOptions
{
}