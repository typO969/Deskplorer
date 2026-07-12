namespace Deskplorer.Models
{
	public class DeskFolder
	{
		public Guid Id { get; set; } = Guid.NewGuid();
		public string Name { get; set; } = string.Empty;
		public Point ClosedLocation { get; set; }
		public Size OpenSize { get; set; } = new Size(420, 420);
		public string MonitorId { get; set; } = string.Empty;
		public bool Locked { get; set; }
		public bool RequiresPlacementBeforeOpen { get; set; }
      public bool AutoArrange { get; set; } = false;
		public int ItemIconSize { get; set; } = 48;
    public string IconPath { get; set; } = string.Empty;
		public bool IsHidden { get; set; }
		public List<DeskItem> Items { get; set; } = new();
	}
}
