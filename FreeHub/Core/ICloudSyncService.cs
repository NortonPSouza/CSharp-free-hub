using System.Threading.Tasks;

namespace FreeHub.Core
{
    /// <summary>
    /// Represents the outcome of a cloud synchronization operation.
    /// </summary>
    public class CloudSyncResult
    {
        public bool Success { get; set; }

        public int UploadedFiles { get; set; }

        public int DownloadedFiles { get; set; }

        public string Message { get; set; }
    }

    /// <summary>
    /// Defines asynchronous cloud synchronization operations for multi-machine SQLite databases.
    /// </summary>
    public interface ICloudSyncService
    {
        bool IsConfigured();

        string GetClientId();

        void SaveCredentials(string clientId, string clientSecret);

        Task<CloudSyncResult> SynchronizeAsync();
    }
}
