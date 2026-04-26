using System.Diagnostics;

namespace Deskplorer.Services
{
	public class LaunchService
	{
		public bool TryLaunch(string filePath, out string? errorMessage)
		{
			errorMessage = null;

			if (string.IsNullOrWhiteSpace(filePath))
			{
				errorMessage = "Item path is empty.";
				return false;
			}

			try
			{
				var startInfo = new ProcessStartInfo
				{
					FileName = filePath,
					UseShellExecute = true
				};

				Process.Start(startInfo);
				return true;
			}
			catch (Exception ex)
			{
				errorMessage = ex.Message;
				return false;
			}
		}
	}
}
