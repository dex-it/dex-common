﻿namespace Dex.Cap.OnceExecutor.Ef
{
    public interface IOnceExecutorEntityFramework<out TDbContext, T> : IOnceExecutor<TDbContext, T>
    {
    }
}