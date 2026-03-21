using System;
using System.Threading;
using System.Threading.Tasks;

namespace UserSpaceShapingDemo.Lib;

public sealed class Worker : IDisposable, IAsyncDisposable
{
    private readonly CancellationTokenSource _workerCancellation;
    private readonly Task _workerTask;

    public Worker(Action<CancellationToken> run)
    {
        _workerCancellation = new CancellationTokenSource();
        _workerTask = Task.Factory.StartNew(() =>
        {
            try
            {
                run(_workerCancellation.Token);
            }
            catch (OperationCanceledException) when (_workerCancellation.IsCancellationRequested)
            {
            }
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    public void Dispose()
    {
        _workerCancellation.Cancel();
        try
        {
            _workerTask.Wait();
        }
        finally
        {
            _workerCancellation.Dispose();
            _workerTask.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _workerCancellation.CancelAsync();
        try
        {
            await _workerTask.ConfigureAwait(false);
        }
        finally
        {
            _workerCancellation.Dispose();
            _workerTask.Dispose();
        }
    }
}