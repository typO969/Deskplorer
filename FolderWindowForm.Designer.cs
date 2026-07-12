using Deskplorer.Models;

namespace Deskplorer
{
	partial class FolderWindowForm
	{
		private System.ComponentModel.IContainer components = null;
      private Panel _headerPanel;
		private Label _titleLabel;
		private FlowLayoutPanel _quickActionsPanel;
      private Panel _itemsPanel;
		private Label _statusLabel;

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
			_headerPanel = new Panel();
			_titleLabel = new Label();
			_quickActionsPanel = new FlowLayoutPanel();
         _itemsPanel = new Panel();
			_statusLabel = new Label();
			_headerPanel.SuspendLayout();
			SuspendLayout();
			// 
			// _headerPanel
			// 
			_headerPanel.BackColor = Color.FromArgb(  0,   120,   215);
			_headerPanel.Controls.Add(_titleLabel);
			_headerPanel.Controls.Add(_quickActionsPanel);
			_headerPanel.Dock = DockStyle.Top;
			_headerPanel.Location = new Point(0, 0);
			_headerPanel.Name = "_headerPanel";
			_headerPanel.Size = new Size(403, 28);
			_headerPanel.TabIndex = 2;
			// 
			// _titleLabel
			// 
			_titleLabel.Dock = DockStyle.Right;
			_titleLabel.ForeColor = Color.White;
			_titleLabel.Location = new Point(223, 0);
			_titleLabel.Name = "_titleLabel";
			_titleLabel.Padding = new Padding(0, 0, 10, 0);
			_titleLabel.Size = new Size(180, 28);
			_titleLabel.TabIndex = 0;
			_titleLabel.Text = "deskplorer folder";
			_titleLabel.TextAlign = ContentAlignment.MiddleRight;
			// 
			// _quickActionsPanel
			// 
			_quickActionsPanel.BackColor = Color.Transparent;
			_quickActionsPanel.Dock = DockStyle.Left;
        _quickActionsPanel.FlowDirection = FlowDirection.LeftToRight;
			_quickActionsPanel.Location = new Point(0, 0);
			_quickActionsPanel.Name = "_quickActionsPanel";
			_quickActionsPanel.Padding = new Padding(8, 3, 0, 0);
			_quickActionsPanel.Size = new Size(80, 28);
			_quickActionsPanel.TabIndex = 1;
			// 
			// _itemsPanel
			// 
			_itemsPanel.AutoScroll = true;
			_itemsPanel.BackColor = Color.FromArgb(  24,   24,   24);
			_itemsPanel.Dock = DockStyle.Fill;
			_itemsPanel.Location = new Point(0, 28);
			_itemsPanel.Name = "_itemsPanel";
        _itemsPanel.Padding = new Padding(0);
			_itemsPanel.Size = new Size(403, 309);
			_itemsPanel.TabIndex = 0;
			// 
			// _statusLabel
			// 
			_statusLabel.BackColor = Color.FromArgb(  24,   24,   24);
			_statusLabel.Dock = DockStyle.Bottom;
			_statusLabel.ForeColor = Color.Gainsboro;
			_statusLabel.Location = new Point(0, 337);
			_statusLabel.Name = "_statusLabel";
			_statusLabel.Padding = new Padding(8, 0, 0, 0);
			_statusLabel.Size = new Size(403, 20);
			_statusLabel.TabIndex = 1;
			_statusLabel.Text = "0 items";
			_statusLabel.TextAlign = ContentAlignment.MiddleLeft;
			// 
			// FolderWindowForm
			// 
			AutoScaleDimensions = new SizeF(120F, 120F);
			AutoScaleMode = AutoScaleMode.Dpi;
			BackColor = Color.FromArgb(  24,   24,   24);
			ClientSize = new Size(403, 357);
			Controls.Add(_itemsPanel);
			Controls.Add(_statusLabel);
			Controls.Add(_headerPanel);
			FormBorderStyle = FormBorderStyle.None;
			Name = "FolderWindowForm";
			ShowInTaskbar = false;
			StartPosition = FormStartPosition.Manual;
			Text = "deskplorer folder";
			_headerPanel.ResumeLayout(false);
			ResumeLayout(false);
		}

		private Control CreateQuickActionLabel(string text)
		{
			return new Label
			{
				AutoSize = false,
				BackColor = Color.Transparent,
				ForeColor = Color.White,
				Font = new Font("Segoe UI", 10F, FontStyle.Bold),
				Margin = new Padding(0, 0, 8, 0),
				Size = new Size(18, 18),
				Text = text,
				TextAlign = ContentAlignment.MiddleCenter
			};
		}

    private Control CreateItemTile(DeskItem item)
		{
        var panel = new Panel
			{
				BackColor = Color.FromArgb(24, 24, 24),
            Cursor = Cursors.Hand,
				Margin = new Padding(8, 6, 8, 6),
				Size = new Size(82, 92)
			};

			var icon = new PictureBox
			{
           Cursor = Cursors.Hand,
				Image = SystemIcons.Application.ToBitmap(),
				Location = new Point(17, 4),
				Size = new Size(48, 48),
				SizeMode = PictureBoxSizeMode.StretchImage
			};

			var label = new Label
			{
            Text = item.DisplayName,
            Cursor = Cursors.Hand,
				ForeColor = Color.White,
            BackColor = Color.Transparent,
            AutoEllipsis = true,
				TextAlign = ContentAlignment.TopCenter,
				Location = new Point(0, 58),
				Size = new Size(82, 32)
			};

         panel.Tag = item;
			icon.Tag = item;
			label.Tag = item;

			panel.Controls.Add(icon);
			panel.Controls.Add(label);

			return panel;
		}
	}
}
