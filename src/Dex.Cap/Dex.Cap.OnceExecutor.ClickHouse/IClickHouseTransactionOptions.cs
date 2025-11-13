using System.Diagnostics.CodeAnalysis;
using Dex.Cap.Common.Interfaces;

namespace Dex.Cap.OnceExecutor.ClickHouse;

[SuppressMessage("Design", "CA1040:Не используйте пустые интерфейсы")]
public interface IClickHouseTransactionOptions : ITransactionOptions;