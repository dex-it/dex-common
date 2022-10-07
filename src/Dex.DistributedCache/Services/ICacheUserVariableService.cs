using System;

namespace Dex.DistributedCache.Services
{
    public interface ICacheUserVariableService
    {
        Guid UserId { get; }
    }
}