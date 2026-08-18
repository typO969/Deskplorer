namespace Deskplorer.Models
{
	public class AppState
	{
		public List<DeskFolder> Folders { get; set; } = [];
    public bool HoverOpenEnabled { get; set; }
		public bool AnimationsEnabled { get; set; } = true;
		public bool DefaultAutoArrange { get; set; }
    public int DefaultTileIconSize { get; set; } = 48;
		public int HoverDetectionBorderDistance { get; set; } = 50;
		public int HoverCloseTimeoutSeconds { get; set; } = 2;
	}
}
