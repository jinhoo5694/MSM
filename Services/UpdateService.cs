using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace MSM.Services
{
    public class UpdateService
    {
        private const string GitHubApiUrl = "https://api.github.com/repos/jinhoo5694/MSM/releases/latest";
        private const string UserAgent = "MSM-UpdateChecker";

        private static readonly HttpClient _httpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        static UpdateService()
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
        }

        /// <summary>
        /// Get current app version
        /// </summary>
        public static string GetCurrentVersion()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";
        }

        /// <summary>
        /// Check for updates from GitHub releases
        /// </summary>
        public static async Task<UpdateInfo?> CheckForUpdateAsync()
        {
            try
            {
                var response = await _httpClient.GetStringAsync(GitHubApiUrl);
                var release = JsonSerializer.Deserialize<GitHubRelease>(response);

                if (release == null || string.IsNullOrEmpty(release.tag_name))
                    return null;

                // Parse version from tag (e.g., "v1.0.1" -> "1.0.1")
                var latestVersion = release.tag_name.TrimStart('v', 'V');
                var currentVersion = GetCurrentVersion();

                if (IsNewerVersion(latestVersion, currentVersion))
                {
                    // Find the zip asset
                    string? downloadUrl = null;
                    foreach (var asset in release.assets ?? Array.Empty<GitHubAsset>())
                    {
                        if (asset.name?.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            downloadUrl = asset.browser_download_url;
                            break;
                        }
                    }

                    return new UpdateInfo
                    {
                        CurrentVersion = currentVersion,
                        LatestVersion = latestVersion,
                        DownloadUrl = downloadUrl ?? "",
                        ReleaseNotes = release.body ?? "",
                        IsUpdateAvailable = true
                    };
                }

                return new UpdateInfo
                {
                    CurrentVersion = currentVersion,
                    LatestVersion = latestVersion,
                    IsUpdateAvailable = false
                };
            }
            catch (Exception ex)
            {
                return new UpdateInfo
                {
                    CurrentVersion = GetCurrentVersion(),
                    Error = ex.Message,
                    IsUpdateAvailable = false
                };
            }
        }

        /// <summary>
        /// Download update and prepare installer
        /// </summary>
        public static async Task<bool> DownloadAndInstallAsync(string downloadUrl, Action<int>? progressCallback = null)
        {
            try
            {
                var appDir = AppContext.BaseDirectory;
                var updateZipPath = Path.Combine(appDir, "update.zip");
                var updaterScriptPath = Path.Combine(appDir, "update.bat");

                // Download the zip file
                using (var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    var totalBytes = response.Content.Headers.ContentLength ?? -1;
                    var downloadedBytes = 0L;

                    using var fileStream = new FileStream(updateZipPath, FileMode.Create, FileAccess.Write, FileShare.None);
                    using var downloadStream = await response.Content.ReadAsStreamAsync();

                    var buffer = new byte[8192];
                    int bytesRead;
                    while ((bytesRead = await downloadStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        downloadedBytes += bytesRead;

                        if (totalBytes > 0)
                        {
                            var progress = (int)((downloadedBytes * 100) / totalBytes);
                            progressCallback?.Invoke(progress);
                        }
                    }
                }

                // Create the updater batch script
                var batchScript = $@"@echo off
chcp 65001 >nul
echo 업데이트 중입니다. 잠시만 기다려 주세요...
timeout /t 2 /nobreak > nul

:: Wait for app to close
taskkill /f /im MSM.exe 2>nul
timeout /t 2 /nobreak > nul

:: Backup data files
echo 데이터 파일 백업 중...
if exist ""stock.xlsx"" copy /y ""stock.xlsx"" ""stock.xlsx.bak"" >nul
if exist ""stock_logs.json"" copy /y ""stock_logs.json"" ""stock_logs.json.bak"" >nul
if exist ""autosave_settings.json"" copy /y ""autosave_settings.json"" ""autosave_settings.json.bak"" >nul

:: Extract new version
echo 새 버전 설치 중...
powershell -command ""Expand-Archive -Path 'update.zip' -DestinationPath '.' -Force""

:: Restore data files
echo 데이터 파일 복원 중...
if exist ""stock.xlsx.bak"" (
    copy /y ""stock.xlsx.bak"" ""stock.xlsx"" >nul
    del ""stock.xlsx.bak"" >nul
)
if exist ""stock_logs.json.bak"" (
    copy /y ""stock_logs.json.bak"" ""stock_logs.json"" >nul
    del ""stock_logs.json.bak"" >nul
)
if exist ""autosave_settings.json.bak"" (
    copy /y ""autosave_settings.json.bak"" ""autosave_settings.json"" >nul
    del ""autosave_settings.json.bak"" >nul
)

:: Cleanup
echo 정리 중...
del ""update.zip"" >nul 2>&1

:: Launch new version
echo 프로그램을 다시 시작합니다...
start """" ""MSM.exe""

:: Self-delete this script
(goto) 2>nul & del ""%~f0""
";

                await File.WriteAllTextAsync(updaterScriptPath, batchScript, System.Text.Encoding.UTF8);

                // Launch the updater script and exit
                var startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{updaterScriptPath}\"",
                    WorkingDirectory = appDir,
                    UseShellExecute = true,
                    CreateNoWindow = false
                };

                Process.Start(startInfo);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsNewerVersion(string latest, string current)
        {
            try
            {
                var latestParts = latest.Split('.');
                var currentParts = current.Split('.');

                for (int i = 0; i < Math.Max(latestParts.Length, currentParts.Length); i++)
                {
                    var latestNum = i < latestParts.Length ? int.Parse(latestParts[i]) : 0;
                    var currentNum = i < currentParts.Length ? int.Parse(currentParts[i]) : 0;

                    if (latestNum > currentNum) return true;
                    if (latestNum < currentNum) return false;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
    }

    public class UpdateInfo
    {
        public string CurrentVersion { get; set; } = "";
        public string LatestVersion { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
        public string ReleaseNotes { get; set; } = "";
        public string Error { get; set; } = "";
        public bool IsUpdateAvailable { get; set; }
    }

    // GitHub API response models
    public class GitHubRelease
    {
        public string tag_name { get; set; } = "";
        public string body { get; set; } = "";
        public GitHubAsset[]? assets { get; set; }
    }

    public class GitHubAsset
    {
        public string name { get; set; } = "";
        public string browser_download_url { get; set; } = "";
    }
}
