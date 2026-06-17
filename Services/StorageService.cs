using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using smartClass.Models;

namespace smartClass.Services
{
    public static class StorageService
    {
        private static readonly string StateFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appstate.json");
        private static readonly object _lockObject = new object();
        private const int MaxRetries = 5;
        private const int RetryDelayMs = 100;

        public static AppState Load()
        {
            lock (_lockObject)
            {
                try
                {
                    if (!File.Exists(StateFile))
                    {
                        var state = new AppState();
                        Save(state);
                        return state;
                    }

                    var json = ReadFileWithRetry();
                    var stateObj = JsonSerializer.Deserialize<AppState>(json);
                    return stateObj ?? new AppState();
                }
                catch
                {
                    return new AppState();
                }
            }
        }

        public static void Save(AppState state)
        {
            lock (_lockObject)
            {
                try
                {
                    var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
                    WriteFileWithRetry(json);
                }
                catch
                {
                    // swallow for now
                }
            }
        }

        private static string ReadFileWithRetry()
        {
            for (int i = 0; i < MaxRetries; i++)
            {
                try
                {
                    return File.ReadAllText(StateFile);
                }
                catch (IOException) when (i < MaxRetries - 1)
                {
                    Thread.Sleep(RetryDelayMs);
                }
            }
            return File.ReadAllText(StateFile);
        }

        private static void WriteFileWithRetry(string content)
        {
            for (int i = 0; i < MaxRetries; i++)
            {
                try
                {
                    File.WriteAllText(StateFile, content);
                    return;
                }
                catch (IOException) when (i < MaxRetries - 1)
                {
                    Thread.Sleep(RetryDelayMs);
                }
            }
            File.WriteAllText(StateFile, content);
        }
    }
}
