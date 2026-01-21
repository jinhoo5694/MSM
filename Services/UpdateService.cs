using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
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

        private static readonly string _appDirectory = AppDomain.CurrentDomain.BaseDirectory;

        static UpdateService()
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            _httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache, no-store");
            _httpClient.DefaultRequestHeaders.Add("Pragma", "no-cache");
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
                    string? assetName = null;
                    foreach (var asset in release.assets ?? Array.Empty<GitHubAsset>())
                    {
                        if (asset.name?.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            downloadUrl = asset.browser_download_url;
                            assetName = asset.name;
                            break;
                        }
                    }

                    return new UpdateInfo
                    {
                        CurrentVersion = currentVersion,
                        LatestVersion = latestVersion,
                        DownloadUrl = downloadUrl ?? "",
                        AssetName = assetName ?? "update.zip",
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
        /// Download update and apply it
        /// </summary>
        public static async Task<bool> DownloadAndInstallAsync(string downloadUrl, Action<int>? progressCallback = null)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "MSM_Update_" + Guid.NewGuid().ToString("N")[..8]);
            string zipPath = Path.Combine(tempDir, "update.zip");
            string extractPath = Path.Combine(tempDir, "extracted");

            try
            {
                Directory.CreateDirectory(tempDir);
                Directory.CreateDirectory(extractPath);

                // Step 1: Download the zip file to temp directory
                using (var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    var totalBytes = response.Content.Headers.ContentLength ?? -1;
                    var downloadedBytes = 0L;

                    using var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None);
                    using var downloadStream = await response.Content.ReadAsStreamAsync();

                    var buffer = new byte[81920];
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

                // Step 2: Extract ZIP using C# ZipFile
                ZipFile.ExtractToDirectory(zipPath, extractPath, overwriteFiles: true);

                // Step 3: Find the actual source folder (might be in a subdirectory)
                string actualSource = extractPath;
                var dirs = Directory.GetDirectories(extractPath);
                if (dirs.Length == 1 && Directory.GetFiles(extractPath).Length == 0)
                {
                    actualSource = dirs[0];
                }

                // Step 4: Create updater script
                string updaterScript = CreateUpdaterScript(actualSource, _appDirectory, tempDir);
                string scriptPath = Path.Combine(tempDir, "update.bat");
                await File.WriteAllTextAsync(scriptPath, updaterScript);

                // Step 5: Launch updater and exit
                var startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{scriptPath}\"",
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    WorkingDirectory = tempDir
                };

                Process.Start(startInfo);

                // Give the script a moment to start
                await Task.Delay(500);

                // Exit the application
                Environment.Exit(0);

                return true;
            }
            catch
            {
                // Cleanup on failure
                try
                {
                    if (Directory.Exists(tempDir))
                        Directory.Delete(tempDir, true);
                }
                catch { }

                return false;
            }
        }

        /// <summary>
        /// Create a batch script that copies files and restarts the app
        /// </summary>
        private static string CreateUpdaterScript(string sourcePath, string targetPath, string tempDir)
        {
            string exePath = Path.Combine(targetPath, "MSM.exe");

            return $@"@echo off
echo ========================================
echo MSM Auto-Updater
echo ========================================
echo.
echo Waiting for application to close...
timeout /t 3 /nobreak > nul

echo.
echo Copying new files...
echo Source: {sourcePath}
echo Target: {targetPath}
echo.

REM Copy all files EXCEPT data files
for %%f in (""{sourcePath}\*.*"") do (
    if /I not ""%%~nxf""==""stock.xlsx"" (
        if /I not ""%%~nxf""==""stock_logs.json"" (
            if /I not ""%%~nxf""==""autosave_settings.json"" (
                echo Copying: %%~nxf
                copy /Y ""%%f"" ""{targetPath}\"" > nul
            )
        )
    )
)

REM Copy subdirectories if any (like runtimes, wwwroot, etc.)
for /D %%d in (""{sourcePath}\*"") do (
    echo Copying folder: %%~nxd
    xcopy /E /Y /I ""%%d"" ""{targetPath}\%%~nxd"" > nul 2>&1
)

echo.
echo ========================================
echo Update complete!
echo ========================================
echo.
echo Starting application...
timeout /t 2 /nobreak > nul

start """" ""{exePath}""

REM Cleanup temp files after a delay
timeout /t 5 /nobreak > nul
rd /s /q ""{tempDir}"" 2>nul

exit
";
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
        public string AssetName { get; set; } = "";
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
