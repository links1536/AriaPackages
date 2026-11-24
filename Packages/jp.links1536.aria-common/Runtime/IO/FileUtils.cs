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
				byte[] bytes = new UTF8Encoding(false).GetBytes(contents);
				using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, true))
				{
					await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);
					await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
				}
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
