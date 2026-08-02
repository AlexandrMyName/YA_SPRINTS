

namespace Concurrency_Lib;


public class ApiRateLimitedScheduler : TaskScheduler
{
    private readonly SemaphoreSlim _semaphore;
    private readonly Queue<Task> _tasks = new Queue<Task>();
    private readonly object _lock = new object();
    private readonly int _maxConcurrency;

    public ApiRateLimitedScheduler(int maxConcurrency)
    {
        _maxConcurrency = maxConcurrency;
        _semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
    }

    public override int MaximumConcurrencyLevel => _maxConcurrency;

    protected override void QueueTask(Task task)
    {
        lock (_lock)
        {
            _tasks.Enqueue(task);
        }

        // Запускаем обработку в пуле потоков
        _ = Task.Run(async () =>
        {
            await _semaphore.WaitAsync();
            try
            {
                Task taskToExecute = null;
                lock (_lock)
                {
                    if (_tasks.Count > 0)
                        taskToExecute = _tasks.Dequeue();
                }

                if (taskToExecute != null)
                    TryExecuteTask(taskToExecute);
            }
            finally
            {
                _semaphore.Release();
            }
        });
    }

    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
        // Не выполняем задачи inline для сохранения ограничения параллелизма
        return false;
    }

    protected override IEnumerable<Task> GetScheduledTasks()
    {
        lock (_lock)
        {
            return _tasks.ToArray();
        }
    }
}