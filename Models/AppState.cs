namespace Deskplorer.Models
{
	public class AppState
	{
		public List<DeskFolder> Folders { get; set; } = new();
     public bool HoverOpenEnabled { get; set; }
	}
}
