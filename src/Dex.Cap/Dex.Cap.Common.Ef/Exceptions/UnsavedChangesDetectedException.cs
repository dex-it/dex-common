using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Dex.Cap.Common.Ef.Exceptions;

public sealed class UnsavedChangesDetectedException : Exception
{
    private const string UnsavedChangesCount = "unsaved_changes_count";
    private const string UnsavedChangesFirst10 = "unsaved_changes_first_10";

    public override string Message
    {
        get
        {
            var unsavedChangesCount = Data.Contains(UnsavedChangesCount) ? Data[UnsavedChangesCount] : null;
            var unsavedChangesFirst10 = Data.Contains(UnsavedChangesFirst10) ? Data[UnsavedChangesFirst10] : null;

            var unsavedChangesCountString = unsavedChangesCount is null
                ? string.Empty
                : $" unsavedChangesCount: {unsavedChangesCount}";

            var unsavedChangesFirst10String = unsavedChangesFirst10 is null
                ? string.Empty
                : $" unsavedChangesFirst10: {unsavedChangesFirst10}";

            return base.Message + unsavedChangesCountString + unsavedChangesFirst10String;
        }
    }

    public UnsavedChangesDetectedException()
    {
    }

    public UnsavedChangesDetectedException(string message) : base(message)
    {
    }

    public UnsavedChangesDetectedException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public UnsavedChangesDetectedException(DbContext dbContext, string message) : base(message)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        var entries = dbContext.ChangeTracker.Entries()
            .Where(e => e.State != EntityState.Unchanged)
            .Select(e => e.Entity.GetType()).ToArray();

        Data[UnsavedChangesCount] = entries.Length;
        Data[UnsavedChangesFirst10] = string.Join("; ", entries.Take(10));
    }
}