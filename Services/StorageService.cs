using System;
using System.IO;
using System.Text.Json;
using smartClass.Models;

namespace smartClass.Services
{
    public static class StorageService
    {
        private static readonly string StateFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appstate.json");

        public static AppState Load()
        {
            try
            {
                if (!File.Exists(StateFile))
                {
                    var state = new AppState();
                    Save(state);
                    return state;
                }

                var json = File.ReadAllText(StateFile);
                var stateObj = JsonSerializer.Deserialize<AppState>(json);
                return stateObj ?? new AppState();
            }
            catch
            {
                return new AppState();
            }
        }

        public static void Save(AppState state)
        {
            try
            {
                var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(StateFile, json);
            }
            catch
            {
                // swallow for now
            }
        }
    }
}
