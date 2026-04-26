using System.Text.Json;
using Deskplorer.Models;

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
				return new AppState();
			}

			try
			{
				var json = File.ReadAllText(_stateFilePath);
				return JsonSerializer.Deserialize<AppState>(json, JsonOptions) ?? new AppState();
			}
			catch
			{
				return new AppState();
			}
		}

		public void Save(AppState state)
		{
			var directory = Path.GetDirectoryName(_stateFilePath);
			if (!string.IsNullOrWhiteSpace(directory))
			{
				Directory.CreateDirectory(directory);
			}

			var json = JsonSerializer.Serialize(state, JsonOptions);
			File.WriteAllText(_stateFilePath, json);
		}
	}
}
