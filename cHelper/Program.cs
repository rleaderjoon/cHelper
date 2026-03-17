using cHelper.App;

namespace cHelper;

static class Program
{
    [STAThread]
    static void Main()
    {
        if (!SingleInstance.TryAcquire(out var mutex))
        {
            MessageBox.Show("cHelper is already running.", "cHelper",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new cHelper.App.AppContext());

        mutex.ReleaseMutex();
        mutex.Dispose();
    }
}