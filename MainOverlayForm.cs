using Deskplorer.Controls;
using Deskplorer.Models;
using Deskplorer.Services;

namespace Deskplorer
{
	public partial class MainOverlayForm : Form
	{
		private readonly PersistenceService _persistenceService;
		private readonly AppState _appState;
		private readonly Dictionary<Guid, FolderIconControl> _folderIcons = new();
		private readonly ContextMenuStrip _overlayMenu;
		private readonly ToolStripMenuItem _movementMenuItem;
      private readonly ToolStripMenuItem _renameFolderMenuItem;
      private readonly ToolStripMenuItem _removeFolderMenuItem;
      private readonly ToolStripMenuItem _hoverOpenMenuItem;
      private readonly ToolStripMenuItem _changeFolderIconMenuItem;
      private readonly ToolStripMenuItem _chooseSystemFolderIconMenuItem;
		private readonly ToolStripMenuItem _newFolderMenuItem;
      private readonly System.Windows.Forms.Timer _hoverOpenTimer;
		private readonly System.Windows.Forms.Timer _hoverCloseTimer;

		private FolderWindowForm? _folderWindow;
		private DeskFolder? _openedFolder;
		private DeskFolder? _menuFolder;
		private FolderIconControl? _hoverCandidateIcon;
		private bool _hoverOpenEnabled;

		private bool _isDraggingIcon;
		private bool _suppressNextIconClick;
		private FolderIconControl? _dragIcon;
		private Point _dragStartMouseScreen;
		private Point _dragStartIconLocation;

		public MainOverlayForm()
		{
			InitializeComponent();
			KeyPreview = true;

			_persistenceService = new PersistenceService();
			_appState = _persistenceService.Load();

			if (_appState.Folders.Count == 0)
			{
				_appState.Folders.Add(CreateDefaultFolder("deskplorer folder", GetDefaultFolderScreenLocation()));
				_persistenceService.Save(_appState);
			}

			_hoverOpenEnabled = _appState.HoverOpenEnabled;

			_overlayMenu = new ContextMenuStrip();
			_movementMenuItem = new ToolStripMenuItem();
         _renameFolderMenuItem = new ToolStripMenuItem("Rename Folder");
         _removeFolderMenuItem = new ToolStripMenuItem("Remove Folder");
         _hoverOpenMenuItem = new ToolStripMenuItem("Enable Hover Open");
         _changeFolderIconMenuItem = new ToolStripMenuItem("Change Folder Icon");
         _chooseSystemFolderIconMenuItem = new ToolStripMenuItem("Choose System Folder Icon");
			_newFolderMenuItem = new ToolStripMenuItem("New Folder");
       _hoverOpenTimer = new System.Windows.Forms.Timer { Interval = 220 };
			_hoverCloseTimer = new System.Windows.Forms.Timer { Interval = 250 };
			_hoverOpenTimer.Tick += HoverOpenTimer_Tick;
			_hoverCloseTimer.Tick += HoverCloseTimer_Tick;

			InitializeOverlayMenu();
			RenderAllFolderIcons();
        MouseDown += MainOverlayForm_MouseDown;
			KeyDown += MainOverlayForm_KeyDown;
			FormClosing += MainOverlayForm_FormClosing;
		}

		private void InitializeOverlayMenu()
		{
			_movementMenuItem.Click += MovementMenuItem_Click;
         _renameFolderMenuItem.Click += RenameFolderMenuItem_Click;
        _removeFolderMenuItem.Click += RemoveFolderMenuItem_Click;
        _hoverOpenMenuItem.Click += HoverOpenMenuItem_Click;
        _changeFolderIconMenuItem.Click += ChangeFolderIconMenuItem_Click;
        _chooseSystemFolderIconMenuItem.Click += ChooseSystemFolderIconMenuItem_Click;
			_newFolderMenuItem.Click += NewFolderMenuItem_Click;
			_overlayMenu.Opening += OverlayMenu_Opening;

			_overlayMenu.Items.AddRange(new ToolStripItem[]
			{
				_movementMenuItem,
            _renameFolderMenuItem,
           _removeFolderMenuItem,
            _changeFolderIconMenuItem,
            _chooseSystemFolderIconMenuItem,
           _hoverOpenMenuItem,
				new ToolStripSeparator(),
				_newFolderMenuItem
			});

			ContextMenuStrip = _overlayMenu;
		}

		private void OverlayMenu_Opening(object? sender, System.ComponentModel.CancelEventArgs e)
		{
			_menuFolder = ResolveFolderFromControl(_overlayMenu.SourceControl);
			_movementMenuItem.Enabled = _menuFolder is not null;
         _renameFolderMenuItem.Enabled = _menuFolder is not null;
       _removeFolderMenuItem.Enabled = _menuFolder is not null;
       _changeFolderIconMenuItem.Enabled = _menuFolder is not null;
       _chooseSystemFolderIconMenuItem.Enabled = _menuFolder is not null;
			_movementMenuItem.Text = _menuFolder is null
				? "Allow Folder Movement"
				: _menuFolder.Locked ? "Allow Folder Movement" : "Disable Folder Movement";
        _hoverOpenMenuItem.Text = _hoverOpenEnabled ? "Disable Hover Open" : "Enable Hover Open";
		}

		private void RenderAllFolderIcons()
		{
			foreach (var folder in _appState.Folders)
			{
				RenderFolderIcon(folder);
			}
		}

		private void RenderFolderIcon(DeskFolder folder)
		{
			var icon = new FolderIconControl
			{
				FolderName = folder.Name,
				ItemCount = folder.Items.Count,
				Tag = folder
			};

			icon.Click += FolderIcon_Click;
			icon.MouseDown += FolderIcon_MouseDown;
			icon.MouseMove += FolderIcon_MouseMove;
			icon.MouseUp += FolderIcon_MouseUp;
        icon.MouseEnter += FolderIcon_MouseEnter;
			icon.MouseLeave += FolderIcon_MouseLeave;
			icon.ContextMenuStrip = _overlayMenu;

			foreach (Control child in icon.Controls)
			{
				child.MouseDown += FolderIcon_MouseDown;
				child.MouseMove += FolderIcon_MouseMove;
				child.MouseUp += FolderIcon_MouseUp;
          child.MouseEnter += FolderIcon_MouseEnter;
				child.MouseLeave += FolderIcon_MouseLeave;
				child.ContextMenuStrip = _overlayMenu;
			}

			icon.Location = ScreenToOverlay(folder.ClosedLocation);
			ApplyMovementVisual(folder, icon);
			ApplyFolderIcon(folder, icon);

			_folderIcons[folder.Id] = icon;
			Controls.Add(icon);
		}

		private void FolderIcon_Click(object? sender, EventArgs e)
		{
			if (_suppressNextIconClick)
			{
				_suppressNextIconClick = false;
				return;
			}

			if (sender is not FolderIconControl icon || icon.Tag is not DeskFolder folder)
			{
				return;
			}

			OpenFolderForIcon(icon, triggeredByHover: false);
		}

		private void OpenFolderForIcon(FolderIconControl icon, bool triggeredByHover)
		{
			if (icon.Tag is not DeskFolder folder)
			{
				return;
			}

			if (_folderWindow is not null && !_folderWindow.IsDisposed)
			{
				if (_openedFolder?.Id == folder.Id)
				{
             if (!triggeredByHover)
					{
						CloseOpenFolderWindow();
					}
					return;
				}

           CloseOpenFolderWindow();
			}

        var iconScreenBounds = icon.RectangleToScreen(icon.ClientRectangle);
			_folderWindow = new FolderWindowForm(folder, iconScreenBounds);
        _folderWindow.FolderItemsChanged += FolderWindow_FolderItemsChanged;
			_folderWindow.StartPosition = FormStartPosition.Manual;
         _folderWindow.Location = CalculateFolderWindowLocation(iconScreenBounds, _folderWindow.Size);
        _folderWindow.KeyPreview = true;
			_folderWindow.KeyDown += FolderWindow_KeyDown;
			_folderWindow.Deactivate += FolderWindow_Deactivate;
        _folderWindow.SetSharedContextMenu(_overlayMenu);
			_folderWindow.FormClosed += FolderWindow_FormClosed;
			_folderWindow.Show(this);
			_openedFolder = folder;

			if (_hoverOpenEnabled)
			{
				_hoverCloseTimer.Stop();
				_hoverCloseTimer.Start();
			}
		}

		private void HoverOpenMenuItem_Click(object? sender, EventArgs e)
		{
			_hoverOpenEnabled = !_hoverOpenEnabled;
       _appState.HoverOpenEnabled = _hoverOpenEnabled;
			_hoverOpenMenuItem.Text = _hoverOpenEnabled ? "Disable Hover Open" : "Enable Hover Open";

			if (_hoverOpenEnabled)
			{
				CloseOpenFolderWindow();
			}

			if (!_hoverOpenEnabled)
			{
				_hoverCandidateIcon = null;
				_hoverOpenTimer.Stop();
				_hoverCloseTimer.Stop();
        }
			else if (_folderWindow is not null && !_folderWindow.IsDisposed)
			{
				_hoverCloseTimer.Stop();
				_hoverCloseTimer.Start();
			}

			SaveState();
		}

		private void ChangeFolderIconMenuItem_Click(object? sender, EventArgs e)
		{
			if (_menuFolder is null)
			{
				return;
			}

			using var dialog = new OpenFileDialog
			{
				Title = "Choose folder icon",
				Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp;*.ico)|*.png;*.jpg;*.jpeg;*.bmp;*.ico|All files (*.*)|*.*",
				CheckFileExists = true,
				Multiselect = false
			};

			if (dialog.ShowDialog(this) != DialogResult.OK)
			{
				return;
			}

			_menuFolder.IconPath = dialog.FileName;
			RefreshFolderIcon(_menuFolder);
			SaveState();
		}

		private void ChooseSystemFolderIconMenuItem_Click(object? sender, EventArgs e)
		{
			if (_menuFolder is null)
			{
				return;
			}

			using var browser = new ImageResIconBrowserForm();
			if (browser.ShowDialog(this) != DialogResult.OK || !browser.SelectedIconIndex.HasValue)
			{
				return;
			}

			_menuFolder.IconPath = $"imageres://{browser.SelectedIconIndex.Value}";
			RefreshFolderIcon(_menuFolder);
			SaveState();
		}

		private void FolderIcon_MouseEnter(object? sender, EventArgs e)
		{
			var icon = ResolveFolderIconFromSender(sender);
			if (icon is null)
			{
				return;
			}

			_hoverCloseTimer.Stop();

			if (!_hoverOpenEnabled || _isDraggingIcon)
			{
				return;
			}

			_hoverCandidateIcon = icon;
			_hoverOpenTimer.Stop();
			_hoverOpenTimer.Start();
		}

		private void FolderIcon_MouseLeave(object? sender, EventArgs e)
		{
			if (!_hoverOpenEnabled)
			{
				return;
			}

			var icon = ResolveFolderIconFromSender(sender);
			if (icon is null)
			{
				return;
			}

			if (IsCursorOverControl(icon))
			{
				return;
			}

			if (_hoverCandidateIcon == icon)
			{
				_hoverOpenTimer.Stop();
				_hoverCandidateIcon = null;
			}

			_hoverCloseTimer.Stop();
			_hoverCloseTimer.Start();
		}

		private void HoverOpenTimer_Tick(object? sender, EventArgs e)
		{
			_hoverOpenTimer.Stop();

			if (!_hoverOpenEnabled || _hoverCandidateIcon is null)
			{
				return;
			}

			if (!IsCursorOverControl(_hoverCandidateIcon))
			{
				_hoverCandidateIcon = null;
				return;
			}

			OpenFolderForIcon(_hoverCandidateIcon, triggeredByHover: true);
        _hoverCloseTimer.Stop();
			_hoverCloseTimer.Start();
		}

		private void HoverCloseTimer_Tick(object? sender, EventArgs e)
		{
			if (!_hoverOpenEnabled || _folderWindow is null || _folderWindow.IsDisposed || _openedFolder is null)
			{
           _hoverCloseTimer.Stop();
				return;
			}

			if (_folderWindow.IsInteractingWithWindow)
			{
				return;
			}

			if (_folderIcons.TryGetValue(_openedFolder.Id, out var icon) && IsCursorOverControl(icon))
			{
				return;
			}

			if (IsCursorOverControl(_folderWindow))
			{
				return;
			}

         _hoverCloseTimer.Stop();
			CloseOpenFolderWindow();
		}

		private static bool IsCursorOverControl(Control control)
		{
			if (control.IsDisposed || !control.Visible)
			{
				return false;
			}

			var rect = control.RectangleToScreen(control.ClientRectangle);
			return rect.Contains(Cursor.Position);
		}

		private void MainOverlayForm_MouseDown(object? sender, MouseEventArgs e)
		{
       if ((ModifierKeys & (Keys.Control | Keys.Alt)) != 0 && e.Button == MouseButtons.Right)
			{
				_menuFolder = null;
				_overlayMenu.Show(this, PointToClient(Cursor.Position));
				return;
			}

			if (e.Button == MouseButtons.Left && _folderWindow is not null && !_folderWindow.IsDisposed)
			{
				CloseOpenFolderWindow();
			}
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (keyData == (Keys.Control | Keys.Apps) || keyData == (Keys.Alt | Keys.Apps))
			{
				_menuFolder = null;
				_overlayMenu.Show(this, PointToClient(Cursor.Position));
				return true;
			}

			return base.ProcessCmdKey(ref msg, keyData);
		}

		private void MainOverlayForm_KeyDown(object? sender, KeyEventArgs e)
		{
			if (e.KeyCode != Keys.Escape)
			{
				return;
			}

			if (_folderWindow is not null && !_folderWindow.IsDisposed)
			{
				CloseOpenFolderWindow();
				e.Handled = true;
				e.SuppressKeyPress = true;
			}
		}

		private void FolderWindow_KeyDown(object? sender, KeyEventArgs e)
		{
			if (e.KeyCode != Keys.Escape)
			{
				return;
			}

			CloseOpenFolderWindow();
			e.Handled = true;
			e.SuppressKeyPress = true;
		}

		private void FolderWindow_Deactivate(object? sender, EventArgs e)
		{
         if (_hoverOpenEnabled)
			{
				return;
			}

			if (_folderWindow is null || _folderWindow.IsDisposed)
			{
				return;
			}

			CloseOpenFolderWindow();
		}

		private void CloseOpenFolderWindow()
		{
			if (_folderWindow is null || _folderWindow.IsDisposed)
			{
				return;
			}

			_folderWindow.KeyDown -= FolderWindow_KeyDown;
			_folderWindow.Deactivate -= FolderWindow_Deactivate;
			_folderWindow.FolderItemsChanged -= FolderWindow_FolderItemsChanged;
        _hoverOpenTimer.Stop();
			_hoverCloseTimer.Stop();
			_hoverCandidateIcon = null;
			_folderWindow.Close();
		}

		private void MovementMenuItem_Click(object? sender, EventArgs e)
		{
			if (_menuFolder is null)
			{
				return;
			}

			_menuFolder.Locked = !_menuFolder.Locked;
       if (!_menuFolder.Locked)
			{
				CloseOpenFolderWindow();
			}

			if (_folderIcons.TryGetValue(_menuFolder.Id, out var icon))
			{
				ApplyMovementVisual(_menuFolder, icon);
			}

			SaveState();
		}

		private void RenameFolderMenuItem_Click(object? sender, EventArgs e)
		{
			if (_menuFolder is null)
			{
				return;
			}

			if (!PromptDialog.TryShow(this, "Rename Folder", "Folder name:", _menuFolder.Name, out var newName))
			{
				return;
			}

			newName = newName.Trim();
			if (string.IsNullOrWhiteSpace(newName))
			{
				MessageBox.Show(this, "Folder name cannot be empty.", "Deskplorer", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			_menuFolder.Name = newName;
			RefreshFolderIcon(_menuFolder);
			if (_openedFolder?.Id == _menuFolder.Id && _folderWindow is not null && !_folderWindow.IsDisposed)
			{
				_folderWindow.RefreshFolderHeader();
			}

			SaveState();
		}

		private void NewFolderMenuItem_Click(object? sender, EventArgs e)
		{
			var nextIndex = _appState.Folders.Count + 1;
			var name = $"deskplorer folder {nextIndex}";
			var location = Cursor.Position;
			var folder = CreateDefaultFolder(name, location);
			folder.Items.Clear();

			_appState.Folders.Add(folder);
			RenderFolderIcon(folder);
			SaveState();
		}

		private void RemoveFolderMenuItem_Click(object? sender, EventArgs e)
		{
			if (_menuFolder is null)
			{
				return;
			}

			if (MessageBox.Show(this,
				$"Remove folder '{_menuFolder.Name}'?",
				"Deskplorer",
				MessageBoxButtons.YesNo,
				MessageBoxIcon.Question) != DialogResult.Yes)
			{
				return;
			}

			if (_openedFolder?.Id == _menuFolder.Id && _folderWindow is not null && !_folderWindow.IsDisposed)
			{
				_folderWindow.FolderItemsChanged -= FolderWindow_FolderItemsChanged;
				_folderWindow.Close();
				_folderWindow = null;
				_openedFolder = null;
			}

			if (_folderIcons.TryGetValue(_menuFolder.Id, out var icon))
			{
				Controls.Remove(icon);
				icon.Dispose();
				_folderIcons.Remove(_menuFolder.Id);
			}

			_appState.Folders.RemoveAll(f => f.Id == _menuFolder.Id);
			_menuFolder = null;
			SaveState();
		}

		private DeskFolder CreateDefaultFolder(string name, Point closedLocation)
		{
			return new DeskFolder
			{
				Name = name,
				ClosedLocation = closedLocation,
				OpenSize = new Size(420, 230),
				MonitorId = Screen.FromPoint(closedLocation).DeviceName,
				Locked = false,
           AutoArrange = false,
				ItemIconSize = 48,
				Items = CreateDefaultItems()
			};
		}

		private void FolderWindow_FolderItemsChanged(object? sender, EventArgs e)
		{
			if (_openedFolder is not null)
			{
				RefreshFolderIcon(_openedFolder);
			}

			SaveState();
		}

		private void RefreshFolderIcon(DeskFolder folder)
		{
			if (_folderIcons.TryGetValue(folder.Id, out var icon))
			{
				icon.FolderName = folder.Name;
				icon.ItemCount = folder.Items.Count;
           ApplyFolderIcon(folder, icon);
			}
		}

		private static void ApplyFolderIcon(DeskFolder folder, FolderIconControl icon)
		{
         if (!string.IsNullOrWhiteSpace(folder.IconPath) && folder.IconPath.StartsWith("imageres://", StringComparison.OrdinalIgnoreCase))
			{
				if (int.TryParse(folder.IconPath[11..], out var index))
				{
					try
					{
						var cache = new IconCacheService();
						icon.IconImage = cache.GetImageResIconImage(index);
						return;
					}
					catch
					{
					}
				}
			}

       if (string.IsNullOrWhiteSpace(folder.IconPath))
			{
				icon.IconImage = null;
				return;
			}

			if (string.Equals(folder.IconPath, "__default_folder__", StringComparison.OrdinalIgnoreCase))
			{
				icon.IconImage = null;
				return;
			}

			if (!string.IsNullOrWhiteSpace(folder.IconPath) && File.Exists(folder.IconPath))
			{
            if (string.Equals(Path.GetExtension(folder.IconPath), ".ico", StringComparison.OrdinalIgnoreCase))
				{
					try
					{
						using var iconFile = new Icon(folder.IconPath, new Size(32, 32));
						icon.IconImage = iconFile.ToBitmap();
						return;
					}
					catch
					{
					}
				}

				try
				{
					using var image = Image.FromFile(folder.IconPath);
					icon.IconImage = new Bitmap(image);
					return;
				}
				catch
				{
				}
			}

         icon.IconImage = null;
		}

		private List<DeskItem> CreateDefaultItems()
		{
			return new List<DeskItem>
			{
				new() { DisplayName = "Notepad", FilePath = "notepad.exe" },
				new() { DisplayName = "Explorer", FilePath = "explorer.exe" },
				new() { DisplayName = "Paint", FilePath = "mspaint.exe" },
				new() { DisplayName = "Calculator", FilePath = "calc.exe" },
				new() { DisplayName = "Command Prompt", FilePath = "cmd.exe" },
				new() { DisplayName = "PowerShell", FilePath = "powershell.exe" },
				new() { DisplayName = "Control Panel", FilePath = "control.exe" }
			};
		}

		private Point GetDefaultFolderScreenLocation()
		{
			var primary = Screen.PrimaryScreen?.Bounds ?? SystemInformation.VirtualScreen;
			return new Point(primary.Left + 40, primary.Top + 300);
		}

		private Point ScreenToOverlay(Point screenPoint)
		{
			var virtualBounds = SystemInformation.VirtualScreen;
			return new Point(screenPoint.X - virtualBounds.Left, screenPoint.Y - virtualBounds.Top);
		}

		private void ApplyMovementVisual(DeskFolder folder, FolderIconControl icon)
		{
			var cursor = folder.Locked ? Cursors.Hand : Cursors.SizeAll;
			icon.Cursor = cursor;
			foreach (Control child in icon.Controls)
			{
				child.Cursor = cursor;
			}

			icon.BackColor = folder.Locked ? Color.Transparent : Color.FromArgb(36, Color.White);
		}

		private void FolderIcon_MouseDown(object? sender, MouseEventArgs e)
		{
			if (e.Button != MouseButtons.Left)
			{
				return;
			}

			if (_folderWindow is not null && !_folderWindow.IsDisposed)
			{
				return;
			}

			var icon = ResolveFolderIconFromSender(sender);
			if (icon?.Tag is not DeskFolder folder || folder.Locked)
			{
				return;
			}

			_dragIcon = icon;
			_isDraggingIcon = true;
			_dragStartMouseScreen = MousePosition;
			_dragStartIconLocation = icon.Location;
		}

		private void FolderIcon_MouseMove(object? sender, MouseEventArgs e)
		{
			if (!_isDraggingIcon || _dragIcon is null)
			{
				return;
			}

			var currentMouse = MousePosition;
			var deltaX = currentMouse.X - _dragStartMouseScreen.X;
			var deltaY = currentMouse.Y - _dragStartMouseScreen.Y;

			if (Math.Abs(deltaX) > 2 || Math.Abs(deltaY) > 2)
			{
				_suppressNextIconClick = true;
			}

			var proposedX = _dragStartIconLocation.X + deltaX;
			var proposedY = _dragStartIconLocation.Y + deltaY;
			var maxX = Math.Max(0, ClientSize.Width - _dragIcon.Width);
			var maxY = Math.Max(0, ClientSize.Height - _dragIcon.Height);

			_dragIcon.Location = new Point(
				Math.Clamp(proposedX, 0, maxX),
				Math.Clamp(proposedY, 0, maxY));
		}

		private void FolderIcon_MouseUp(object? sender, MouseEventArgs e)
		{
			if (e.Button != MouseButtons.Left || !_isDraggingIcon)
			{
				return;
			}

			_isDraggingIcon = false;
			_dragIcon = null;
			SaveState();
		}

		private FolderIconControl? ResolveFolderIconFromSender(object? sender)
		{
			if (sender is FolderIconControl icon)
			{
				return icon;
			}

			if (sender is Control control && control.Parent is FolderIconControl parentIcon)
			{
				return parentIcon;
			}

			return null;
		}

		private DeskFolder? ResolveFolderFromControl(Control? sourceControl)
		{
			if (sourceControl is FolderIconControl icon && icon.Tag is DeskFolder folderFromIcon)
			{
				return folderFromIcon;
			}

			if (sourceControl?.Parent is FolderIconControl parentIcon && parentIcon.Tag is DeskFolder folderFromParent)
			{
				return folderFromParent;
			}

			return null;
		}

		private void FolderWindow_FormClosed(object? sender, FormClosedEventArgs e)
		{
			if (_folderWindow is not null && _openedFolder is not null)
			{
            _folderWindow.KeyDown -= FolderWindow_KeyDown;
				_folderWindow.Deactivate -= FolderWindow_Deactivate;
				_folderWindow.FolderItemsChanged -= FolderWindow_FolderItemsChanged;
				_openedFolder.OpenSize = _folderWindow.ClientSize;
           RefreshFolderIcon(_openedFolder);
			}

			_folderWindow = null;
			_openedFolder = null;
			SaveState();
		}

      private static Point CalculateFolderWindowLocation(Rectangle iconScreenBounds, Size windowSize)
		{
         var screen = Screen.FromPoint(iconScreenBounds.Location);
			var bounds = screen.WorkingArea;

         var x = iconScreenBounds.Left - ((windowSize.Width - iconScreenBounds.Width) / 2);
			var y = iconScreenBounds.Top - ((windowSize.Height - iconScreenBounds.Height) / 2);

			x = Math.Clamp(x, bounds.Left, bounds.Right - windowSize.Width);
			y = Math.Clamp(y, bounds.Top, bounds.Bottom - windowSize.Height);

			return new Point(x, y);
		}

		private void MainOverlayForm_FormClosing(object? sender, FormClosingEventArgs e)
		{
			SaveState();
		}

		private void SaveState()
		{
			foreach (var icon in _folderIcons.Values)
			{
				if (icon.Tag is not DeskFolder folder)
				{
					continue;
				}

				folder.ClosedLocation = icon.PointToScreen(Point.Empty);
				folder.ItemIconSize = 48;
				folder.MonitorId = Screen.FromPoint(folder.ClosedLocation).DeviceName;
			}

			_persistenceService.Save(_appState);
		}
	}
}
