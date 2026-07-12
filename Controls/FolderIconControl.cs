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
				_nameLabel.Text = FormatLabelText(_folderName, _nameLabel.Width, _nameLabel.Font, 2);
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
		public Image IconImage
		{
			get => _iconPicture.Image ?? DefaultFolderImage;
        set => _iconPicture.Image = value ?? DefaultFolderImage;
		}

		public FolderIconControl()
		{
			Size = new Size(74, 90);
			BackColor = Color.Transparent;
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

		private static Image LoadDefaultFolderImage()
		{
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
	}
}
