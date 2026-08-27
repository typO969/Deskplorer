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
		private readonly string _tempFilePath;
		private readonly string _backupFilePath;
		private readonly object _ioLock = new object();

		public PersistenceService()
		{
			var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			var appFolder = Path.Combine(appDataPath, "Deskplorer");

			_stateFilePath = Path.Combine(appFolder, "state.json");
			_tempFilePath = Path.Combine(appFolder, "state.json.tmp");
			_backupFilePath = Path.Combine(appFolder, "state.json.bak");
		}

		public AppState Load()
		{
			lock (_ioLock) // Prevent reading while another thread is mid-save
			{
				if (!File.Exists(_stateFilePath))
				{
					if (File.Exists(_backupFilePath))
					{
						LoggingService.Log("Main state file missing, recovering from backup.");
						return LoadFromFile(_backupFilePath);
					}
					LoggingService.Log("State file does not exist, returning new AppState");
					return new AppState();
				}

				return LoadFromFile(_stateFilePath);
			}
		}

		private AppState LoadFromFile(string path)
		{
			try
			{
				var json = File.ReadAllText(path);
				var result = JsonSerializer.Deserialize<AppState>(json, JsonOptions) ?? new AppState();
				LoggingService.Log($"Successfully loaded state from {Path.GetFileName(path)}");
				return result;
			}
			catch (Exception ex)
			{
				LoggingService.LogException(ex, $"PersistenceService.LoadFromFile ({Path.GetFileName(path)})");

				// If loading the main file fails (e.g., 0-byte corruption), instantly fallback to the .bak file
				if (path == _stateFilePath && File.Exists(_backupFilePath))
				{
					LoggingService.Log("Corrupted main state file detected, attempting recovery from backup.");
					try
					{
						var backupJson = File.ReadAllText(_backupFilePath);
						return JsonSerializer.Deserialize<AppState>(backupJson, JsonOptions) ?? new AppState();
					}
					catch { /* Backup is also corrupt, fallback to new */ }
				}

				LoggingService.Log("Returning default AppState due to load failure");
				return new AppState();
			}
		}

		public void Save(AppState state)
		{
			lock (_ioLock) // Guarantee only one thread touches the disk at a time
			{
				try
				{
					var directory = Path.GetDirectoryName(_stateFilePath);
					if (!string.IsNullOrWhiteSpace(directory))
					{
						Directory.CreateDirectory(directory);
					}

					// 1. Serialize data in memory FIRST before touching the disk.
					// If serialization fails, the files remain completely untouched.
					var json = JsonSerializer.Serialize(state, JsonOptions);

					// 2. Write to a temporary file. If power is lost here, 
					// state.json and state.json.bak remain perfectly intact.
					File.WriteAllText(_tempFilePath, json);

					// 3. Atomically rotate the files using native NTFS overwrite
					if (File.Exists(_stateFilePath))
					{
						File.Copy(_stateFilePath, _backupFilePath, overwrite: true);
						File.Move(_tempFilePath, _stateFilePath, overwrite: true);
					}
					else
					{
						File.Move(_tempFilePath, _stateFilePath);
					}

					LoggingService.Log("Successfully saved state to file atomically");
				}
				catch (Exception ex)
				{
					LoggingService.LogException(ex, "PersistenceService.Save");

					// Cleanup dangling temp file so we don't leave trash on the user's drive
					try { if (File.Exists(_tempFilePath)) File.Delete(_tempFilePath); } catch { }

					throw;
				}
			}
		}
	}
}