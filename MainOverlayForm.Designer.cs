namespace Deskplorer
{
	partial class MainOverlayForm
	{
		private System.ComponentModel.IContainer components = null;

		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
			AutoScaleMode = AutoScaleMode.Dpi;
         BackColor = Color.Lime;
			TransparencyKey = Color.Lime;
			FormBorderStyle = FormBorderStyle.None;
			ShowInTaskbar = false;
			StartPosition = FormStartPosition.Manual;
         TopMost = false;

			var virtualBounds = SystemInformation.VirtualScreen;
			Location = new Point(virtualBounds.Left, virtualBounds.Top);
			Size = virtualBounds.Size;
			Name = "MainOverlayForm";
			Text = "Deskplorer Overlay";
		}
	}
}
