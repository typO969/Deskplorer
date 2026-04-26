namespace Deskplorer.Models
{
	public class DeskItem
	{
		public Guid Id { get; set; } = Guid.NewGuid();
		public string DisplayName { get; set; } = string.Empty;
		public string FilePath { get; set; } = string.Empty;
		public string IconCacheKey { get; set; } = string.Empty;
		public Point? CustomPosition { get; set; }
	}
}
