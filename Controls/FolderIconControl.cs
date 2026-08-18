using System.ComponentModel;

namespace Deskplorer.Controls
{
	public class FolderIconControl : UserControl
	{
		private static readonly Image DefaultFolderImage = LoadDefaultFolderImage();

		private readonly PictureBox _iconPicture;
		private readonly Label _nameLabel;
		private readonly Label _countBadge;
		private readonly ToolTip _toolTip;
		private string _folderName = string.Empty;

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string FolderName
		{
			get => _folderName;
			set
			{
				_folderName = value ?? string.Empty;
				_nameLabel.Text = LabelTextFormatter.FormatLabelText(_folderName, _nameLabel.Width, _nameLabel.Font, 2);
				_toolTip.SetToolTip(_nameLabel, _folderName);
			}
		}

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int ItemCount
		{
			get => int.TryParse(_countBadge.Text, out var value) ? value : 0;
			set => _countBadge.Text = value.ToString();
		}

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Image? IconImage
		{
			get => _iconPicture.Image ?? DefaultFolderImage;
			set => _iconPicture.Image = value ?? DefaultFolderImage;
		}

		public FolderIconControl()
		{
			Size = new Size(74, 90);
			BackColor = IsDesignTime() ? Color.FromArgb(48, 48, 48) : Color.Transparent;
			Padding = new Padding(1);
			_toolTip = new ToolTip();

			_iconPicture = new PictureBox
			{
				Image = DefaultFolderImage,
				Location = new Point(21, 6),
				Size = new Size(32, 32),
				SizeMode = PictureBoxSizeMode.StretchImage,
				Cursor = Cursors.Hand
			};

			_nameLabel = new Label
			{
				Text = "folder",
				TextAlign = ContentAlignment.TopCenter,
				AutoEllipsis = true,
				ForeColor = Color.White,
				BackColor = Color.Transparent,
				Location = new Point(2, 44),
				Size = new Size(70, 42),
				Cursor = Cursors.Hand
			};

			_countBadge = new Label
			{
				Text = "0",
				TextAlign = ContentAlignment.MiddleCenter,
				ForeColor = Color.Black,
				BackColor = Color.White,
				BorderStyle = BorderStyle.FixedSingle,
				Location = new Point(48, 28),
				Size = new Size(22, 22),
				Cursor = Cursors.Hand
			};

			Controls.Add(_iconPicture);
			Controls.Add(_nameLabel);
			Controls.Add(_countBadge);

			_iconPicture.Click += ChildClicked;
			_nameLabel.Click += ChildClicked;
			_countBadge.Click += ChildClicked;
		}

		private void ChildClicked(object? sender, EventArgs e)
		{
			OnClick(e);
		}

		private static Image LoadDefaultFolderImage()
		{
			if (IsDesignTime())
			{
				return SystemIcons.WinLogo.ToBitmap();
			}

			var localDefault = TryLoadLocalDefaultFolderIcon();
			if (localDefault is not null)
			{
				return localDefault;
			}

			if (!string.IsNullOrWhiteSpace(Environment.SystemDirectory))
			{
				var shell32Path = Path.Combine(Environment.SystemDirectory, "shell32.dll");
				if (File.Exists(shell32Path))
				{
					try
					{
						using var shellIcon = Icon.ExtractAssociatedIcon(shell32Path);
						if (shellIcon is not null)
						{
							return shellIcon.ToBitmap();
						}
					}
					catch
					{
					}
				}
			}

			var folderCandidates = new[]
			{
				Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
				Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
				Environment.GetFolderPath(Environment.SpecialFolder.Windows)
			};

			foreach (var folderPath in folderCandidates)
			{
				if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
				{
					continue;
				}

				try
				{
					using var icon = Icon.ExtractAssociatedIcon(folderPath);
					if (icon is not null)
					{
						return icon.ToBitmap();
					}
				}
				catch
				{
				}
			}

			return SystemIcons.WinLogo.ToBitmap();
		}

		private static Image? TryLoadLocalDefaultFolderIcon()
		{
			var startDir = AppContext.BaseDirectory;
			var current = new DirectoryInfo(startDir);

			while (current is not null)
			{
				var candidate = Path.Combine(current.FullName, "Assets", "Icons", "folder01.ico");
				if (File.Exists(candidate))
				{
					try
					{
						using var icon = new Icon(candidate, new Size(32, 32));
						return icon.ToBitmap();
					}
					catch
					{
						return null;
					}
				}

				current = current.Parent;
			}

			return null;
		}

		private void InitializeComponent()
		{

		}

		private static bool IsDesignTime()
		{
			return LicenseManager.UsageMode == LicenseUsageMode.Designtime;
		}
	}
}
