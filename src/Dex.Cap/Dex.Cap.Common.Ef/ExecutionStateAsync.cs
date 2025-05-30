using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dex.Cap.Common.Ef;

internal sealed class ExecutionStateAsync<TState, TResult>
{
    public ExecutionStateAsync(
        Func<TState, CancellationToken, Task<TResult>> operation,
        Func<TState, CancellationToken, Task<bool>> verifySucceeded,
        TState state)
    {
        Operation = operation;
        VerifySucceeded = verifySucceeded;
        State = state;
    }

    public Func<TState, CancellationToken, Task<TResult>> Operation { get; }
    public Func<TState, CancellationToken, Task<bool>> VerifySucceeded { get; }
    public TState State { get; }
    public TResult Result { get; set; } = default!;
}