using System;
using System.IO;
using System.Text.Json;

namespace MSM.Models
{
    public class AutoSaveSettings
    {
        public string? PrimaryPath { get; set; }
        public string? SecondaryPath { get; set; }

        private static readonly string SettingsFilePath = Path.Combine(AppContext.BaseDirectory, "autosave_settings.json");

        public static AutoSaveSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    return JsonSerializer.Deserialize<AutoSaveSettings>(json) ?? new AutoSaveSettings();
                }
            }
            catch
            {
                // Ignore errors, return default
            }
            return new AutoSaveSettings();
        }

        public void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFilePath, json);
            }
            catch
            {
                // Ignore save errors
            }
        }

        /// <summary>
        /// Check if a path exists and is accessible
        /// </summary>
        public static bool IsPathValid(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            try
            {
                return Directory.Exists(path);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get the fallback directory path (creates if doesn't exist)
        /// </summary>
        public static string GetFallbackPath()
        {
            var fallbackPath = Path.Combine(AppContext.BaseDirectory, "자동저장");
            if (!Directory.Exists(fallbackPath))
            {
                Directory.CreateDirectory(fallbackPath);
            }
            return fallbackPath;
        }

        /// <summary>
        /// Get the best available path for auto-save with fallback logic
        /// </summary>
        public string GetEffectivePath()
        {
            // Try primary path first
            if (IsPathValid(PrimaryPath))
                return PrimaryPath!;

            // Try secondary path
            if (IsPathValid(SecondaryPath))
                return SecondaryPath!;

            // Fallback to local directory
            return GetFallbackPath();
        }
    }
}
