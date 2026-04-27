using Deskplorer.Services;

namespace Deskplorer
{
   public class ImageResIconBrowserForm : Form
   {
      private readonly IconCacheService _iconCacheService = new();
      private readonly FlowLayoutPanel _iconPanel;
      private readonly NumericUpDown _startIndexInput;
      private readonly NumericUpDown _endIndexInput;
      private readonly Button _loadButton;

      public int? SelectedIconIndex { get; private set; }

      public ImageResIconBrowserForm()
      {
         Text = "Choose Folder Icon";
         StartPosition = FormStartPosition.CenterParent;
         FormBorderStyle = FormBorderStyle.SizableToolWindow;
         MinimizeBox = false;
         MaximizeBox = false;
         ShowInTaskbar = false;
         ClientSize = new Size(560, 420);

         var topPanel = new Panel
         {
            Dock = DockStyle.Top,
            Height = 40
         };

         var startLabel = new Label
         {
            Text = "Start:",
            Location = new Point(8, 12),
            Size = new Size(40, 20)
         };

         _startIndexInput = new NumericUpDown
         {
            Location = new Point(52, 10),
            Size = new Size(70, 23),
            Minimum = 0,
            Maximum = 4000,
            Value = 0
         };

         var endLabel = new Label
         {
            Text = "End:",
            Location = new Point(130, 12),
            Size = new Size(35, 20)
         };

         _endIndexInput = new NumericUpDown
         {
            Location = new Point(170, 10),
            Size = new Size(70, 23),
            Minimum = 0,
            Maximum = 4000,
            Value = 300
         };

         _loadButton = new Button
         {
            Text = "Load",
            Location = new Point(250, 9),
            Size = new Size(70, 25)
         };
         _loadButton.Click += (_, _) => LoadIconRange();

         topPanel.Controls.Add(startLabel);
         topPanel.Controls.Add(_startIndexInput);
         topPanel.Controls.Add(endLabel);
         topPanel.Controls.Add(_endIndexInput);
         topPanel.Controls.Add(_loadButton);

         _iconPanel = new FlowLayoutPanel
         {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(8),
            BackColor = Color.FromArgb(24, 24, 24)
         };

         Controls.Add(_iconPanel);
         Controls.Add(topPanel);

         LoadIconRange();
      }

      private void LoadIconRange()
      {
         _iconPanel.Controls.Clear();

         var start = (int)_startIndexInput.Value;
         var end = (int)_endIndexInput.Value;
         if (end < start)
         {
            (start, end) = (end, start);
         }

         var available = _iconCacheService.GetAvailableImageResIconIndexes(start, end);
         foreach (var index in available)
         {
            var tile = CreateIconTile(index);
            _iconPanel.Controls.Add(tile);
         }
      }

      private Control CreateIconTile(int iconIndex)
      {
         var panel = new Panel
         {
            Size = new Size(74, 86),
            BackColor = Color.FromArgb(36, 36, 36),
            Margin = new Padding(6),
            Cursor = Cursors.Hand,
            Tag = iconIndex
         };

         var picture = new PictureBox
         {
            Size = new Size(32, 32),
            Location = new Point(21, 10),
            SizeMode = PictureBoxSizeMode.StretchImage,
            Image = _iconCacheService.GetImageResIconImage(iconIndex),
            Cursor = Cursors.Hand,
            Tag = iconIndex
         };

         var label = new Label
         {
            Text = iconIndex.ToString(),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(0, 52),
            Size = new Size(74, 24),
            Cursor = Cursors.Hand,
            Tag = iconIndex
         };

         panel.Controls.Add(picture);
         panel.Controls.Add(label);

         panel.DoubleClick += IconSelected;
         picture.DoubleClick += IconSelected;
         label.DoubleClick += IconSelected;

         return panel;
      }

      private void IconSelected(object? sender, EventArgs e)
      {
         if (sender is Control control)
         {
            if (control.Tag is int idx)
            {
               SelectedIconIndex = idx;
            }
            else if (control.Tag is string str && int.TryParse(str, out var parsed))
            {
               SelectedIconIndex = parsed;
            }
            else if (control.Parent?.Tag is int parentIdx)
            {
               SelectedIconIndex = parentIdx;
            }
         }

         if (SelectedIconIndex.HasValue)
         {
            DialogResult = DialogResult.OK;
            Close();
         }
      }
   }
}
