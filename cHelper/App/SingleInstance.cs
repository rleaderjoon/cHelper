namespace cHelper.App;

public static class SingleInstance
{
    private static Mutex? _mutex;

    public static bool TryAcquire(out Mutex mutex)
    {
        mutex = new Mutex(true, "Global\\cHelper_SingleInstance", out bool created);
        _mutex = mutex;
        if (!created)
        {
            mutex.Dispose();
            _mutex = null;
        }
        return created;
    }
}
