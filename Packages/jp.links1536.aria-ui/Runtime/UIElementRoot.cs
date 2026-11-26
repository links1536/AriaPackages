#nullable enable
using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Aria.UI
{
	[DisallowMultipleComponent]
	public sealed class UIElementRoot : MonoBehaviour
	{
		// 配列順が描画順（後ろほど前面）。先頭をScreenレイヤーとして扱う
		[SerializeField] UIElementContainer[] m_Containers;
		// OverlayとSystemの間に配置する（Containerと同じ親の下に置く）
		[SerializeField] ScreenTransitionController m_ScreenTransitionController;
		[SerializeField] InputActionReference? m_BackAction;
		// Screenレイヤーの最後の1枚は戻る入力で閉じない
		[SerializeField] bool m_KeepLastScreen = true;

		IUIElementProvider? m_Provider;
		Scene m_Scene;

		public ScreenTransitionController ScreenTransitionController
			=> m_ScreenTransitionController;

		/// <summary>
		/// 画面のプロバイダー
		/// 未設定のままPushすると例外になる
		/// </summary>
		public IUIElementProvider? Provider
		{
			get => m_Provider;
			set
			{
				m_Provider = value;
				foreach (var container in m_Containers)
					container.Provider = value;
			}
		}

		/// <summary>
		/// ルートを作成する
		/// </summary>
		public static UIElementRoot Create(UIElementRoot prefab, Transform? parent)
		{
			var root = Instantiate(prefab, parent, false);
			root.m_Scene = SceneManager.GetActiveScene();
			SceneManager.sceneUnloaded += root.OnSceneUnloaded;
			return root;
		}

		/// <summary>
		/// 名前からレイヤーを取得する
		/// </summary>
		public UIElementContainer GetContainer(string name)
		{
			foreach (var container in m_Containers)
				if (container.name == name)
					return container;
			throw new ArgumentException($"[{name}] レイヤーが見つかりません", nameof(name));
		}

		/// <summary>
		/// 戻る入力を処理する
		/// </summary>
		public void HandleBack()
		{
			// 画面を覆っている間は、覆いより前面のレイヤーだけが受け付ける
			bool covered = m_ScreenTransitionController != null
				&& (m_ScreenTransitionController.IsCovered || m_ScreenTransitionController.IsTransitioning);
			for (int i = m_Containers.Length - 1; i >= 0; --i)
			{
				var container = m_Containers[i];
				if (covered && !IsAboveScreenTransition(container))
					return;
				if (container.IsEmpty)
					continue;
				// 遷移中の入力は捨てる
				if (container.IsBusy)
					return;
				var top = container.Top.CurrentValue;
				if (top != null && top.OnBackRequested())
					return;
				if (m_KeepLastScreen && i == 0 && container.Count == 1)
					return;
				container.PopAsync(destroyCancellationToken).Forget();
				return;
			}
		}

		bool IsAboveScreenTransition(UIElementContainer container)
			=> container.transform.GetSiblingIndex() > m_ScreenTransitionController.transform.GetSiblingIndex();

		void OnBackPerformed(InputAction.CallbackContext context)
			=> HandleBack();

		void OnSceneUnloaded(Scene scene)
		{
			if (scene == m_Scene)
				Destroy(gameObject);
		}

		void OnEnable()
		{
			if (m_BackAction == null)
				return;
			m_BackAction.action.performed += OnBackPerformed;
			m_BackAction.action.Enable();
		}

		void OnDisable()
		{
			if (m_BackAction == null)
				return;
			m_BackAction.action.performed -= OnBackPerformed;
		}

		void OnDestroy()
		{
			SceneManager.sceneUnloaded -= OnSceneUnloaded;
		}
	}
}
