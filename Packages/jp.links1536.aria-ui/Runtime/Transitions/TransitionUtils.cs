#nullable enable
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Aria.UI.Transitions
{
	public static class TransitionUtils
	{
		/// <summary>
		/// 演出を指定した時間で再生する
		/// </summary>
		public static async UniTask PlayAsync(TransitionBase transition, TransitionKind kind, float duration, CancellationToken cancellationToken)
		{
			float elapsed = 0.0f;
			transition.Apply(kind, 0.0f);
			while (elapsed < duration)
			{
				await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
				elapsed += Time.unscaledDeltaTime;
				transition.Apply(kind, Mathf.Clamp01(elapsed / duration));
			}
			transition.Apply(kind, 1.0f);
		}

		/// <summary>
		/// 演出を開始状態にする
		/// </summary>
		public static void Prepare(TransitionBase transition, TransitionKind kind)
			=> transition.Apply(kind, 0.0f);

		/// <summary>
		/// 演出を完了状態にする
		/// </summary>
		public static void Complete(TransitionBase transition, TransitionKind kind)
			=> transition.Apply(kind, 1.0f);
	}
}
