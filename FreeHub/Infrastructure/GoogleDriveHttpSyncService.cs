using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FreeHub.Core;

namespace FreeHub.Infrastructure
{
    /// <summary>
    /// Synchronizes local SQLite telemetry databases with Google Drive REST API v3 over HTTP using OAuth 2.0.
    /// Zero external NuGet dependencies required.
    /// </summary>
    public class GoogleDriveHttpSyncService : ICloudSyncService
    {
        private const string DriveFolderName = "FreeHub_Data";
        private const string DriveScope = "https://www.googleapis.com/auth/drive.file";
        private const string ConfigFileName = "gdrive_oauth_config.txt";

        private readonly StoragePathResolver _pathResolver;
        private readonly string _configFilePath;

        private string _clientId;
        private string _clientSecret;
        private string _refreshToken;

        public GoogleDriveHttpSyncService(StoragePathResolver pathResolver)
        {
            if (pathResolver == null)
            {
                throw new ArgumentNullException("pathResolver");
            }

            _pathResolver = pathResolver;
            _configFilePath = Path.Combine(_pathResolver.GetDataDirectory(), ConfigFileName);

            // Ensure TLS 1.2 is enabled on .NET Framework 4.6 for Google HTTPS endpoints.
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            LoadConfiguration();
        }

        public bool IsConfigured()
        {
            return !string.IsNullOrWhiteSpace(_clientId) && !string.IsNullOrWhiteSpace(_clientSecret);
        }

        public string GetClientId()
        {
            return _clientId ?? string.Empty;
        }

        public void SaveCredentials(string clientId, string clientSecret)
        {
            _clientId = (clientId ?? string.Empty).Trim();
            _clientSecret = (clientSecret ?? string.Empty).Trim();
            PersistConfiguration();
        }

        public async Task<CloudSyncResult> SynchronizeAsync()
        {
            if (!IsConfigured())
            {
                return new CloudSyncResult
                {
                    Success = false,
                    Message = "Google Drive OAuth Client ID and Secret are not configured."
                };
            }

            try
            {
                string accessToken = await ObtainAccessTokenAsync().ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(accessToken))
                {
                    return new CloudSyncResult
                    {
                        Success = false,
                        Message = "Failed to authenticate with Google OAuth 2.0."
                    };
                }

                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                    string folderId = await EnsureDriveFolderAsync(httpClient).ConfigureAwait(false);
                    IList<DriveFileEntry> remoteFiles = await ListDatabaseFilesInFolderAsync(httpClient, folderId).ConfigureAwait(false);

                    int uploadedCount = await UploadCurrentMachineDatabaseAsync(httpClient, folderId, remoteFiles).ConfigureAwait(false);
                    int downloadedCount = await DownloadOtherMachinesDatabasesAsync(httpClient, remoteFiles).ConfigureAwait(false);

                    return new CloudSyncResult
                    {
                        Success = true,
                        UploadedFiles = uploadedCount,
                        DownloadedFiles = downloadedCount,
                        Message = string.Format("Synced! Uploaded: {0}, Downloaded: {1} remote DB(s).", uploadedCount, downloadedCount)
                    };
                }
            }
            catch (Exception ex)
            {
                return new CloudSyncResult
                {
                    Success = false,
                    Message = "Cloud Sync error: " + ex.Message
                };
            }
        }

        private async Task<string> ObtainAccessTokenAsync()
        {
            if (!string.IsNullOrWhiteSpace(_refreshToken))
            {
                string refreshedAccessToken = await TryRefreshAccessTokenAsync(_refreshToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(refreshedAccessToken))
                {
                    return refreshedAccessToken;
                }
            }

            return await PerformInteractiveBrowserLoginAsync().ConfigureAwait(false);
        }

        private async Task<string> TryRefreshAccessTokenAsync(string refreshToken)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    FormUrlEncodedContent content = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        { "client_id", _clientId },
                        { "client_secret", _clientSecret },
                        { "refresh_token", refreshToken },
                        { "grant_type", "refresh_token" }
                    });

                    HttpResponseMessage response = await client.PostAsync("https://oauth2.googleapis.com/token", content).ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode)
                    {
                        return null;
                    }

                    string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return ExtractJsonStringValue(json, "access_token");
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        private async Task<string> PerformInteractiveBrowserLoginAsync()
        {
            int port = GetAvailableLocalPort();
            string redirectUri = string.Format("http://127.0.0.1:{0}/callback/", port);

            using (HttpListener listener = new HttpListener())
            {
                listener.Prefixes.Add(redirectUri);
                listener.Start();

                string authUrl = string.Format(
                    "https://accounts.google.com/o/oauth2/v2/auth?client_id={0}&redirect_uri={1}&response_type=code&scope={2}&access_type=offline&prompt=consent",
                    Uri.EscapeDataString(_clientId),
                    Uri.EscapeDataString(redirectUri),
                    Uri.EscapeDataString(DriveScope));

                Process.Start(new ProcessStartInfo
                {
                    FileName = authUrl,
                    UseShellExecute = true
                });

                HttpListenerContext context = await listener.GetContextAsync().ConfigureAwait(false);
                string code = context.Request.QueryString["code"];

                string htmlResponse = "<html><body style='font-family:Segoe UI,sans-serif;background:#121218;color:#00E676;text-align:center;padding-top:80px;'>"
                    + "<h2>FreeHub Connected to Google Drive!</h2>"
                    + "<p style='color:#CCC;'>You can close this browser tab and return to FreeHub.</p></body></html>";
                byte[] buffer = Encoding.UTF8.GetBytes(htmlResponse);
                context.Response.ContentLength64 = buffer.Length;
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                context.Response.OutputStream.Close();
                listener.Stop();

                if (string.IsNullOrWhiteSpace(code))
                {
                    return null;
                }

                using (HttpClient client = new HttpClient())
                {
                    FormUrlEncodedContent tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        { "code", code },
                        { "client_id", _clientId },
                        { "client_secret", _clientSecret },
                        { "redirect_uri", redirectUri },
                        { "grant_type", "authorization_code" }
                    });

                    HttpResponseMessage response = await client.PostAsync("https://oauth2.googleapis.com/token", tokenRequest).ConfigureAwait(false);
                    string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new InvalidOperationException("OAuth token exchange failed: " + json);
                    }

                    string accessToken = ExtractJsonStringValue(json, "access_token");
                    string newRefreshToken = ExtractJsonStringValue(json, "refresh_token");

                    if (!string.IsNullOrWhiteSpace(newRefreshToken))
                    {
                        _refreshToken = newRefreshToken;
                        PersistConfiguration();
                    }

                    return accessToken;
                }
            }
        }

        private async Task<string> EnsureDriveFolderAsync(HttpClient httpClient)
        {
            string query = string.Format(
                "mimeType='application/vnd.google-apps.folder' and name='{0}' and trashed=false",
                DriveFolderName);

            string listUrl = "https://www.googleapis.com/drive/v3/files?q=" + Uri.EscapeDataString(query) + "&fields=files(id,name)";
            string listJson = await httpClient.GetStringAsync(listUrl).ConfigureAwait(false);

            IList<DriveFileEntry> folders = ParseDriveFilesList(listJson);
            if (folders.Count > 0)
            {
                return folders[0].Id;
            }

            string createMetadata = string.Format(
                "{{\"name\":\"{0}\",\"mimeType\":\"application/vnd.google-apps.folder\"}}",
                DriveFolderName);

            using (StringContent body = new StringContent(createMetadata, Encoding.UTF8, "application/json"))
            {
                HttpResponseMessage response = await httpClient.PostAsync("https://www.googleapis.com/drive/v3/files?fields=id", body).ConfigureAwait(false);
                string createdJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException("Failed to create FreeHub_Data folder on Google Drive: " + createdJson);
                }

                return ExtractJsonStringValue(createdJson, "id");
            }
        }

        private async Task<IList<DriveFileEntry>> ListDatabaseFilesInFolderAsync(HttpClient httpClient, string folderId)
        {
            string query = string.Format("'{0}' in parents and trashed=false", folderId);
            string url = "https://www.googleapis.com/drive/v3/files?q=" + Uri.EscapeDataString(query) + "&fields=files(id,name)";
            string json = await httpClient.GetStringAsync(url).ConfigureAwait(false);
            return ParseDriveFilesList(json);
        }

        private async Task<int> UploadCurrentMachineDatabaseAsync(HttpClient httpClient, string folderId, IList<DriveFileEntry> remoteFiles)
        {
            string localDbPath = _pathResolver.GetCurrentMachineDatabasePath();
            if (!File.Exists(localDbPath))
            {
                return 0;
            }

            string fileName = Path.GetFileName(localDbPath);
            byte[] fileBytes = ReadFileBytesSafely(localDbPath);

            string existingFileId = null;
            for (int i = 0; i < remoteFiles.Count; i++)
            {
                if (string.Equals(remoteFiles[i].Name, fileName, StringComparison.OrdinalIgnoreCase))
                {
                    existingFileId = remoteFiles[i].Id;
                    break;
                }
            }

            if (!string.IsNullOrWhiteSpace(existingFileId))
            {
                string updateUrl = string.Format("https://www.googleapis.com/upload/drive/v3/files/{0}?uploadType=media", existingFileId);
                using (HttpRequestMessage request = new HttpRequestMessage(new HttpMethod("PATCH"), updateUrl))
                {
                    ByteArrayContent content = new ByteArrayContent(fileBytes);
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    request.Content = content;

                    HttpResponseMessage response = await httpClient.SendAsync(request).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();
                }

                return 1;
            }

            string boundary = "freehub_boundary_" + Guid.NewGuid().ToString("N");
            string metadataJson = string.Format(
                "{{\"name\":\"{0}\",\"parents\":[\"{1}\"]}}",
                fileName,
                folderId);

            using (MultipartContent multipart = new MultipartContent("related", boundary))
            {
                StringContent metadataPart = new StringContent(metadataJson, Encoding.UTF8, "application/json");
                ByteArrayContent mediaPart = new ByteArrayContent(fileBytes);
                mediaPart.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                multipart.Add(metadataPart);
                multipart.Add(mediaPart);

                HttpResponseMessage response = await httpClient.PostAsync(
                    "https://www.googleapis.com/upload/drive/v3/files?uploadType=multipart",
                    multipart).ConfigureAwait(false);

                response.EnsureSuccessStatusCode();
            }

            return 1;
        }

        private async Task<int> DownloadOtherMachinesDatabasesAsync(HttpClient httpClient, IList<DriveFileEntry> remoteFiles)
        {
            string localMachineDbName = Path.GetFileName(_pathResolver.GetCurrentMachineDatabasePath());
            string dataDir = _pathResolver.GetDataDirectory();
            int downloaded = 0;

            for (int i = 0; i < remoteFiles.Count; i++)
            {
                DriveFileEntry remote = remoteFiles[i];
                if (!remote.Name.StartsWith("freehub_", StringComparison.OrdinalIgnoreCase) ||
                    !remote.Name.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.Equals(remote.Name, localMachineDbName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string downloadUrl = string.Format("https://www.googleapis.com/drive/v3/files/{0}?alt=media", remote.Id);
                byte[] bytes = await httpClient.GetByteArrayAsync(downloadUrl).ConfigureAwait(false);

                string targetPath = Path.Combine(dataDir, remote.Name);
                File.WriteAllBytes(targetPath, bytes);
                downloaded++;
            }

            return downloaded;
        }

        private static byte[] ReadFileBytesSafely(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                byte[] buffer = new byte[fs.Length];
                fs.Read(buffer, 0, buffer.Length);
                return buffer;
            }
        }

        private static int GetAvailableLocalPort()
        {
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private static string ExtractJsonStringValue(string json, string propertyName)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            string pattern = "\"" + Regex.Escape(propertyName) + "\"\\s*:\\s*\"([^\"]+)\"";
            Match match = Regex.Match(json, pattern);
            return match.Success ? match.Groups[1].Value : null;
        }

        private static IList<DriveFileEntry> ParseDriveFilesList(string json)
        {
            List<DriveFileEntry> results = new List<DriveFileEntry>();
            if (string.IsNullOrWhiteSpace(json))
            {
                return results;
            }

            MatchCollection objectMatches = Regex.Matches(json, "\\{[^{}]*\\}");
            for (int i = 0; i < objectMatches.Count; i++)
            {
                string block = objectMatches[i].Value;
                string id = ExtractJsonStringValue(block, "id");
                string name = ExtractJsonStringValue(block, "name");
                if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(name))
                {
                    results.Add(new DriveFileEntry { Id = id, Name = name });
                }
            }

            return results;
        }

        private void LoadConfiguration()
        {
            try
            {
                if (!File.Exists(_configFilePath))
                {
                    return;
                }

                string[] lines = File.ReadAllLines(_configFilePath, Encoding.UTF8);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    int separatorIndex = line.IndexOf('=');
                    if (separatorIndex <= 0)
                    {
                        continue;
                    }

                    string key = line.Substring(0, separatorIndex).Trim();
                    string value = line.Substring(separatorIndex + 1).Trim();

                    if (string.Equals(key, "ClientId", StringComparison.OrdinalIgnoreCase))
                    {
                        _clientId = value;
                    }
                    else if (string.Equals(key, "ClientSecret", StringComparison.OrdinalIgnoreCase))
                    {
                        _clientSecret = value;
                    }
                    else if (string.Equals(key, "RefreshToken", StringComparison.OrdinalIgnoreCase))
                    {
                        _refreshToken = value;
                    }
                }
            }
            catch (Exception)
            {
                // Ignore corrupt config file.
            }
        }

        private void PersistConfiguration()
        {
            try
            {
                string[] lines = new string[]
                {
                    "ClientId=" + (_clientId ?? string.Empty),
                    "ClientSecret=" + (_clientSecret ?? string.Empty),
                    "RefreshToken=" + (_refreshToken ?? string.Empty)
                };

                File.WriteAllLines(_configFilePath, lines, Encoding.UTF8);
            }
            catch (Exception)
            {
                // Ignore disk write error.
            }
        }

        private class DriveFileEntry
        {
            public string Id { get; set; }

            public string Name { get; set; }
        }
    }
}
