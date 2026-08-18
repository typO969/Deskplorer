namespace Deskplorer
{
	internal static class LabelTextFormatter
	{
		public static string FormatLabelText(string text, int maxWidth, Font font, int maxLines)
		{
			if (string.IsNullOrWhiteSpace(text) || maxWidth <= 0 || maxLines <= 0)
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
	}
}
