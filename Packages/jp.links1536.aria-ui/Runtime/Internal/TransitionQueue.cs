#nullable enable
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Aria.UI.Internal
{
	sealed class TransitionQueue
	{
		// ひとつずつ処理を行う
		readonly SemaphoreSlim m_Semaphore = new SemaphoreSlim(1, 1);

		/// <summary>
		/// 実行中の要求があるか
		/// </summary>
		public bool IsBusy
			=> m_Semaphore.CurrentCount == 0;

		/// <summary>
		/// 要求を順番に実行する
		/// </summary>
		public async UniTask EnqueueAsync(Func<CancellationToken, UniTask> operation, CancellationToken cancellationToken)
		{
			await m_Semaphore.WaitAsync(cancellationToken);
			try
			{
				await operation(cancellationToken);
			}
			finally
			{
				m_Semaphore.Release();
			}
		}

		/// <summary>
		/// 要求を順番に実行して結果を返す
		/// </summary>
		public async UniTask<T> EnqueueAsync<T>(Func<CancellationToken, UniTask<T>> operation, CancellationToken cancellationToken)
		{
			await m_Semaphore.WaitAsync(cancellationToken);
			try
			{
				return await operation(cancellationToken);
			}
			finally
			{
				m_Semaphore.Release();
			}
		}
	}
}
