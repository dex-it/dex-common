using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Dex.Audit.EF.Interceptors.Interfaces;

/// <summary>
/// The interface of the audit interceptor, responsible for intercepting changes to the database context and sending audit records.
/// </summary>
public interface IAuditSaveChangesInterceptor : ISaveChangesInterceptor;