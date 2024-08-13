using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Dex.Audit.EF.Interfaces;

/// <summary>
/// Интерфейс интерсептора для перехвата коммита транзакции в базе данных.
/// </summary>
public interface IAuditDbTransactionInterceptor : IDbTransactionInterceptor;