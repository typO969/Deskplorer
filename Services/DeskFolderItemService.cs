using Deskplorer.Models;

namespace Deskplorer.Services
{
	internal static class DeskFolderItemService
	{
		public static string BuildCacheKey(string targetPath)
		{
			using var sha = System.Security.Cryptography.SHA256.Create();
			var bytes = System.Text.Encoding.UTF8.GetBytes(targetPath.Trim().ToLowerInvariant());
			return Convert.ToHexString(sha.ComputeHash(bytes)).ToLowerInvariant();
		}

		public static (int addedCount, int duplicateCount) AddFilesToFolder(
			DeskFolder folder,
			IEnumerable<string> filePaths,
			Func<int, Point?>? customPositionFactory = null,
			Func<string, string>? cacheKeyFactory = null)
		{
			var addedCount = 0;
			var duplicateCount = 0;

			foreach (var filePath in filePaths)
			{
				if (string.IsNullOrWhiteSpace(filePath))
				{
					continue;
				}

				if (folder.Items.Any(item => string.Equals(item.FilePath, filePath, StringComparison.OrdinalIgnoreCase)))
				{
					duplicateCount++;
					continue;
				}

				var displayName = Path.GetFileNameWithoutExtension(filePath);
				if (string.IsNullOrWhiteSpace(displayName))
				{
					displayName = Path.GetFileName(filePath);
				}

				var item = new DeskItem
				{
					DisplayName = displayName,
					FilePath = filePath,
					IconCacheKey = cacheKeyFactory?.Invoke(filePath) ?? string.Empty
				};

				if (customPositionFactory is not null)
				{
					item.CustomPosition = customPositionFactory(addedCount);
				}

				folder.Items.Add(item);
				addedCount++;
			}

			return (addedCount, duplicateCount);
		}
	}
}