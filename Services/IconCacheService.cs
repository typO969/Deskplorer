using System.Collections.Concurrent;
using System.Drawing.Imaging;
using Deskplorer.Native;

namespace Deskplorer.Services
{
   public class IconCacheService
   {
      private readonly ConcurrentDictionary<string, Image> _imageCache = new(StringComparer.OrdinalIgnoreCase);
      private readonly string _iconCacheDirectory;

      public IconCacheService()
      {
         var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
         _iconCacheDirectory = Path.Combine(appDataPath, "Deskplorer", "icons");
         Directory.CreateDirectory(_iconCacheDirectory);
      }

      public Image GetIconImage(string targetPath, string? cacheKey = null)
      {
         if (string.IsNullOrWhiteSpace(targetPath))
         {
            return SystemIcons.Application.ToBitmap();
         }

         var key = string.IsNullOrWhiteSpace(cacheKey) ? BuildCacheKey(targetPath) : cacheKey;
         if (_imageCache.TryGetValue(key, out var cached))
         {
            return cached;
         }

         var diskPath = Path.Combine(_iconCacheDirectory, $"{key}.png");
         if (File.Exists(diskPath))
         {
            using var diskImage = Image.FromFile(diskPath);
            var loaded = new Bitmap(diskImage);
            _imageCache[key] = loaded;
            return loaded;
         }

         var extracted = ExtractIcon(targetPath);
         _imageCache[key] = extracted;

         try
         {
            extracted.Save(diskPath, ImageFormat.Png);
         }
         catch
         {
         }

         return extracted;
      }

      public Image GetImageResIconImage(int iconIndex)
      {
         var key = $"imageres_{iconIndex}";
         if (_imageCache.TryGetValue(key, out var cached))
         {
            return cached;
         }

         var diskPath = Path.Combine(_iconCacheDirectory, $"{key}.png");
         if (File.Exists(diskPath))
         {
            using var diskImage = Image.FromFile(diskPath);
            var loaded = new Bitmap(diskImage);
            _imageCache[key] = loaded;
            return loaded;
         }

         var extracted = ExtractImageResIcon(iconIndex);
         _imageCache[key] = extracted;

         try
         {
            extracted.Save(diskPath, ImageFormat.Png);
         }
         catch
         {
         }

         return extracted;
      }

      public List<int> GetAvailableImageResIconIndexes(int startIndex, int endIndex)
      {
         var available = new List<int>();
         if (endIndex < startIndex)
         {
            return available;
         }

         for (var i = startIndex; i <= endIndex; i++)
         {
            if (TryExtractImageResIcon(i, out _))
            {
               available.Add(i);
            }
         }

         return available;
      }

      public void RemoveCachedIcon(string cacheKey)
      {
         if (string.IsNullOrWhiteSpace(cacheKey))
         {
            return;
         }

         if (_imageCache.TryRemove(cacheKey, out var image))
         {
            image.Dispose();
         }

         var diskPath = Path.Combine(_iconCacheDirectory, $"{cacheKey}.png");
         if (File.Exists(diskPath))
         {
            try
            {
               File.Delete(diskPath);
            }
            catch
            {
            }
         }
      }

      public string BuildCacheKey(string targetPath)
      {
         using var sha = System.Security.Cryptography.SHA256.Create();
         var bytes = System.Text.Encoding.UTF8.GetBytes(targetPath.Trim().ToLowerInvariant());
         return Convert.ToHexString(sha.ComputeHash(bytes)).ToLowerInvariant();
      }

      private static Image ExtractIcon(string targetPath)
      {
         try
         {
            var icon = Icon.ExtractAssociatedIcon(targetPath);
            if (icon is not null)
            {
               return icon.ToBitmap();
            }
         }
         catch
         {
         }

         return SystemIcons.Application.ToBitmap();
      }

      private static Image ExtractImageResIcon(int iconIndex)
      {
         if (TryExtractImageResIcon(iconIndex, out var image))
         {
            return image;
         }

         return SystemIcons.Application.ToBitmap();
      }

      private static bool TryExtractImageResIcon(int iconIndex, out Image image)
      {
         image = SystemIcons.Application.ToBitmap();
         var imageResPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "imageres.dll");
         if (!File.Exists(imageResPath))
         {
            return false;
         }

         var smallIcons = new IntPtr[1];
         var extracted = User32.ExtractIconEx(imageResPath, iconIndex, null, smallIcons, 1);
         if (extracted == 0 || smallIcons[0] == IntPtr.Zero)
         {
            return false;
         }

         try
         {
            using var nativeIcon = Icon.FromHandle(smallIcons[0]);
            image = new Bitmap(nativeIcon.ToBitmap());
            return true;
         }
         catch
         {
            return false;
         }
         finally
         {
            User32.DestroyIcon(smallIcons[0]);
         }
      }
   }
}
