using System;
using System.IO;

namespace MegaCrossbows
{
    public static class ModLogger
    {
        private static string logFilePath;
        private static object lockObj = new object();

        public static void Initialize()
        {
            string pluginFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                @"r2modmanPlus-local\Valheim\profiles\Valheim Min Mods\BepInEx\plugins\MegaCrossbows"
            );
            
            logFilePath = Path.Combine(pluginFolder, "MegaCrossbows.log");
            
            // Ensure directory exists before creating log file
            try
            {
                // Create directory if it doesn't exist
                string directory = Path.GetDirectoryName(logFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    UnityEngine.Debug.Log($"[MegaCrossbows] Created log directory: {directory}");
                }
                
                // Clear old log on startup
                if (File.Exists(logFilePath))
                {
                    File.Delete(logFilePath);
                }
                
                // Write initial log header
                Log("=================================================");
                Log("MegaCrossbows Mod - Log Started");
                Log($"Time: {DateTime.Now}");
                Log($"Log Path: {logFilePath}");
                Log("=================================================");
                
                UnityEngine.Debug.Log($"[MegaCrossbows] Log file initialized: {logFilePath}");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[MegaCrossbows] Failed to initialize log file: {ex.Message}");
                UnityEngine.Debug.LogError($"[MegaCrossbows] Attempted path: {logFilePath}");
            }
        }

        public static void Log(string message)
        {
            if (string.IsNullOrEmpty(logFilePath)) return;

            lock (lockObj)
            {
                try
                {
                    // Ensure directory exists (in case it was deleted during runtime)
                    string directory = Path.GetDirectoryName(logFilePath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    
                    string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                    string logLine = $"[{timestamp}] {message}";
                    
                    File.AppendAllText(logFilePath, logLine + Environment.NewLine);
                    
                    // Also log to Unity console
                    UnityEngine.Debug.Log($"[MC] {message}");
                }
                catch (Exception ex)
                {
                    // If file logging fails, at least log to Unity console
                    UnityEngine.Debug.LogError($"[MegaCrossbows] Log write failed: {ex.Message}");
                    UnityEngine.Debug.Log($"[MC] {message}");
                }
            }
        }

        public static void LogError(string message)
        {
            Log($"ERROR: {message}");
        }

        public static void LogWarning(string message)
        {
            Log($"WARNING: {message}");
        }

        public static string GetLogFilePath()
        {
            return logFilePath;
        }
    }
}
