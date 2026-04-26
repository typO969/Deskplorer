using System.ComponentModel;

namespace Deskplorer.Controls
{
	public class FolderIconControl : UserControl
	{
		private readonly PictureBox _iconPicture;
		private readonly Label _nameLabel;
		private readonly Label _countBadge;

    [Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string FolderName
		{
			get => _nameLabel.Text;
			set => _nameLabel.Text = value;
		}

      [Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int ItemCount
		{
			get => int.TryParse(_countBadge.Text, out var value) ? value : 0;
			set => _countBadge.Text = value.ToString();
		}

		public FolderIconControl()
		{
			Size = new Size(74, 90);
			BackColor = Color.Transparent;

			_iconPicture = new PictureBox
			{
				Image = SystemIcons.WinLogo.ToBitmap(),
				Location = new Point(21, 6),
				Size = new Size(32, 32),
				SizeMode = PictureBoxSizeMode.StretchImage,
				Cursor = Cursors.Hand
			};

			_nameLabel = new Label
			{
				Text = "folder",
				TextAlign = ContentAlignment.TopCenter,
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
	}
}
