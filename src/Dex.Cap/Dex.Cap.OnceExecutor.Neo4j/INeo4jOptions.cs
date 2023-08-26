using System.Diagnostics.CodeAnalysis;

namespace Dex.Cap.OnceExecutor.Neo4j;

[SuppressMessage("Design", "CA1040:Не используйте пустые интерфейсы")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public interface INeo4jOptions : IOnceExecutorOptions
{
}