#nullable enable
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Aria.UI
{
	public interface IUIElementProvider
	{
		/// <summary>
		/// 画面のプレハブを読み込む
		/// </summary>
		public UniTask<UIElement> LoadAsync(string assetPath, CancellationToken cancellationToken = default);

		/// <summary>
		/// 画面のプレハブを解放する
		/// </summary>
		public UniTask UnloadAsync(string assetPath);
	}
}
