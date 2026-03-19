using System.Windows.Threading;

namespace MultiTool.App.Tests;

internal static class StaDispatcherTestRunner
{
    public static Task RunAsync(Func<Task> testBody)
    {
        var completionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var thread = new Thread(
            () =>
            {
                var dispatcher = Dispatcher.CurrentDispatcher;
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));

                _ = dispatcher.InvokeAsync(
                    async () =>
                    {
                        try
                        {
                            await testBody();
                            completionSource.TrySetResult();
                        }
                        catch (Exception ex)
                        {
                            completionSource.TrySetException(ex);
                        }
                        finally
                        {
                            dispatcher.BeginInvokeShutdown(DispatcherPriority.Background);
                        }
                    });

                Dispatcher.Run();
            });

        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();

        return completionSource.Task;
    }
}
