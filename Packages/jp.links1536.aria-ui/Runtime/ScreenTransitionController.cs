#nullable enable
using System.Threading;
using Aria.UI.Internal;
using Aria.UI.Transitions;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Aria.UI
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(CanvasGroup))]
	public sealed class ScreenTransitionController : MonoBehaviour
	{
		[SerializeField, Min(0.0f)] float m_Duration = 0.3f;

		readonly TransitionQueue m_Queue = new TransitionQueue();
		CanvasGroup m_CanvasGroup;
		TransitionBase m_Transition;

		/// <summary>
		/// 画面を覆っているか
		/// </summary>
		public bool IsCovered { get; private set; }

		/// <summary>
		/// 遷移中か
		/// </summary>
		public bool IsTransitioning
			=> m_Queue.IsBusy;

		public async UniTask EnterAsync(CancellationToken cancellationToken = default)
		{
			using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, destroyCancellationToken);
			await m_Queue.EnqueueAsync(token => PlayAsync(TransitionKind.Enter, token), linked.Token);
		}

		public async UniTask ExitAsync(CancellationToken cancellationToken = default)
		{
			using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, destroyCancellationToken);
			await m_Queue.EnqueueAsync(token => PlayAsync(TransitionKind.Exit, token), linked.Token);
		}

		/// <summary>
		/// 即時切り替え
		/// </summary>
		public void SetCovered(bool covered)
		{
			var kind = covered
				? TransitionKind.Enter
				: TransitionKind.Exit;
			TransitionUtils.Complete(m_Transition, kind);
			IsCovered = covered;
			m_CanvasGroup.blocksRaycasts = covered;
		}

		async UniTask PlayAsync(TransitionKind kind, CancellationToken cancellationToken)
		{
			bool covered = kind == TransitionKind.Enter;
			// 既に目的の状態なら再生しない
			if (IsCovered == covered)
				return;

			// 遷移中は下のレイヤーへの入力を遮断する
			m_CanvasGroup.blocksRaycasts = true;
			await TransitionUtils.PlayAsync(m_Transition, kind, m_Duration, cancellationToken);
			IsCovered = covered;
			m_CanvasGroup.blocksRaycasts = covered;
		}

		void Awake()
		{
			m_CanvasGroup = GetComponent<CanvasGroup>();
			m_Transition = gameObject.GetOrAddComponent<TransitionBase, FadeTransition>();
			SetCovered(false);
		}
	}
}
