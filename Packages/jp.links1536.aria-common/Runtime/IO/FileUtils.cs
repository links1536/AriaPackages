using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Aria.Common.IO
{
	public static class FileUtils
	{
		public static async Task WriteAllTextAtomicAsync(string path, string contents, CancellationToken cancellationToken = default)
		{
			string fullPath = Path.GetFullPath(path);
			string directory = Path.GetDirectoryName(fullPath) ?? throw new ArgumentException("保存先が不正です。", nameof(path));
			Directory.CreateDirectory(directory);
			string temporaryPath = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
			try
			{
				var encoding = new UTF8Encoding(false);
				await File.WriteAllTextAsync(temporaryPath, contents, encoding, cancellationToken).ConfigureAwait(false);
				cancellationToken.ThrowIfCancellationRequested();
				if (File.Exists(fullPath))
					File.Replace(temporaryPath, fullPath, null);
				else
					File.Move(temporaryPath, fullPath);
			}
			finally
			{
				try
				{
					File.Delete(temporaryPath);
				}
				catch (IOException)
				{
				}
				catch (UnauthorizedAccessException)
				{
				}
			}
		}
	}
}
