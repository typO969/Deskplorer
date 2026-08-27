using System.Collections.Concurrent;
using System.Drawing.Imaging;

using Deskplorer.Native;

namespace Deskplorer.Services
{
	public class IconCacheService
	{
		// Elite Pattern: Lazy<Image> guarantees the factory method runs exactly ONCE per key,
		// completely eliminating GDI object leaks under heavy UI concurrency.
		private readonly ConcurrentDictionary<string, Lazy<Image>> _imageCache = new(StringComparer.OrdinalIgnoreCase);
		private readonly string _iconCacheDirectory;

		public IconCacheService()
		{
			var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			_iconCacheDirectory = Path.Combine(appDataPath, "Deskplorer", "icons");
			Directory.CreateDirectory(_iconCacheDirectory);
		}

		public Image GetIconImage(string targetPath, string? cacheKey = null)
		{
			if (string.IsNullOrWhiteSpace(targetPath)) return SystemIcons.Application.ToBitmap();

			var key = string.IsNullOrWhiteSpace(cacheKey) ? BuildCacheKey(targetPath) : cacheKey;
			return _imageCache.GetOrAdd(key, k => new Lazy<Image>(() => LoadOrExtractIcon(targetPath, k))).Value;
		}

		public Image GetImageResIconImage(int iconIndex)
		{
			var key = $"imageres_{iconIndex}";
			return _imageCache.GetOrAdd(key, k => new Lazy<Image>(() => LoadOrExtractImageResIcon(iconIndex, k))).Value;
		}

		private Image LoadOrExtractIcon(string targetPath, string key)
		{
			var diskPath = Path.Combine(_iconCacheDirectory, $"{key}.png");
			if (File.Exists(diskPath))
			{
				try
				{
					using var diskImage = Image.FromFile(diskPath);
					return new Bitmap(diskImage);
				}
				catch { /* Corrupted cache, fallback to extraction */ }
			}

			var extracted = ExtractIcon(targetPath);
			SaveToDisk(extracted, diskPath);
			return extracted;
		}

		private Image LoadOrExtractImageResIcon(int iconIndex, string key)
		{
			var diskPath = Path.Combine(_iconCacheDirectory, $"{key}.png");
			if (File.Exists(diskPath))
			{
				try
				{
					using var diskImage = Image.FromFile(diskPath);
					return new Bitmap(diskImage);
				}
				catch { /* Corrupted cache, fallback to extraction */ }
			}

			var extracted = ExtractImageResIcon(iconIndex);
			SaveToDisk(extracted, diskPath);
			return extracted;
		}

		private static void SaveToDisk(Image image, string diskPath)
		{
			try { image.Save(diskPath, ImageFormat.Png); }
			catch { /* Silent fail for anti-virus/I/O locks */ }
		}

		public List<int> GetAvailableImageResIconIndexes(int startIndex, int endIndex)
		{
			var available = new List<int>();
			if (endIndex < startIndex) return available;

			for (var i = startIndex; i <= endIndex; i++)
			{
				if (TryExtractImageResIcon(i, out var image))
				{
					image?.Dispose(); // Prevent GDI leak during theoretical probing
					available.Add(i);
				}
			}
			return available;
		}

		public void RemoveCachedIcon(string cacheKey)
		{
			if (string.IsNullOrWhiteSpace(cacheKey)) return;

			if (_imageCache.TryRemove(cacheKey, out var lazyImage) && lazyImage.IsValueCreated)
			{
				lazyImage.Value.Dispose();
			}

			var diskPath = Path.Combine(_iconCacheDirectory, $"{cacheKey}.png");
			if (File.Exists(diskPath))
			{
				try { File.Delete(diskPath); } catch { }
			}
		}

		public string BuildCacheKey(string targetPath)
		{
			var bytes = System.Text.Encoding.UTF8.GetBytes(targetPath.Trim().ToLowerInvariant());
			return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant();
		}

		private static Image ExtractIcon(string targetPath)
		{
			try
			{
				using var icon = Icon.ExtractAssociatedIcon(targetPath);
				if (icon is not null) return icon.ToBitmap();
			}
			catch { }
			return SystemIcons.Application.ToBitmap();
		}

		private static Image ExtractImageResIcon(int iconIndex)
		{
			if (TryExtractImageResIcon(iconIndex, out var image) && image != null) return image;
			return SystemIcons.Application.ToBitmap();
		}

		private static bool TryExtractImageResIcon(int iconIndex, out Image? image)
		{
			image = null;
			var imageResPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "imageres.dll");

			if (!File.Exists(imageResPath)) return false;

			var smallIcons = new IntPtr[1];
			var extracted = User32.ExtractIconEx(imageResPath, iconIndex, null, smallIcons, 1);

			if (extracted == 0 || smallIcons[0] == IntPtr.Zero) return false;

			try
			{
				using var nativeIcon = Icon.FromHandle(smallIcons[0]);
				// BUG FIX: Removed 'new Bitmap(...)'. ToBitmap() already returns a detached GDI+ Bitmap.
				image = nativeIcon.ToBitmap();
				return true;
			}
			catch
			{
				return false;
			}
			finally
			{
				User32.DestroyIcon(smallIcons[0]); // CRITICAL: Free unmanaged memory
			}
		}
	}
}