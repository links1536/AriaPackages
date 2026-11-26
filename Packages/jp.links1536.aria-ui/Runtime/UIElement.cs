#nullable enable
using System;
using System.Threading;
using Aria.UI.Transitions;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Aria.UI
{
	[RequireComponent(typeof(CanvasGroup))]
	public abstract class UIElement : MonoBehaviour
	{
		CanvasGroup? m_CanvasGroup;

		public CanvasGroup CanvasGroup
			=> m_CanvasGroup != null
			? m_CanvasGroup
			: m_CanvasGroup = GetComponent<CanvasGroup>();

		/// <summary>
		/// 遷移演出
		/// </summary>
		public TransitionBase Transition { get; internal set; }

		/// <summary>
		/// 登録先のコンテナ
		/// </summary>
		public UIElementContainer? Container { get; internal set; }

		/// <summary>
		/// 登録時のアセットパス
		/// </summary>
		public string AssetPath { get; internal set; }

		internal bool IsInitialized { get; set; }

		/// <summary>
		/// 生成完了時処理
		/// </summary>
		protected internal virtual UniTask OnInitializeAsync(CancellationToken cancellationToken)
			=> UniTask.CompletedTask;

		/// <summary>
		/// 表示開始時処理
		/// </summary>
		protected internal virtual UniTask OnEnterAsync(CancellationToken cancellationToken)
			=> UniTask.CompletedTask;

		/// <summary>
		/// 表示完了時処理
		/// </summary>
		protected internal virtual void OnEntered()
		{
		}

		protected internal virtual void OnPause()
		{
		}

		protected internal virtual void OnResume()
		{
		}

		protected internal virtual UniTask OnExitAsync(CancellationToken cancellationToken)
			=> UniTask.CompletedTask;

		protected internal virtual void OnExited()
		{
		}

		protected internal virtual void OnRelease()
		{
		}

		/// <summary>
		/// 戻る入力を受付時
		/// </summary>
		/// <returns> 自前で後処理まで行った </returns>
		protected internal virtual bool OnBackRequested()
			=> false;

		protected UniTask CloseAsync()
		{
			if (Container == null)
				throw new InvalidOperationException($"[{name}] コンテナに登録されていません");
			return Container.PopAsync(this);
		}

		internal virtual void OnClosed()
		{
		}
	}

	public abstract class UIElement<TArgs> : UIElement
	{
		protected internal abstract void OnSetup(TArgs args);
	}
}
