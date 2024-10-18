﻿using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Dex.Audit.EF.Interfaces;

/// <summary>
/// The interface of the interceptor for intercepting a transaction commit in the database.
/// </summary>
public interface IAuditDbTransactionInterceptor : IDbTransactionInterceptor;