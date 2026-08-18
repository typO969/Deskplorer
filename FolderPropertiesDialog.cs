namespace Deskplorer
{
	internal sealed class FolderPropertiesDialog : Form
	{
		private readonly TextBox _folderNameInput;
		private readonly NumericUpDown _detectionBorderInput;
		private readonly NumericUpDown _closeTimeoutInput;

		public string FolderName => _folderNameInput.Text.Trim();
		public int DetectionBorderDistance => (int) _detectionBorderInput.Value;
		public int CloseTimeoutSeconds => (int) _closeTimeoutInput.Value;

		public FolderPropertiesDialog(string folderName, int detectionBorderDistance, int closeTimeoutSeconds)
		{
			Text = "Folder Properties";
			FormBorderStyle = FormBorderStyle.FixedDialog;
			StartPosition = FormStartPosition.CenterParent;
			MinimizeBox = false;
			MaximizeBox = false;
			ShowInTaskbar = false;
			ClientSize = new Size(420, 196);

			var layout = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				Padding = new Padding(12),
				ColumnCount = 2,
				RowCount = 4,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink
			};

			layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
			layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
			layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
			layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
			layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
			layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));

			layout.Controls.Add(new Label
			{
				Text = "Folder name:",
				TextAlign = ContentAlignment.MiddleLeft,
				Dock = DockStyle.Fill,
				AutoSize = true
			}, 0, 0);

			_folderNameInput = new TextBox
			{
				Dock = DockStyle.Fill,
				Text = folderName
			};
			layout.Controls.Add(_folderNameInput, 1, 0);

			layout.Controls.Add(new Label
			{
				Text = "Hover detection border (px):",
				TextAlign = ContentAlignment.MiddleLeft,
				Dock = DockStyle.Fill,
				AutoSize = true
			}, 0, 1);

			_detectionBorderInput = new NumericUpDown
			{
				Dock = DockStyle.Left,
				Minimum = 0,
				Maximum = 200,
				Value = Math.Clamp(detectionBorderDistance, 0, 200),
				Width = 100
			};
			layout.Controls.Add(_detectionBorderInput, 1, 1);

			layout.Controls.Add(new Label
			{
				Text = "Hover close timeout (seconds):",
				TextAlign = ContentAlignment.MiddleLeft,
				Dock = DockStyle.Fill,
				AutoSize = true
			}, 0, 2);

			_closeTimeoutInput = new NumericUpDown
			{
				Dock = DockStyle.Left,
				Minimum = 0,
				Maximum = 10,
				Value = Math.Clamp(closeTimeoutSeconds, 0, 10),
				Width = 100
			};
			layout.Controls.Add(_closeTimeoutInput, 1, 2);

			var buttonPanel = new FlowLayoutPanel
			{
				Dock = DockStyle.Fill,
				FlowDirection = FlowDirection.RightToLeft,
				WrapContents = false
			};

			var okButton = new Button
			{
				Text = "OK",
				Width = 80
			};
			okButton.Click += OkButton_Click;

			var cancelButton = new Button
			{
				Text = "Cancel",
				Width = 80,
				DialogResult = DialogResult.Cancel
			};

			buttonPanel.Controls.Add(okButton);
			buttonPanel.Controls.Add(cancelButton);
			layout.Controls.Add(buttonPanel, 0, 3);
			layout.SetColumnSpan(buttonPanel, 2);

			Controls.Add(layout);
			AcceptButton = okButton;
			CancelButton = cancelButton;

			if (_folderNameInput.TextLength > 0)
			{
				_folderNameInput.SelectAll();
			}

			_folderNameInput.Focus();
		}

		public static bool TryShow(
			IWin32Window owner,
			string folderName,
			int detectionBorderDistance,
			int closeTimeoutSeconds,
			out string newFolderName,
			out int newDetectionBorderDistance,
			out int newCloseTimeoutSeconds)
		{
			using var dialog = new FolderPropertiesDialog(folderName, detectionBorderDistance, closeTimeoutSeconds);
			if (dialog.ShowDialog(owner) != DialogResult.OK)
			{
				newFolderName = folderName;
				newDetectionBorderDistance = detectionBorderDistance;
				newCloseTimeoutSeconds = closeTimeoutSeconds;
				return false;
			}

			newFolderName = dialog.FolderName;
			newDetectionBorderDistance = dialog.DetectionBorderDistance;
			newCloseTimeoutSeconds = dialog.CloseTimeoutSeconds;
			return true;
		}

		private void OkButton_Click(object? sender, EventArgs e)
		{
			if (string.IsNullOrWhiteSpace(_folderNameInput.Text))
			{
				MessageBox.Show(this, "Folder name cannot be empty.", "Deskplorer", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			DialogResult = DialogResult.OK;
			Close();
		}
	}
}