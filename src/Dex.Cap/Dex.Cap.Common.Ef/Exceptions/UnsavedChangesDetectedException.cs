using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Dex.Cap.Common.Ef.Exceptions
{
    public sealed class UnsavedChangesDetectedException : Exception
    {
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
            if (dbContext == null) throw new ArgumentNullException(nameof(dbContext));

            var entries = dbContext.ChangeTracker.Entries().Select(e => e.Entity).ToArray();
            Data["unsaved_changes_count"] = entries.Length;
            Data["unsaved_changes_first_10"] = entries.Take(10);
        }
    }
}