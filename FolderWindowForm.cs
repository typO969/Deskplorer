using Deskplorer.Models;
using Deskplorer.Services;

namespace Deskplorer
{
	public partial class FolderWindowForm : Form
	{
      private const int ManualGridSize = 12;
		private const int DragActivationThreshold = 6;
    private static readonly Size MinimumWindowSize = new(300, 220);

		private readonly DeskFolder _folder;
     private readonly Rectangle _anchorIconScreenBounds;
		private readonly LaunchService _launchService = new();
    private readonly IconCacheService _iconCacheService = new();
		private readonly ToolStripMenuItem _windowMenuItem = new("Window");
		private const string WindowMenuItemName = "_windowMenuItem";
		private const string WindowMenuSeparatorName = "_windowMenuSeparator";
		private readonly ToolTip _toolTip = new();
		private DeskItem? _contextMenuItem;
		private Label? _arrangeModeAction;
		private Control? _dragTile;
		private DeskItem? _dragItem;
		private Point _dragStartMouse;
		private Point _dragStartTileLocation;
		private bool _isDraggingTile;
    private bool _didDragTile;
		private bool _suppressLaunchOnMouseUp;
		private bool _isDraggingWindow;
		private Point _windowDragStartMouse;
		private Point _windowDragStartLocation;
		private bool _isResizingWindow;
		private Point _resizeStartMouse;
		private Size _resizeStartSize;
		private Point _resizeStartLocation;

     public bool IsInteractingWithWindow => _isDraggingWindow || _isResizingWindow;

		public event EventHandler? FolderItemsChanged;

    public FolderWindowForm(DeskFolder folder, Rectangle anchorIconScreenBounds)
		{
			_folder = folder;
        _anchorIconScreenBounds = anchorIconScreenBounds;
			InitializeComponent();
			InitializeQuickActions();
        InitializeWindowMenu();
         InitializeDropSupport();
         InitializeWindowMoveResizeSupport();
			ApplyFolderData();
		}

		private void InitializeWindowMoveResizeSupport()
		{
			var headerDragControls = new Control[] { _headerPanel, _titleLabel, _quickActionsPanel };
			foreach (var control in headerDragControls)
			{
				control.MouseDown += HeaderDrag_MouseDown;
				control.MouseMove += HeaderDrag_MouseMove;
				control.MouseUp += HeaderDrag_MouseUp;
			}

			var resizeHandle = new Panel
			{
				Name = "_resizeHandle",
				Size = new Size(14, 14),
				Cursor = Cursors.SizeNWSE,
				BackColor = Color.Transparent,
				Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
				Location = new Point(ClientSize.Width - 14, ClientSize.Height - 14)
			};

			resizeHandle.MouseDown += ResizeHandle_MouseDown;
			resizeHandle.MouseMove += ResizeHandle_MouseMove;
			resizeHandle.MouseUp += ResizeHandle_MouseUp;
			Controls.Add(resizeHandle);
			resizeHandle.BringToFront();

			Shown += (_, _) => ApplyConstrainedBounds();
			Shown += (_, _) => LoadItemIconsAsync();
		}

		private void HeaderDrag_MouseDown(object? sender, MouseEventArgs e)
		{
			if (e.Button != MouseButtons.Left)
			{
				return;
			}

			_isDraggingWindow = true;
			_windowDragStartMouse = MousePosition;
			_windowDragStartLocation = Location;
		}

		private void HeaderDrag_MouseMove(object? sender, MouseEventArgs e)
		{
			if (!_isDraggingWindow)
			{
				return;
			}

			var deltaX = MousePosition.X - _windowDragStartMouse.X;
			var deltaY = MousePosition.Y - _windowDragStartMouse.Y;
			var proposed = new Point(_windowDragStartLocation.X + deltaX, _windowDragStartLocation.Y + deltaY);
			Location = ClampWindowLocation(proposed, Size);
		}

		private void HeaderDrag_MouseUp(object? sender, MouseEventArgs e)
		{
			if (e.Button != MouseButtons.Left)
			{
				return;
			}

			_isDraggingWindow = false;
		}

		private void ResizeHandle_MouseDown(object? sender, MouseEventArgs e)
		{
			if (e.Button != MouseButtons.Left)
			{
				return;
			}

			_isResizingWindow = true;
			_resizeStartMouse = MousePosition;
			_resizeStartSize = Size;
			_resizeStartLocation = Location;
		}

		private void ResizeHandle_MouseMove(object? sender, MouseEventArgs e)
		{
			if (!_isResizingWindow)
			{
				return;
			}

			var deltaX = MousePosition.X - _resizeStartMouse.X;
			var deltaY = MousePosition.Y - _resizeStartMouse.Y;
			var proposed = new Size(_resizeStartSize.Width + deltaX, _resizeStartSize.Height + deltaY);
			Size = ClampWindowSize(proposed, _resizeStartLocation);
			Location = ClampWindowLocation(Location, Size);
		}

		private void ResizeHandle_MouseUp(object? sender, MouseEventArgs e)
		{
			if (e.Button != MouseButtons.Left)
			{
				return;
			}

			_isResizingWindow = false;
		}

		private void ApplyConstrainedBounds()
		{
			Size = ClampWindowSize(Size, Location);
			Location = ClampWindowLocation(Location, Size);
		}

		private Point ClampWindowLocation(Point proposed, Size size)
		{
         var bounds = Screen.FromPoint(_anchorIconScreenBounds.Location).WorkingArea;
			var maxX = Math.Max(bounds.Left, bounds.Right - size.Width);
        var maxY = Math.Max(bounds.Top, bounds.Bottom - size.Height);
			var centerX = _anchorIconScreenBounds.Left + (_anchorIconScreenBounds.Width / 2);
			var centerY = _anchorIconScreenBounds.Top + (_anchorIconScreenBounds.Height / 2);

			var minXByAnchor = centerX - size.Width;
			var maxXByAnchor = centerX;
			var minYByAnchor = centerY - size.Height;
			var maxYByAnchor = centerY;

			var clampedMinX = Math.Clamp(minXByAnchor, bounds.Left, maxX);
			var clampedMaxX = Math.Clamp(maxXByAnchor, bounds.Left, maxX);
			if (clampedMaxX < clampedMinX)
			{
				clampedMaxX = clampedMinX;
			}

			var clampedMinY = Math.Clamp(minYByAnchor, bounds.Top, maxY);
			var clampedMaxY = Math.Clamp(maxYByAnchor, bounds.Top, maxY);
			if (clampedMaxY < clampedMinY)
			{
				clampedMaxY = clampedMinY;
			}

			return new Point(
            Math.Clamp(proposed.X, clampedMinX, clampedMaxX),
				Math.Clamp(proposed.Y, clampedMinY, clampedMaxY));
		}

		private Size ClampWindowSize(Size proposed, Point currentLocation)
		{
         var bounds = Screen.FromPoint(_anchorIconScreenBounds.Location).WorkingArea;
			var centerX = _anchorIconScreenBounds.Left + (_anchorIconScreenBounds.Width / 2);
			var centerY = _anchorIconScreenBounds.Top + (_anchorIconScreenBounds.Height / 2);

			var maxWidthByAnchor = Math.Max(MinimumWindowSize.Width, Math.Max(centerX - currentLocation.X, currentLocation.X - centerX) * 2);
			var maxHeightByAnchor = Math.Max(MinimumWindowSize.Height, Math.Max(centerY - currentLocation.Y, currentLocation.Y - centerY) * 2);
			var maxWidth = Math.Max(MinimumWindowSize.Width, bounds.Right - currentLocation.X);
        var maxHeight = Math.Max(MinimumWindowSize.Height, bounds.Bottom - currentLocation.Y);

			maxWidth = Math.Min(maxWidth, maxWidthByAnchor);
			maxHeight = Math.Min(maxHeight, maxHeightByAnchor);

			var minimumContentSize = CalculateMinimumContentWindowSize();
			var minWidth = Math.Max(MinimumWindowSize.Width, minimumContentSize.Width);
			var minHeight = Math.Max(MinimumWindowSize.Height, minimumContentSize.Height);

			return new Size(
          Math.Clamp(proposed.Width, minWidth, Math.Max(minWidth, maxWidth)),
				Math.Clamp(proposed.Height, minHeight, Math.Max(minHeight, maxHeight)));
		}

		private Size CalculateMinimumContentWindowSize()
		{
			if (_folder.Items.Count == 0)
			{
				return MinimumWindowSize;
			}

			if (_folder.AutoArrange)
			{
				const int tileWidth = 82;
				const int tileHeight = 92;
				const int spacingX = 16;
				const int spacingY = 12;
				const int columns = 3;

				var rows = (int)Math.Ceiling(_folder.Items.Count / (double)columns);
				var contentWidth = (columns * tileWidth) + ((columns - 1) * spacingX) + 20;
				var contentHeight = (rows * tileHeight) + ((rows - 1) * spacingY) + 20;
				return new Size(contentWidth, contentHeight + _headerPanel.Height + _statusLabel.Height);
			}

			var furthestX = 0;
			var furthestY = 0;
			foreach (var item in _folder.Items)
			{
				var pos = item.CustomPosition ?? Point.Empty;
				furthestX = Math.Max(furthestX, pos.X + 82);
				furthestY = Math.Max(furthestY, pos.Y + 92);
			}

			return new Size(furthestX + 16, furthestY + _headerPanel.Height + _statusLabel.Height + 16);
		}

		private void InitializeDropSupport()
		{
			_itemsPanel.AllowDrop = true;
			_itemsPanel.DragEnter += ItemsPanel_DragEnter;
			_itemsPanel.DragDrop += ItemsPanel_DragDrop;
		}

		private void ItemsPanel_DragEnter(object? sender, DragEventArgs e)
		{
			if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
			{
				e.Effect = DragDropEffects.Copy;
				return;
			}

			e.Effect = DragDropEffects.None;
		}

		private void ItemsPanel_DragDrop(object? sender, DragEventArgs e)
		{
			if (e.Data?.GetData(DataFormats.FileDrop) is not string[] droppedPaths || droppedPaths.Length == 0)
			{
				return;
			}

			var dropPoint = _itemsPanel.PointToClient(new Point(e.X, e.Y));
			var nextDropPoint = ClampTileLocation(dropPoint, new Size(82, 92));
			var result = DeskFolderItemService.AddFilesToFolder(
				_folder,
				droppedPaths,
				_folder.AutoArrange ? null : _ =>
				{
					var point = nextDropPoint;
					nextDropPoint = ClampTileLocation(new Point(nextDropPoint.X + 24, nextDropPoint.Y + 24), new Size(82, 92));
					return point;
				},
				_iconCacheService.BuildCacheKey);

			if (result.addedCount == 0)
			{
				if (result.duplicateCount > 0)
				{
					MessageBox.Show(this, "Dropped items are already in this folder.", "Deskplorer", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
				return;
			}

			ApplyFolderData();
			FolderItemsChanged?.Invoke(this, EventArgs.Empty);

			if (result.duplicateCount > 0)
			{
				MessageBox.Show(this, $"Added {result.addedCount} item(s). {result.duplicateCount} duplicate item(s) were skipped.", "Deskplorer", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right && _contextMenuItem is null)
			{
				ContextMenuStrip?.Show(this, PointToClient(Cursor.Position));
				return;
			}

			base.OnMouseUp(e);
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				_contextMenuItem = null;
			}

			base.OnMouseDown(e);
		}

		private void InitializeQuickActions()
		{
			var addItemAction = CreateQuickActionLabel("+");
			addItemAction.Cursor = Cursors.Hand;
			addItemAction.Click += AddItemAction_Click;
			_toolTip.SetToolTip(addItemAction, "Add item");
			_quickActionsPanel.Controls.Add(addItemAction);

			_arrangeModeAction = (Label)CreateQuickActionLabel(string.Empty);
			_arrangeModeAction.Cursor = Cursors.Hand;
			_arrangeModeAction.Click += ArrangeModeAction_Click;
			_quickActionsPanel.Controls.Add(_arrangeModeAction);
			UpdateArrangeModeActionText();
		}

     private void InitializeWindowMenu()
		{
        var refreshIconMenuItem = new ToolStripMenuItem("Refresh Tile Icon") { Name = "_refreshTileIconMenuItem" };
			refreshIconMenuItem.Click += RefreshIconMenuItem_Click;
       var resetIconMenuItem = new ToolStripMenuItem("Reset Tile Icon") { Name = "_resetTileIconMenuItem" };
			resetIconMenuItem.Click += ResetIconMenuItem_Click;
         var resetAllIconsMenuItem = new ToolStripMenuItem("Reset All Folder's Icons") { Name = "_resetAllFolderIconsMenuItem" };
			resetAllIconsMenuItem.Click += ResetAllIconsMenuItem_Click;
        var renameMenuItem = new ToolStripMenuItem("Rename Tile") { Name = "_renameTileMenuItem" };
			renameMenuItem.Click += RenameMenuItem_Click;
       var removeMenuItem = new ToolStripMenuItem("Remove from Folder") { Name = "_removeTileMenuItem" };
			removeMenuItem.Click += RemoveMenuItem_Click;
			var importMenuItem = new ToolStripMenuItem("Import desktop shortcut");
			var importMultipleMenuItem = new ToolStripMenuItem("Import multiple desktop shortcuts...");
			importMultipleMenuItem.Click += ImportMultipleDesktopShortcutsMenuItem_Click;
			importMenuItem.DropDownItems.Add(importMultipleMenuItem);

			_windowMenuItem.Name = WindowMenuItemName;
			_windowMenuItem.DropDownItems.Add(refreshIconMenuItem);
			_windowMenuItem.DropDownItems.Add(resetIconMenuItem);
			_windowMenuItem.DropDownItems.Add(resetAllIconsMenuItem);
			_windowMenuItem.DropDownItems.Add(renameMenuItem);
			_windowMenuItem.DropDownItems.Add(removeMenuItem);
			_windowMenuItem.DropDownItems.Add(new ToolStripSeparator());
			_windowMenuItem.DropDownItems.Add(importMenuItem);
			_windowMenuItem.DropDownOpening += WindowMenu_Opening;
		}

		public void RefreshFolderHeader()
		{
			_titleLabel.Text = _folder.Name;
			Text = _folder.Name;
		}

		public void ReloadFolderData()
		{
			ApplyFolderData();
		}

     public void SetSharedContextMenu(ContextMenuStrip menu)
		{
			var existingWindowMenuItems = menu.Items.OfType<ToolStripMenuItem>().Where(item => string.Equals(item.Name, WindowMenuItemName, StringComparison.Ordinal)).ToList();
			foreach (var existing in existingWindowMenuItems)
			{
				menu.Items.Remove(existing);
			}

			foreach (var existingSeparator in menu.Items.OfType<ToolStripSeparator>().Where(item => string.Equals(item.Name, WindowMenuSeparatorName, StringComparison.Ordinal)).ToList())
			{
				menu.Items.Remove(existingSeparator);
			}

			menu.Items.Add(new ToolStripSeparator { Name = WindowMenuSeparatorName });
			menu.Items.Add(_windowMenuItem);

			ContextMenuStrip = menu;
			_headerPanel.ContextMenuStrip = menu;
			_titleLabel.ContextMenuStrip = menu;
			_quickActionsPanel.ContextMenuStrip = menu;
			_itemsPanel.ContextMenuStrip = menu;
			_statusLabel.ContextMenuStrip = menu;
		}

		private void ApplyFolderData()
		{
			RefreshFolderHeader();
			if (_folder.OpenSize.Width > 0 && _folder.OpenSize.Height > 0)
			{
				ClientSize = _folder.OpenSize;
			}

			_itemsPanel.SuspendLayout();
			try
			{
			_itemsPanel.Controls.Clear();
			if (_folder.Items.Count == 0)
			{
				_itemsPanel.Controls.Add(CreateEmptyStateLabel());
				_statusLabel.Text = "0 items";
				return;
			}

			foreach (var item in _folder.Items)
			{
				var tile = CreateItemTile(item);
				WireItemContextMenu(tile, item);
				WireItemDrag(tile, item);
				_itemsPanel.Controls.Add(tile);
			}

			if (_folder.AutoArrange)
			{
				ArrangeTilesAutomatically();
			}
			else
			{
				ArrangeTilesManually();
			}

			_statusLabel.Text = $"{_folder.Items.Count} items";
			}
			finally
			{
				_itemsPanel.ResumeLayout(true);
			}

			if (IsHandleCreated && !IsDisposed)
			{
				LoadItemIconsAsync();
			}
		}

		private void LoadItemIconsAsync()
		{
			if (!IsHandleCreated || IsDisposed)
			{
				return;
			}

			foreach (Control tile in _itemsPanel.Controls)
			{
				if (tile.Tag is not DeskItem item || tile.Controls.Count == 0 || tile.Controls[0] is not PictureBox icon || icon.IsDisposed)
				{
					continue;
				}

				var filePath = item.FilePath;
				var cacheKey = item.IconCacheKey;
				_ = System.Threading.Tasks.Task.Run(() => _iconCacheService.GetIconImage(filePath, cacheKey))
					.ContinueWith(task =>
					{
						if (!task.IsCompletedSuccessfully || IsDisposed || icon.IsDisposed)
						{
							return;
						}

						try
						{
							if (!icon.IsDisposed && ReferenceEquals(icon.Tag, item))
							{
								if (icon.InvokeRequired)
								{
									icon.BeginInvoke(new Action(() =>
									{
										if (!icon.IsDisposed && ReferenceEquals(icon.Tag, item))
										{
											icon.Image = task.Result;
										}
									}));
								}
								else
								{
									icon.Image = task.Result;
								}
							}
						}
						catch
						{
						}
					}, System.Threading.Tasks.TaskScheduler.Default);
			}
		}

		private void ArrangeModeAction_Click(object? sender, EventArgs e)
		{
			_folder.AutoArrange = !_folder.AutoArrange;
			UpdateArrangeModeActionText();
			ApplyFolderData();
			FolderItemsChanged?.Invoke(this, EventArgs.Empty);
		}

		private void ResetAllIconsMenuItem_Click(object? sender, EventArgs e)
		{
			var anyReset = false;
			foreach (var item in _folder.Items)
			{
				if (!string.IsNullOrWhiteSpace(item.IconCacheKey))
				{
					_iconCacheService.RemoveCachedIcon(item.IconCacheKey);
					item.IconCacheKey = string.Empty;
					anyReset = true;
				}
			}

			if (!anyReset)
			{
				return;
			}

			ApplyFolderData();
			FolderItemsChanged?.Invoke(this, EventArgs.Empty);
		}

		private void RefreshIconMenuItem_Click(object? sender, EventArgs e)
		{
			if (_contextMenuItem is null)
			{
				return;
			}

			if (string.IsNullOrWhiteSpace(_contextMenuItem.FilePath))
			{
				return;
			}

			if (!string.IsNullOrWhiteSpace(_contextMenuItem.IconCacheKey))
			{
				_iconCacheService.RemoveCachedIcon(_contextMenuItem.IconCacheKey);
			}

			_contextMenuItem.IconCacheKey = _iconCacheService.BuildCacheKey(_contextMenuItem.FilePath);
			ApplyFolderData();
			FolderItemsChanged?.Invoke(this, EventArgs.Empty);
		}

		private void ImportMultipleDesktopShortcutsMenuItem_Click(object? sender, EventArgs e)
		{
			MessageBox.Show(this, "Importing desktop shortcuts is not available yet.", "Deskplorer", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void ResetIconMenuItem_Click(object? sender, EventArgs e)
		{
			if (_contextMenuItem is null)
			{
				return;
			}

			if (!string.IsNullOrWhiteSpace(_contextMenuItem.IconCacheKey))
			{
				_iconCacheService.RemoveCachedIcon(_contextMenuItem.IconCacheKey);
			}

			_contextMenuItem.IconCacheKey = string.Empty;
			ApplyFolderData();
			FolderItemsChanged?.Invoke(this, EventArgs.Empty);
		}

		private void UpdateArrangeModeActionText()
		{
			if (_arrangeModeAction is null)
			{
				return;
			}

			_arrangeModeAction.Text = _folder.AutoArrange ? "A" : "M";
			_toolTip.SetToolTip(_arrangeModeAction, _folder.AutoArrange ? "Auto-arrange enabled" : "Manual arrangement enabled");
		}

		private void ArrangeTilesAutomatically()
		{
			var tiles = _itemsPanel.Controls.Cast<Control>().Where(c => c.Tag is DeskItem).ToList();
			if (tiles.Count == 0)
			{
				return;
			}

			const int startX = 10;
			const int startY = 10;
			const int spacingX = 16;
			const int spacingY = 12;
			var tileWidth = tiles[0].Width;
			var tileHeight = tiles[0].Height;
			var availableWidth = Math.Max(tileWidth, _itemsPanel.ClientSize.Width - (startX * 2));
			var columns = Math.Max(1, (availableWidth + spacingX) / (tileWidth + spacingX));

			for (var i = 0; i < tiles.Count; i++)
			{
				var row = i / columns;
				var col = i % columns;
				tiles[i].Location = new Point(startX + col * (tileWidth + spacingX), startY + row * (tileHeight + spacingY));
			}
		}

		private void ArrangeTilesManually()
		{
			const int startX = 10;
			const int startY = 10;
			const int spacingX = 16;
			const int spacingY = 12;
			var index = 0;

			foreach (Control tile in _itemsPanel.Controls)
			{
				if (tile.Tag is not DeskItem item)
				{
					continue;
				}

				if (item.CustomPosition is Point stored)
				{
					tile.Location = ClampTileLocation(stored, tile.Size);
              item.CustomPosition = tile.Location;
				}
				else
				{
					var availableWidth = Math.Max(tile.Width, _itemsPanel.ClientSize.Width - (startX * 2));
					var columns = Math.Max(1, (availableWidth + spacingX) / (tile.Width + spacingX));
					var row = index / columns;
					var col = index % columns;
					tile.Location = new Point(startX + col * (tile.Width + spacingX), startY + row * (tile.Height + spacingY));
					item.CustomPosition = tile.Location;
				}

				index++;
			}
		}

		private Point ClampTileLocation(Point proposed, Size tileSize)
		{
			const int min = 4;
         var layoutBounds = _itemsPanel.DisplayRectangle;
			var maxX = Math.Max(min, layoutBounds.Width - tileSize.Width - min);
			var maxY = Math.Max(min, layoutBounds.Height - tileSize.Height - min);

			var clampedX = Math.Clamp(proposed.X, min, maxX);
			var clampedY = Math.Clamp(proposed.Y, min, maxY);

			var snappedX = SnapCoordinate(clampedX, min, maxX);
			var snappedY = SnapCoordinate(clampedY, min, maxY);

			return new Point(snappedX, snappedY);
		}

		private static int SnapCoordinate(int value, int origin, int max)
		{
			var snapped = origin + (int)Math.Round((value - origin) / (double)ManualGridSize) * ManualGridSize;
			return Math.Clamp(snapped, origin, max);
		}

		private Control CreateEmptyStateLabel()
		{
			return new Label
			{
				AutoSize = false,
				ForeColor = Color.Gainsboro,
				BackColor = Color.Transparent,
				TextAlign = ContentAlignment.MiddleCenter,
				Margin = new Padding(8),
				Size = new Size(220, 72),
				Text = "Folder is empty\nClick + to add items"
			};
		}

		private void WireItemContextMenu(Control tile, DeskItem item)
		{
         if (tile.Controls.Count > 1 && tile.Controls[1] is Label label)
			{
				label.Text = FormatLabelText(item.DisplayName, label.Width, label.Font, 2);
				_toolTip.SetToolTip(label, item.DisplayName);
			}

			tile.MouseUp += ItemControl_MouseUp;
				tile.ContextMenuStrip = ContextMenuStrip;
			tile.Tag = item;

			foreach (Control child in tile.Controls)
			{
				child.MouseUp += ItemControl_MouseUp;
				child.ContextMenuStrip = ContextMenuStrip;
				child.Tag = item;
			}
		}

		private void WindowMenu_Opening(object? sender, EventArgs e)
		{
			var enableItemActions = _contextMenuItem is not null;
			var refreshMenuItem = _windowMenuItem.DropDownItems.Find("_refreshTileIconMenuItem", false).OfType<ToolStripMenuItem>().FirstOrDefault();
			if (refreshMenuItem is not null)
			{
				refreshMenuItem.Enabled = enableItemActions;
			}

			var resetMenuItem = _windowMenuItem.DropDownItems.Find("_resetTileIconMenuItem", false).OfType<ToolStripMenuItem>().FirstOrDefault();
			if (resetMenuItem is not null)
			{
				resetMenuItem.Enabled = enableItemActions;
			}

			var resetAllMenuItem = _windowMenuItem.DropDownItems.Find("_resetAllFolderIconsMenuItem", false).OfType<ToolStripMenuItem>().FirstOrDefault();
			if (resetAllMenuItem is not null)
			{
				resetAllMenuItem.Enabled = _folder.Items.Count > 0;
			}

			var renameMenuItem = _windowMenuItem.DropDownItems.Find("_renameTileMenuItem", false).OfType<ToolStripMenuItem>().FirstOrDefault();
			if (renameMenuItem is not null)
			{
				renameMenuItem.Enabled = enableItemActions;
			}

			var removeMenuItem = _windowMenuItem.DropDownItems.Find("_removeTileMenuItem", false).OfType<ToolStripMenuItem>().FirstOrDefault();
			if (removeMenuItem is not null)
			{
				removeMenuItem.Enabled = enableItemActions;
			}
		}

		private static string FormatLabelText(string text, int maxWidth, Font font, int maxLines)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				return string.Empty;
			}

			if (maxWidth <= 0 || maxLines <= 0)
			{
				return string.Empty;
			}

			var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			if (words.Length == 0)
			{
				return string.Empty;
			}

			var lines = new List<string>();
			var currentLine = string.Empty;

         var truncated = false;
			for (var i = 0; i < words.Length; i++)
			{
          var word = words[i];
				var candidate = string.IsNullOrEmpty(currentLine) ? word : $"{currentLine} {word}";
          if (FitsLine(candidate, maxWidth, font) || string.IsNullOrEmpty(currentLine))
				{
					currentLine = candidate;
					continue;
				}

				lines.Add(currentLine);
				currentLine = word;

          if (lines.Count == maxLines - 1)
				{
               truncated = i < words.Length - 1;
					break;
				}
			}

         if (lines.Count < maxLines)
			{
				lines.Add(currentLine);
			}

			if (lines.Count > 0)
			{
				var lastIndex = Math.Min(lines.Count, maxLines) - 1;
				var lastLine = lines[lastIndex];
				if (truncated || !FitsLine(lastLine, maxWidth, font))
				{
					lines[lastIndex] = TrimLineToWidth(lastLine, maxWidth, font);
				}
			}

			return string.Join("\n", lines.Take(maxLines));
		}

		private static bool FitsLine(string text, int maxWidth, Font font)
		{
			return TextRenderer.MeasureText(text, font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix).Width <= maxWidth;
		}

		private static string TrimLineToWidth(string text, int maxWidth, Font font)
		{
			var trimmed = text.TrimEnd();
			if (string.IsNullOrEmpty(trimmed))
			{
				return string.Empty;
			}

			while (trimmed.Length > 0 && !FitsLine($"{trimmed}…", maxWidth, font))
			{
				trimmed = trimmed[..^1];
			}

			return trimmed.Length == 0 ? "…" : $"{trimmed}…";
		}

		private void WireItemDrag(Control tile, DeskItem item)
		{
			tile.MouseDown += ItemDrag_MouseDown;
			tile.MouseMove += ItemDrag_MouseMove;
			tile.MouseUp += ItemDrag_MouseUp;

			foreach (Control child in tile.Controls)
			{
				child.MouseDown += ItemDrag_MouseDown;
				child.MouseMove += ItemDrag_MouseMove;
				child.MouseUp += ItemDrag_MouseUp;
			}
		}

		public bool IsShowingDialog { get; private set; }

		private void AddItemAction_Click(object? sender, EventArgs e)
{
    using var dialog = new OpenFileDialog
    {
        Title = "Add item to folder",
        Filter = "All files (*.*)|*.*",
        CheckFileExists = true,
           Multiselect = true
    };

    try
    {
        IsShowingDialog = true;
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }
    }
    finally
    {
        IsShowingDialog = false;
    }

				var nextDropPoint = ClampTileLocation(new Point(10, 10), new Size(82, 92));
				var result = DeskFolderItemService.AddFilesToFolder(
					_folder,
					dialog.FileNames,
					_folder.AutoArrange ? null : _ =>
					{
						var point = nextDropPoint;
						nextDropPoint = ClampTileLocation(new Point(nextDropPoint.X + 24, nextDropPoint.Y + 24), new Size(82, 92));
						return point;
					},
					_iconCacheService.BuildCacheKey);

			if (result.addedCount == 0)
			{
				if (result.duplicateCount > 0)
				{
					MessageBox.Show(this, "Dropped items are already in this folder.", "Deskplorer", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
				return;
			}

			ApplyFolderData();
			FolderItemsChanged?.Invoke(this, EventArgs.Empty);

			if (result.duplicateCount > 0)
			{
				MessageBox.Show(this, $"Added {result.addedCount} item(s). {result.duplicateCount} duplicate item(s) were skipped.", "Deskplorer", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}

		private void ItemControl_MouseUp(object? sender, MouseEventArgs e)
		{
			if (sender is not Control control)
			{
				return;
			}

			var item = control.Tag as DeskItem ?? control.Parent?.Tag as DeskItem;
			if (item is null)
			{
           _contextMenuItem = null;
				return;
			}


			if (e.Button == MouseButtons.Right)
			{
				_contextMenuItem = item;
				ContextMenuStrip?.Show(control, e.Location);
				return;
			}

			if (e.Button == MouseButtons.Left)
			{
            _contextMenuItem = item;
          if (_didDragTile)
				{
					_didDragTile = false;
					_suppressLaunchOnMouseUp = false;
					return;
				}

				if (_suppressLaunchOnMouseUp)
				{
					_suppressLaunchOnMouseUp = false;
					return;
				}

				LaunchItem(item);
			}
		}

		private void ItemDrag_MouseDown(object? sender, MouseEventArgs e)
		{
			if (e.Button != MouseButtons.Left || _folder.AutoArrange)
			{
				return;
			}

			var tile = ResolveTileFromSender(sender);
			if (tile?.Tag is not DeskItem item)
			{
				return;
			}

			_isDraggingTile = true;
       _didDragTile = false;
			_dragTile = tile;
			_dragItem = item;
			_dragStartMouse = MousePosition;
			_dragStartTileLocation = tile.Location;
		}

		private void ItemDrag_MouseMove(object? sender, MouseEventArgs e)
		{
			if (!_isDraggingTile || _dragTile is null)
			{
				return;
			}

			var currentMouse = MousePosition;
			var deltaX = currentMouse.X - _dragStartMouse.X;
			var deltaY = currentMouse.Y - _dragStartMouse.Y;

        if (Math.Abs(deltaX) > DragActivationThreshold || Math.Abs(deltaY) > DragActivationThreshold)
			{
          _didDragTile = true;
				_suppressLaunchOnMouseUp = true;
			}

			var proposed = new Point(_dragStartTileLocation.X + deltaX, _dragStartTileLocation.Y + deltaY);
			_dragTile.Location = ClampTileLocation(proposed, _dragTile.Size);
		}

		private void ItemDrag_MouseUp(object? sender, MouseEventArgs e)
		{
			if (e.Button != MouseButtons.Left || !_isDraggingTile)
			{
				return;
			}

			_isDraggingTile = false;
			if (_dragTile is not null && _dragItem is not null && !_folder.AutoArrange)
			{
				_dragItem.CustomPosition = _dragTile.Location;
				FolderItemsChanged?.Invoke(this, EventArgs.Empty);
			}

			_dragTile = null;
			_dragItem = null;
		}

		private Control? ResolveTileFromSender(object? sender)
		{
         if (sender is not Control control)
			{
          return null;
			}

			var current = control;
			while (current.Parent is not null && current.Parent != _itemsPanel)
			{
				current = current.Parent;
			}

			if (current.Parent == _itemsPanel && current.Tag is DeskItem)
			{
				return current;
			}

			return null;
		}

		private void RemoveMenuItem_Click(object? sender, EventArgs e)
		{
			if (_contextMenuItem is null)
			{
				return;
			}

			var itemToRemove = _contextMenuItem;
			_contextMenuItem = null;

			if (MessageBox.Show(this,
				$"Remove '{itemToRemove.DisplayName}' from this folder?",
				"Deskplorer",
				MessageBoxButtons.YesNo,
				MessageBoxIcon.Question) != DialogResult.Yes)
			{
				return;
			}

			_folder.Items.RemoveAll(i => i.Id == itemToRemove.Id);
			ApplyFolderData();
			FolderItemsChanged?.Invoke(this, EventArgs.Empty);
		}

		private void RenameMenuItem_Click(object? sender, EventArgs e)
		{
			if (_contextMenuItem is null)
			{
				return;
			}

			if (!PromptDialog.TryShow(this, "Rename Item", "Item name:", _contextMenuItem.DisplayName, out var newName))
			{
				return;
			}

			newName = newName.Trim();
			if (string.IsNullOrWhiteSpace(newName))
			{
				MessageBox.Show(this, "Item name cannot be empty.", "Deskplorer", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			_contextMenuItem.DisplayName = newName;
			ApplyFolderData();
			FolderItemsChanged?.Invoke(this, EventArgs.Empty);
		}

		private void LaunchItem(DeskItem item)
		{
			if (string.IsNullOrWhiteSpace(item.FilePath))
			{
				return;
			}

			if (!_launchService.TryLaunch(item.FilePath, out var errorMessage))
			{
				MessageBox.Show(this, $"Unable to launch '{item.FilePath}'.\n{errorMessage}", "Deskplorer", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

	}
}
