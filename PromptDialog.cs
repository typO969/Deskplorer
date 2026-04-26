namespace Deskplorer
{
	internal static class PromptDialog
	{
		public static bool TryShow(IWin32Window owner, string title, string prompt, string initialValue, out string value)
		{
			using var form = new Form();
			using var promptLabel = new Label();
			using var inputBox = new TextBox();
			using var okButton = new Button();
			using var cancelButton = new Button();

			form.Text = title;
			form.FormBorderStyle = FormBorderStyle.FixedDialog;
			form.StartPosition = FormStartPosition.CenterParent;
			form.MinimizeBox = false;
			form.MaximizeBox = false;
			form.ShowInTaskbar = false;
			form.ClientSize = new Size(360, 130);

			promptLabel.Text = prompt;
			promptLabel.AutoSize = false;
			promptLabel.Location = new Point(12, 12);
			promptLabel.Size = new Size(336, 22);

			inputBox.Location = new Point(12, 38);
			inputBox.Size = new Size(336, 23);
			inputBox.Text = initialValue;

			okButton.Text = "OK";
			okButton.DialogResult = DialogResult.OK;
			okButton.Location = new Point(192, 84);
			okButton.Size = new Size(75, 26);

			cancelButton.Text = "Cancel";
			cancelButton.DialogResult = DialogResult.Cancel;
			cancelButton.Location = new Point(273, 84);
			cancelButton.Size = new Size(75, 26);

			form.Controls.Add(promptLabel);
			form.Controls.Add(inputBox);
			form.Controls.Add(okButton);
			form.Controls.Add(cancelButton);
			form.AcceptButton = okButton;
			form.CancelButton = cancelButton;

			inputBox.SelectAll();
			inputBox.Focus();

			if (form.ShowDialog(owner) != DialogResult.OK)
			{
				value = initialValue;
				return false;
			}

			value = inputBox.Text;
			return true;
		}
	}
}
