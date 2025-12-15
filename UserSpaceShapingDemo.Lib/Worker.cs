using System;
using System.Threading;
using System.Threading.Tasks;

namespace UserSpaceShapingDemo.Lib;

public sealed class Worker : IDisposable
{
    private readonly Task _workerTask;
    private readonly CancellationTokenSource _workerCancellation;

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
        }, TaskCreationOptions.LongRunning);
    }

    public void Dispose()
    {
        _workerCancellation.Cancel();
        _workerTask.Wait();
        _workerCancellation.Dispose();
        _workerTask.Dispose();
    }
}