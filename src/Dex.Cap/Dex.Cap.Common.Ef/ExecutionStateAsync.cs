using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dex.Cap.Common.Ef;

internal sealed class ExecutionStateAsync<TState, TResult>(
    Func<TState, CancellationToken, Task<TResult>> operation,
    Func<TState, CancellationToken, Task<bool>> verifySucceeded,
    TState state)
{
    public Func<TState, CancellationToken, Task<TResult>> Operation { get; } = operation;
    public Func<TState, CancellationToken, Task<bool>> VerifySucceeded { get; } = verifySucceeded;
    public TState State { get; } = state;
    public TResult Result { get; set; } = default!;
}