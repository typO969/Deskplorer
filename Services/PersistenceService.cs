using System.Text.Json;
using Deskplorer.Models;
using Deskplorer.Services;

namespace Deskplorer.Services
{
    public class PersistenceService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        private readonly string _stateFilePath;

        public PersistenceService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "Deskplorer");
            _stateFilePath = Path.Combine(appFolder, "state.json");
        }

        public AppState Load()
        {
            if (!File.Exists(_stateFilePath))
            {
                LoggingService.Log("State file does not exist, returning new AppState");
                return new AppState();
            }

            try
            {
                var json = File.ReadAllText(_stateFilePath);
                var result = JsonSerializer.Deserialize<AppState>(json, JsonOptions) ?? new AppState();
                LoggingService.Log("Successfully loaded state from file");
                return result;
            }
            catch (Exception ex)
            {
                LoggingService.LogException(ex, "PersistenceService.Load");
                LoggingService.Log("Returning default AppState due to load failure");
                return new AppState();
            }
        }

        public void Save(AppState state)
        {
            try
            {
                var directory = Path.GetDirectoryName(_stateFilePath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(state, JsonOptions);
                File.WriteAllText(_stateFilePath, json);
                LoggingService.Log("Successfully saved state to file");
            }
            catch (Exception ex)
            {
                LoggingService.LogException(ex, "PersistenceService.Save");
                throw; // Re-throw to let calling code know there was a failure
            }
        }
    }
}