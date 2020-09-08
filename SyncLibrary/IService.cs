using Microsoft.Extensions.Configuration;

namespace SyncLibrary
{
    public interface IService
    {
        void LoadConfig(IConfigurationRoot configuration);
        void Start();
    }
}
