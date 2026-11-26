#nullable enable
using Cysharp.Threading.Tasks;

namespace Aria.UI
{
	public abstract class DialogElement<TArgs, TResult> : UIElement<TArgs>
	{
		UniTaskCompletionSource<TResult>? m_CompletionSource;

		/// <summary>
		/// 結果を確定せずに閉じられた場合の結果
		/// </summary>
		protected virtual TResult DefaultResult
			=> default!;

		/// <summary>
		/// 結果を確定して閉じる
		/// </summary>
		protected UniTask CloseAsync(TResult result)
		{
			m_CompletionSource?.TrySetResult(result);
			return CloseAsync();
		}

		internal UniTask<TResult> BeginResult()
		{
			m_CompletionSource = new UniTaskCompletionSource<TResult>();
			return m_CompletionSource.Task;
		}

		internal override void OnClosed()
		{
			// 戻るキーや外部からのPopで閉じられた場合
			m_CompletionSource?.TrySetResult(DefaultResult);
			m_CompletionSource = null;
		}
	}
}
