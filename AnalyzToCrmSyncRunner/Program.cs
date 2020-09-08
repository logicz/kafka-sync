using SyncRunnerAnalyzToCrm;

namespace AnalyzToCrmSyncRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            Startup startup = new Startup();
            startup.LoadConfigurations();
            startup.Start();
        }
    }
}
