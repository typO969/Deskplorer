using Deskplorer.Controls;
using Deskplorer.Models;
using Deskplorer.Services;

namespace Deskplorer
{
	public partial class MainOverlayForm : Form
	{
		private readonly PersistenceService _persistenceService;
		private readonly AppState _appState;
		private readonly Dictionary<Guid, FolderIconControl> _folderIcons = [];
		private readonly ContextMenuStrip _desktopMenu;
		private readonly ToolStripMenuItem _lockMovementMenuItem;
		private readonly ToolStripMenuItem _addFolderMenuItem;
		private readonly ToolStripMenuItem _deleteFolderMenuItem;
		private readonly ToolStripMenuItem _renameFolderMenuItem;
		private readonly ToolStripMenuItem _folderIconMenuItem;
		private readonly ToolStripMenuItem _changeFolderIconMenuItem;
		private readonly ToolStripMenuItem _chooseSystemFolderIconMenuItem;
		private readonly ToolStripMenuItem _resetFolderIconMenuItem;
		private readonly ToolStripMenuItem _folderPropertiesMenuItem;
		private readonly ToolStripMenuItem _preferencesMenuItem;
		private readonly ToolStripMenuItem _hoverOpenMenuItem;
		private readonly ToolStripMenuItem _hoverCloseBehaviorMenuItem;
		private readonly ToolStripMenuItem _hoverDetectionBorderMenuItem;
		private readonly ToolStripMenuItem _hoverCloseTimeoutMenuItem;
		private readonly ToolStripMenuItem _animationsMenuItem;
		private readonly ToolStripMenuItem _tileDefaultsMenuItem;
		private readonly ToolStripMenuItem _defaultAutoArrangeMenuItem;
		private readonly ToolStripMenuItem _tileSizeMenuItem;
		private readonly ToolStripMenuItem _tileSizeSmallMenuItem;
		private readonly ToolStripMenuItem _tileSizeMediumMenuItem;
		private readonly ToolStripMenuItem _tileSizeLargeMenuItem;
		private readonly ToolStripMenuItem _customizeFolderViewMenuItem;
		private readonly ToolStripMenuItem _hideAllIconsMenuItem;
		private readonly ToolStripMenuItem _restoreHiddenIconsMenuItem;
		private readonly ToolStripMenuItem _refreshDeskplorerMenuItem;
		private readonly ToolStripMenuItem _quitDeskplorerMenuItem;
		private readonly System.Windows.Forms.Timer _hoverOpenTimer;
		private readonly System.Windows.Forms.Timer _hoverCloseTimer;

		private FolderWindowForm? _folderWindow;
		private DeskFolder? _openedFolder;
		private DeskFolder? _menuFolder;
		private FolderIconControl? _hoverCandidateIcon;
		private long _hoverCloseOutsideSinceTick = -1;
		private bool _hoverOpenEnabled;
		private bool _animationsEnabled;

		private const int MinHoverDetectionBorder = 0;
		private const int MaxHoverDetectionBorder = 200;
		private const int MinHoverCloseTimeoutSeconds = 0;
		private const int MaxHoverCloseTimeoutSeconds = 10;

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
			_animationsEnabled = _appState.AnimationsEnabled;
			_appState.HoverDetectionBorderDistance = Math.Clamp(_appState.HoverDetectionBorderDistance, MinHoverDetectionBorder, MaxHoverDetectionBorder);
			_appState.HoverCloseTimeoutSeconds = Math.Clamp(_appState.HoverCloseTimeoutSeconds, MinHoverCloseTimeoutSeconds, MaxHoverCloseTimeoutSeconds);

			_desktopMenu = new ContextMenuStrip();
			_lockMovementMenuItem = new ToolStripMenuItem();
			_addFolderMenuItem = new ToolStripMenuItem("Add Folder");
			_deleteFolderMenuItem = new ToolStripMenuItem("Delete Folder");
			_renameFolderMenuItem = new ToolStripMenuItem("Rename Folder");
			_folderIconMenuItem = new ToolStripMenuItem("Folder Icon");
			_changeFolderIconMenuItem = new ToolStripMenuItem("Change Icon...");
			_chooseSystemFolderIconMenuItem = new ToolStripMenuItem("Choose System Folder Icon");
			_resetFolderIconMenuItem = new ToolStripMenuItem("Reset Icon");
			_folderPropertiesMenuItem = new ToolStripMenuItem("Folder Properties...");
			_preferencesMenuItem = new ToolStripMenuItem("Preferences");
			_hoverOpenMenuItem = new ToolStripMenuItem("Enable Auto Open/Close Folder (Hover)");
			_hoverCloseBehaviorMenuItem = new ToolStripMenuItem("Hover close behavior");
			_hoverDetectionBorderMenuItem = new ToolStripMenuItem("Detection border...");
			_hoverCloseTimeoutMenuItem = new ToolStripMenuItem("Close timeout...");
			_animationsMenuItem = new ToolStripMenuItem("Enable Animations");
			_tileDefaultsMenuItem = new ToolStripMenuItem("Tile defaults");
			_defaultAutoArrangeMenuItem = new ToolStripMenuItem("Manual/Auto organize tiles");
			_tileSizeMenuItem = new ToolStripMenuItem("Tile size");
			_tileSizeSmallMenuItem = new ToolStripMenuItem("Small");
			_tileSizeMediumMenuItem = new ToolStripMenuItem("Medium (default)");
			_tileSizeLargeMenuItem = new ToolStripMenuItem("Large");
			_customizeFolderViewMenuItem = new ToolStripMenuItem("Customize Folder View...");
			_hideAllIconsMenuItem = new ToolStripMenuItem("Hide All Folder Icons");
			_restoreHiddenIconsMenuItem = new ToolStripMenuItem("Restore Hidden Icons");
			_refreshDeskplorerMenuItem = new ToolStripMenuItem("Refresh Deskplorer");
			_quitDeskplorerMenuItem = new ToolStripMenuItem("Quit Deskplorer");
			_hoverOpenTimer = new System.Windows.Forms.Timer { Interval = 220 };
			_hoverCloseTimer = new System.Windows.Forms.Timer { Interval = 100 };
			_hoverOpenTimer.Tick += HoverOpenTimer_Tick;
			_hoverCloseTimer.Tick += HoverCloseTimer_Tick;

			InitializeDesktopMenu();
			RenderAllFolderIcons();
			MouseDown += MainOverlayForm_MouseDown;
			KeyDown += MainOverlayForm_KeyDown;
			FormClosing += MainOverlayForm_FormClosing;
		}

		private void InitializeDesktopMenu()
		{
			_lockMovementMenuItem.Click += MovementMenuItem_Click;
			_addFolderMenuItem.Click += NewFolderMenuItem_Click;
			_deleteFolderMenuItem.Click += RemoveFolderMenuItem_Click;
			_renameFolderMenuItem.Click += RenameFolderMenuItem_Click;
			_changeFolderIconMenuItem.Click += ChangeFolderIconMenuItem_Click;
			_chooseSystemFolderIconMenuItem.Click += ChooseSystemFolderIconMenuItem_Click;
			_resetFolderIconMenuItem.Click += ResetFolderIconMenuItem_Click;
			_folderPropertiesMenuItem.Click += FolderPropertiesMenuItem_Click;
			_hoverOpenMenuItem.Click += HoverOpenMenuItem_Click;
			_hoverDetectionBorderMenuItem.Enabled = false;
			_hoverCloseTimeoutMenuItem.Enabled = false;
			_animationsMenuItem.Click += AnimationsMenuItem_Click;
			_defaultAutoArrangeMenuItem.Click += DefaultAutoArrangeMenuItem_Click;
			_tileSizeSmallMenuItem.Click += TileSizeSmallMenuItem_Click;
			_tileSizeMediumMenuItem.Click += TileSizeMediumMenuItem_Click;
			_tileSizeLargeMenuItem.Click += TileSizeLargeMenuItem_Click;
			_customizeFolderViewMenuItem.Click += CustomizeFolderViewMenuItem_Click;
			_hideAllIconsMenuItem.Click += HideAllIconsMenuItem_Click;
			_restoreHiddenIconsMenuItem.Click += RestoreHiddenIconsMenuItem_Click;
			_refreshDeskplorerMenuItem.Click += RefreshDeskplorerMenuItem_Click;
			_quitDeskplorerMenuItem.Click += QuitDeskplorerMenuItem_Click;
			_desktopMenu.Opening += DesktopMenu_Opening;

			_folderIconMenuItem.DropDownItems.AddRange(
			[
				_changeFolderIconMenuItem,
				_chooseSystemFolderIconMenuItem,
				_resetFolderIconMenuItem
			]);

			_tileSizeMenuItem.DropDownItems.AddRange(
			[
				_tileSizeSmallMenuItem,
				_tileSizeMediumMenuItem,
				_tileSizeLargeMenuItem
			]);

			_tileDefaultsMenuItem.DropDownItems.AddRange(
			[
				_defaultAutoArrangeMenuItem,
				_tileSizeMenuItem
			]);

			_preferencesMenuItem.DropDownItems.AddRange(
			[
				_hoverOpenMenuItem,
				_hoverCloseBehaviorMenuItem,
				_animationsMenuItem,
				new ToolStripSeparator(),
				_tileDefaultsMenuItem,
				_customizeFolderViewMenuItem
			]);

			_hoverCloseBehaviorMenuItem.DropDownItems.AddRange([
				_hoverDetectionBorderMenuItem,
				_hoverCloseTimeoutMenuItem
			]);

			_desktopMenu.Items.AddRange(
			[
				_lockMovementMenuItem,
				new ToolStripSeparator(),
				_addFolderMenuItem,
				_deleteFolderMenuItem,
				new ToolStripSeparator(),
				_renameFolderMenuItem,
				_folderIconMenuItem,
				_folderPropertiesMenuItem,
				new ToolStripSeparator(),
				_preferencesMenuItem,
				new ToolStripSeparator(),
				_hideAllIconsMenuItem,
				_restoreHiddenIconsMenuItem,
				_refreshDeskplorerMenuItem,
				new ToolStripSeparator(),
				_quitDeskplorerMenuItem
			]);

			ContextMenuStrip = _desktopMenu;
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			RefreshVirtualBounds();

			// Actively adapt to users plugging/unplugging monitors while the app runs
			Microsoft.Win32.SystemEvents.DisplaySettingsChanged += (s, ev) => RefreshVirtualBounds();
		}

		private void RefreshVirtualBounds()
		{
			// CRITICAL: Overrides the internal OS-level lock on WinForms maximum window sizes
			this.MaximumSize = new Size(int.MaxValue, int.MaxValue);

			var virtualBounds = SystemInformation.VirtualScreen;
			this.Location = virtualBounds.Location;
			this.Size = virtualBounds.Size;
		}

		private void DesktopMenu_Opening(object? sender, System.ComponentModel.CancelEventArgs e)
		{
			_menuFolder = ResolveFolderFromControl(_desktopMenu.SourceControl);
			_lockMovementMenuItem.Enabled = _menuFolder is not null;
			_deleteFolderMenuItem.Enabled = _menuFolder is not null;
			_renameFolderMenuItem.Enabled = _menuFolder is not null;
			_folderIconMenuItem.Enabled = _menuFolder is not null;
			_changeFolderIconMenuItem.Enabled = _menuFolder is not null;
			_chooseSystemFolderIconMenuItem.Enabled = _menuFolder is not null;
			_resetFolderIconMenuItem.Enabled = _menuFolder is not null && !string.IsNullOrWhiteSpace(_menuFolder.IconPath);
			_folderPropertiesMenuItem.Enabled = _menuFolder is not null;
			_lockMovementMenuItem.Text = _menuFolder is null
				? "Unlock/Lock Folder Position"
				: _menuFolder.Locked ? "Unlock Folder Position" : "Lock Folder Position";
			_hoverOpenMenuItem.Text = _hoverOpenEnabled
				? "Disable Auto Open/Close Folder (Hover)"
				: "Enable Auto Open/Close Folder (Hover)";
			_hoverDetectionBorderMenuItem.Text = $"Detection border: {_appState.HoverDetectionBorderDistance}px";
			_hoverCloseTimeoutMenuItem.Text = $"Close timeout: {_appState.HoverCloseTimeoutSeconds}s";
			_animationsMenuItem.Text = _animationsEnabled ? "Disable Animations" : "Enable Animations";
			_defaultAutoArrangeMenuItem.Text = _appState.DefaultAutoArrange ? "Auto organize tiles" : "Manual organize tiles";
			_tileSizeSmallMenuItem.Checked = _appState.DefaultTileIconSize == 32;
			_tileSizeMediumMenuItem.Checked = _appState.DefaultTileIconSize == 48;
			_tileSizeLargeMenuItem.Checked = _appState.DefaultTileIconSize == 64;
			_hideAllIconsMenuItem.Enabled = _appState.Folders.Any(folder => !folder.IsHidden);
			_restoreHiddenIconsMenuItem.Enabled = _appState.Folders.Any(folder => folder.IsHidden);
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
			EnableFolderIconDropSupport(icon);
			icon.ContextMenuStrip = _desktopMenu;

			foreach (Control child in icon.Controls)
			{
				child.MouseDown += FolderIcon_MouseDown;
				child.MouseMove += FolderIcon_MouseMove;
				child.MouseUp += FolderIcon_MouseUp;
				child.MouseEnter += FolderIcon_MouseEnter;
				child.MouseLeave += FolderIcon_MouseLeave;
				child.AllowDrop = true;
				child.DragEnter += FolderIcon_DragEnter;
				child.DragDrop += FolderIcon_DragDrop;
				child.ContextMenuStrip = _desktopMenu;
			}

			icon.Location = ScreenToOverlay(folder.ClosedLocation);
			ApplyMovementVisual(folder, icon);
			ApplyFolderIcon(folder, icon);

			_folderIcons[folder.Id] = icon;
			Controls.Add(icon);
			icon.Visible = !folder.IsHidden;
		}

		private void EnableFolderIconDropSupport(FolderIconControl icon)
		{
			icon.AllowDrop = true;
			icon.DragEnter += FolderIcon_DragEnter;
			icon.DragDrop += FolderIcon_DragDrop;
		}

		private void FolderIcon_DragEnter(object? sender, DragEventArgs e)
		{
			e.Effect = e.Data?.GetDataPresent(DataFormats.FileDrop) == true
				? DragDropEffects.Copy
				: DragDropEffects.None;
		}

		private void FolderIcon_DragDrop(object? sender, DragEventArgs e)
		{
			if (sender is not Control control)
			{
				return;
			}

			var folderIcon = ResolveFolderIconFromSender(control);
			if (folderIcon?.Tag is not DeskFolder folder)
			{
				return;
			}

			if (e.Data?.GetData(DataFormats.FileDrop) is not string[] droppedPaths || droppedPaths.Length == 0)
			{
				return;
			}

			var nextDropPoint = new Point(10, 10);
			var (addedCount, duplicateCount) = DeskFolderItemService.AddFilesToFolder(
				folder,
				droppedPaths,
				folder.AutoArrange ? null : _ =>
				{
					var point = nextDropPoint;
					nextDropPoint = new Point(nextDropPoint.X + 24, nextDropPoint.Y + 24);
					return point;
				},
				DeskFolderItemService.BuildCacheKey);

			if (addedCount == 0)
			{
				if (duplicateCount > 0)
				{
					MessageBox.Show(this, "Dropped items are already in this folder.", "Deskplorer", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
				return;
			}

			if (_openedFolder?.Id == folder.Id && _folderWindow is not null && !_folderWindow.IsDisposed)
			{
				_folderWindow.ReloadFolderData();
			}

			RefreshFolderIcon(folder);
			SaveState();

			if (duplicateCount > 0)
			{
				MessageBox.Show(this, $"Added {addedCount} item(s). {duplicateCount} duplicate item(s) were skipped.", "Deskplorer", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
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

			if (folder.RequiresPlacementBeforeOpen && !folder.Locked)
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
			_folderWindow.Location = CalculateFolderWindowLocation(iconScreenBounds, _folderWindow.Size, folder.OpenLocation);
			_folderWindow.KeyPreview = true;
			_folderWindow.KeyDown += FolderWindow_KeyDown;
			_folderWindow.Deactivate += FolderWindow_Deactivate;
			_folderWindow.SetSharedContextMenu(_desktopMenu);
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
			_hoverOpenMenuItem.Text = _hoverOpenEnabled
				  ? "Disable Auto Open/Close Folder (Hover)"
				  : "Enable Auto Open/Close Folder (Hover)";

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

		private void HoverDetectionBorderMenuItem_Click(object? sender, EventArgs e)
		{
			if (!TryPromptClampedInt(
				"Detection border",
				"Pixels to keep the folder open after the mouse leaves its edges:",
				_appState.HoverDetectionBorderDistance,
				MinHoverDetectionBorder,
				MaxHoverDetectionBorder,
				out var value))
			{
				return;
			}

			_appState.HoverDetectionBorderDistance = value;
			SaveState();
		}

		private void HoverCloseTimeoutMenuItem_Click(object? sender, EventArgs e)
		{
			if (!TryPromptClampedInt(
				"Close timeout",
				"Seconds to wait before auto-closing after the mouse leaves the folder area:",
				_appState.HoverCloseTimeoutSeconds,
				MinHoverCloseTimeoutSeconds,
				MaxHoverCloseTimeoutSeconds,
				out var value))
			{
				return;
			}

			_appState.HoverCloseTimeoutSeconds = value;
			SaveState();
		}

		private bool TryPromptClampedInt(string title, string prompt, int currentValue, int minValue, int maxValue, out int value)
		{
			value = currentValue;
			if (!PromptDialog.TryShow(this, title, prompt, currentValue.ToString(), out var input))
			{
				return false;
			}

			if (!int.TryParse(input, out var parsed))
			{
				MessageBox.Show(this, "Please enter a whole number.", "Deskplorer", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return false;
			}

			value = Math.Clamp(parsed, minValue, maxValue);
			return true;
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

		private void ApplyHiddenState()
		{
			foreach (var pair in _folderIcons)
			{
				if (pair.Value.Tag is not DeskFolder folder)
				{
					continue;
				}

				pair.Value.Visible = !folder.IsHidden;
			}
		}

		private void RebuildFolderIcons()
		{
			var icons = _folderIcons.Values.ToList();
			foreach (var icon in icons)
			{
				Controls.Remove(icon);
				icon.Dispose();
			}

			_folderIcons.Clear();
			RenderAllFolderIcons();
			ApplyHiddenState();
		}

		private void ResetFolderIconMenuItem_Click(object? sender, EventArgs e)
		{
			if (_menuFolder is null)
			{
				return;
			}

			_menuFolder.IconPath = string.Empty;
			RefreshFolderIcon(_menuFolder);
			SaveState();
		}

		private void FolderPropertiesMenuItem_Click(object? sender, EventArgs e)
		{
			if (_menuFolder is null)
			{
				return;
			}

			if (!FolderPropertiesDialog.TryShow(
				this,
				_menuFolder.Name,
				_appState.HoverDetectionBorderDistance,
				_appState.HoverCloseTimeoutSeconds,
				out var folderName,
				out var borderDistance,
				out var closeTimeoutSeconds))
			{
				return;
			}

			folderName = folderName.Trim();
			var folderChanged = false;
			if (!string.Equals(_menuFolder.Name, folderName, StringComparison.Ordinal))
			{
				_menuFolder.Name = folderName;
				RefreshFolderIcon(_menuFolder);
				if (_openedFolder?.Id == _menuFolder.Id && _folderWindow is not null && !_folderWindow.IsDisposed)
				{
					_folderWindow.RefreshFolderHeader();
				}

				folderChanged = true;
			}

			if (_appState.HoverDetectionBorderDistance != borderDistance)
			{
				_appState.HoverDetectionBorderDistance = borderDistance;
				folderChanged = true;
			}

			if (_appState.HoverCloseTimeoutSeconds != closeTimeoutSeconds)
			{
				_appState.HoverCloseTimeoutSeconds = closeTimeoutSeconds;
				folderChanged = true;
			}

			if (folderChanged)
			{
				SaveState();
			}
		}

		private void AnimationsMenuItem_Click(object? sender, EventArgs e)
		{
			_animationsEnabled = !_animationsEnabled;
			_appState.AnimationsEnabled = _animationsEnabled;
			SaveState();
		}

		private void DefaultAutoArrangeMenuItem_Click(object? sender, EventArgs e)
		{
			_appState.DefaultAutoArrange = !_appState.DefaultAutoArrange;
			SaveState();
		}

		private void TileSizeSmallMenuItem_Click(object? sender, EventArgs e)
		{
			SetDefaultTileIconSize(32);
		}

		private void TileSizeMediumMenuItem_Click(object? sender, EventArgs e)
		{
			SetDefaultTileIconSize(48);
		}

		private void TileSizeLargeMenuItem_Click(object? sender, EventArgs e)
		{
			SetDefaultTileIconSize(64);
		}

		private void SetDefaultTileIconSize(int size)
		{
			_appState.DefaultTileIconSize = size;
			SaveState();
		}

		private void CustomizeFolderViewMenuItem_Click(object? sender, EventArgs e)
		{
			MessageBox.Show(this, "Customize Folder View is not available yet.", "Deskplorer", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void HideAllIconsMenuItem_Click(object? sender, EventArgs e)
		{
			foreach (var folder in _appState.Folders)
			{
				folder.IsHidden = true;
			}

			ApplyHiddenState();
			SaveState();
		}

		private void RestoreHiddenIconsMenuItem_Click(object? sender, EventArgs e)
		{
			foreach (var folder in _appState.Folders)
			{
				folder.IsHidden = false;
			}

			ApplyHiddenState();
			SaveState();
		}

		private void RefreshDeskplorerMenuItem_Click(object? sender, EventArgs e)
		{
			RebuildFolderIcons();
		}

		private void QuitDeskplorerMenuItem_Click(object? sender, EventArgs e)
		{
			Close();
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
			_hoverCloseTimer.Start();
		}

		private void HoverCloseTimer_Tick(object? sender, EventArgs e)
		{
			if (!_hoverOpenEnabled || _folderWindow is null || _folderWindow.IsDisposed || _openedFolder is null)
			{
				_hoverCloseTimer.Stop();
				_hoverCloseOutsideSinceTick = -1;
				return;
			}

			if (_folderWindow.IsInteractingWithWindow)
			{
				_hoverCloseOutsideSinceTick = -1;
				return;
			}

			if (IsCursorWithinExpandedFolderProximity(_folderWindow))
			{
				_hoverCloseOutsideSinceTick = -1;
				return;
			}

			if (_openedFolder is not null && _folderIcons.TryGetValue(_openedFolder.Id, out var icon) && IsCursorOverControl(icon))
			{
				_hoverCloseOutsideSinceTick = -1;
				return;
			}

			if (IsCursorOverAnyOpenMenu())
			{
				_hoverCloseOutsideSinceTick = -1;
				return;
			}

			if (_appState.HoverCloseTimeoutSeconds <= 0)
			{
				_hoverCloseTimer.Stop();
				CloseOpenFolderWindow();
				return;
			}

			var now = Environment.TickCount64;
			if (_hoverCloseOutsideSinceTick < 0)
			{
				_hoverCloseOutsideSinceTick = now;
			}

			if (now - _hoverCloseOutsideSinceTick < _appState.HoverCloseTimeoutSeconds * 1000L)
			{
				return;
			}

			_hoverCloseTimer.Stop();
			_hoverCloseOutsideSinceTick = -1;
			CloseOpenFolderWindow();
		}

		private bool IsCursorWithinExpandedFolderProximity(FolderWindowForm folderWindow)
		{
			if (folderWindow.IsDisposed || !folderWindow.Visible)
			{
				return false;
			}

			var border = Math.Clamp(_appState.HoverDetectionBorderDistance, MinHoverDetectionBorder, MaxHoverDetectionBorder);
			var rect = folderWindow.RectangleToScreen(folderWindow.ClientRectangle);
			rect.Inflate(border, border);
			return rect.Contains(Cursor.Position);
		}

		private bool IsCursorOverAnyOpenMenu()
		{
			if (IsCursorOverDropDown(_desktopMenu))
			{
				return true;
			}

			return false;
		}

		private static bool IsCursorOverDropDown(ToolStripDropDown dropDown)
		{
			if (!dropDown.Visible)
			{
				return false;
			}

			if (dropDown.Bounds.Contains(Cursor.Position))
			{
				return true;
			}

			foreach (ToolStripItem item in dropDown.Items)
			{
				if (item is ToolStripDropDownItem dropDownItem && dropDownItem.DropDown is not null && IsCursorOverDropDown(dropDownItem.DropDown))
				{
					return true;
				}
			}

			return false;
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
				_desktopMenu.Show(this, PointToClient(Cursor.Position));
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
				_desktopMenu.Show(this, PointToClient(Cursor.Position));
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
			if (_hoverOpenEnabled) return;
			if (_folderWindow?.IsShowingDialog == true) return;
			CloseOpenFolderWindow();
		}

		private void CloseOpenFolderWindow()
		{
			if (_folderWindow is null || _folderWindow.IsDisposed)
			{
				_hoverCloseOutsideSinceTick = -1;
				return;
			}

			_folderWindow.KeyDown -= FolderWindow_KeyDown;
			_folderWindow.Deactivate -= FolderWindow_Deactivate;
			_folderWindow.FolderItemsChanged -= FolderWindow_FolderItemsChanged;
			_hoverOpenTimer.Stop();
			_hoverCloseTimer.Stop();
			_hoverCandidateIcon = null;
			_hoverCloseOutsideSinceTick = -1;
			_folderWindow.Close();
		}

		private void MovementMenuItem_Click(object? sender, EventArgs e)
		{
			if (_menuFolder is null)
			{
				return;
			}

			_menuFolder.Locked = !_menuFolder.Locked;
			if (_menuFolder.Locked)
			{
				_menuFolder.RequiresPlacementBeforeOpen = false;
			}
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
			folder.AutoArrange = _appState.DefaultAutoArrange;
			folder.ItemIconSize = _appState.DefaultTileIconSize;
			folder.RequiresPlacementBeforeOpen = true;

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
				OpenSize = DeskFolder.DefaultOpenSize,
				MonitorId = Screen.FromPoint(closedLocation).DeviceName,
				Locked = false,
				AutoArrange = _appState.DefaultAutoArrange,
				ItemIconSize = _appState.DefaultTileIconSize,
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

			if (_folderWindow is not null && !_folderWindow.IsDisposed && _openedFolder is not null)
			{
				var current = sourceControl;
				while (current is not null)
				{
					if (ReferenceEquals(current, _folderWindow))
					{
						return _openedFolder;
					}

					current = current.Parent;
				}

				if (sourceControl is null)
				{
					return _openedFolder;
				}
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
			_hoverCloseOutsideSinceTick = -1;
			SaveState();
		}

		private static Point CalculateFolderWindowLocation(Rectangle iconScreenBounds, Size windowSize, Point? preferredLocation = null)
		{
			var screen = Screen.FromPoint(iconScreenBounds.Location);
			var bounds = screen.WorkingArea;

			var anchorCenterX = iconScreenBounds.Left + (iconScreenBounds.Width / 2);
			var anchorCenterY = iconScreenBounds.Top + (iconScreenBounds.Height / 2);
			var defaultX = iconScreenBounds.Left - ((windowSize.Width - iconScreenBounds.Width) / 2);
			var defaultY = iconScreenBounds.Top - ((windowSize.Height - iconScreenBounds.Height) / 2);
			var proposed = preferredLocation ?? new Point(defaultX, defaultY);

			var minX = Math.Max(bounds.Left, anchorCenterX - windowSize.Width);
			var maxX = Math.Min(bounds.Right - windowSize.Width, anchorCenterX);
			var minY = Math.Max(bounds.Top, anchorCenterY - windowSize.Height);
			var maxY = Math.Min(bounds.Bottom - windowSize.Height, anchorCenterY);

			if (maxX < minX)
			{
				maxX = minX;
			}

			if (maxY < minY)
			{
				maxY = minY;
			}

			return new Point(
				Math.Clamp(proposed.X, minX, maxX),
				Math.Clamp(proposed.Y, minY, maxY));
		}

		private void MainOverlayForm_FormClosing(object? sender, FormClosingEventArgs e)
		{
			CloseOpenFolderWindow();
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

				if (_folderWindow is not null && !_folderWindow.IsDisposed && _openedFolder is not null && ReferenceEquals(folder, _openedFolder))
				{
					folder.OpenSize = _folderWindow.ClientSize;
					folder.OpenLocation = _folderWindow.Location;
				}

				folder.ClosedLocation = icon.PointToScreen(Point.Empty);
				folder.MonitorId = Screen.FromPoint(folder.ClosedLocation).DeviceName;
			}

			_persistenceService.Save(_appState);
		}


	}
}
