namespace EightBot.Stellar;

public static class Schedulers
{
    private static Lazy<IScheduler> _shortTermTaskScheduler = new Lazy<IScheduler>(() => TaskPoolScheduler.Default.DisableOptimizations(typeof(ISchedulerLongRunning)), LazyThreadSafetyMode.ExecutionAndPublication);

    public static IScheduler ShortTermThreadPoolScheduler => _shortTermTaskScheduler.Value;
}