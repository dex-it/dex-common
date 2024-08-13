using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Dex.Audit.EF.Interfaces;

/// <summary>
/// Интерфейс интерсептора аудита, отвечающий за перехват изменений контекста базы данных и отправку записей аудита.
/// </summary>
public interface IAuditSaveChangesInterceptor : ISaveChangesInterceptor;