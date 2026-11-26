#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Aria.UI.Internal;
using Aria.UI.Transitions;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace Aria.UI
{
	[DisallowMultipleComponent]
	public sealed class UIElementContainer : MonoBehaviour
	{
		[SerializeField] RectTransform m_Content;
		[SerializeField] bool m_HideBelow = true;
		[SerializeField] RectTransform? m_Backdrop;
		[SerializeField, Min(0.0f)] float m_Duration = 0.15f;

		readonly List<UIElement> m_Stack = new List<UIElement>();
		readonly Dictionary<string, UIElement> m_Instances = new Dictionary<string, UIElement>();
		readonly HashSet<string> m_LoadedPaths = new HashSet<string>();
		readonly TransitionQueue m_Queue = new TransitionQueue();
		readonly ReactiveProperty<UIElement?> m_Top = new ReactiveProperty<UIElement?>();
		bool m_Destroyed;

		/// <summary>
		/// 画面のプロバイダー
		/// </summary>
		public IUIElementProvider? Provider { get; internal set; }

		/// <summary>
		/// 開いている画面の数
		/// </summary>
		public int Count
			=> m_Stack.Count;

		public bool IsEmpty
			=> m_Stack.Count == 0;

		/// <summary>
		/// 最前面の画面
		/// </summary>
		public ReadOnlyReactiveProperty<UIElement?> Top
			=> m_Top;

		/// <summary>
		/// 遷移要求の実行中か
		/// </summary>
		public bool IsBusy
			=> m_Queue.IsBusy;

		/// <summary>
		/// 生成済みの画面を登録する
		/// </summary>
		public void Register(string assetPath, UIElement instance)
		{
			if (m_Instances.ContainsKey(assetPath))
				throw new InvalidOperationException($"[{assetPath}] 既に登録されています");
			instance.transform.SetParent(m_Content, false);
			Attach(assetPath, instance);
		}

		/// <summary>
		/// 画面を上に重ねる
		/// </summary>
		public async UniTask<T> PushAsync<T>(string assetPath, CancellationToken cancellationToken = default)
			where T : UIElement
		{
			using var linked = CreateLinkedTokenSource(cancellationToken);
			return await m_Queue.EnqueueAsync(token => PushInternalAsync<T>(assetPath, null, token), linked.Token);
		}

		/// <summary>
		/// 引数を渡して画面を上に重ねる
		/// </summary>
		public async UniTask<T> PushAsync<T, TArgs>(string assetPath, TArgs args, CancellationToken cancellationToken = default)
			where T : UIElement<TArgs>
		{
			using var linked = CreateLinkedTokenSource(cancellationToken);
			return await m_Queue.EnqueueAsync(token => PushInternalAsync<T>(assetPath, element => element.OnSetup(args), token), linked.Token);
		}

		/// <summary>
		/// ダイアログを開き、閉じられるまで待って結果を返す
		/// </summary>
		public async UniTask<TResult> ShowDialogAsync<T, TArgs, TResult>(string assetPath, TArgs args, CancellationToken cancellationToken = default)
			where T : DialogElement<TArgs, TResult>
		{
			var result = default(UniTask<TResult>);
			using (var linked = CreateLinkedTokenSource(cancellationToken))
			{
				await m_Queue.EnqueueAsync(token => PushInternalAsync<T>(assetPath, element =>
				{
					result = element.BeginResult();
					element.OnSetup(args);
				}, token), linked.Token);
			}
			return await result;
		}

		/// <summary>
		/// 画面を上から取り除く
		/// </summary>
		public async UniTask PopAsync(CancellationToken cancellationToken = default)
		{
			using var linked = CreateLinkedTokenSource(cancellationToken);
			await m_Queue.EnqueueAsync(token => PopInternalAsync(token), linked.Token);
		}

		/// <summary>
		/// 指定した画面が最前面の場合のみ取り除く
		/// </summary>
		public async UniTask PopAsync(UIElement target, CancellationToken cancellationToken = default)
		{
			using var linked = CreateLinkedTokenSource(cancellationToken);
			await m_Queue.EnqueueAsync(token =>
			{
				if (!TryGetTop(out var top) || top != target)
					return UniTask.CompletedTask;
				return PopInternalAsync(token);
			}, linked.Token);
		}

		/// <summary>
		/// 指定した画面が最前面になるまで取り除く
		/// </summary>
		public async UniTask PopToAsync(string assetPath, CancellationToken cancellationToken = default)
		{
			using var linked = CreateLinkedTokenSource(cancellationToken);
			await m_Queue.EnqueueAsync(async token =>
			{
				int index = m_Stack.FindIndex(x => x.AssetPath == assetPath);
				if (index < 0)
					throw new InvalidOperationException($"[{assetPath}] 開かれていません");
				// 間の画面は演出なしで取り除き、最前面だけ演出する
				while (m_Stack.Count - 2 > index)
					await CloseImmediateAsync(m_Stack[m_Stack.Count - 2], token);
				if (m_Stack.Count - 1 > index)
					await PopInternalAsync(token);
			}, linked.Token);
		}

		/// <summary>
		/// 全ての画面を削除する
		/// </summary>
		public async UniTask ClearAsync(CancellationToken cancellationToken = default)
		{
			using var linked = CreateLinkedTokenSource(cancellationToken);
			await m_Queue.EnqueueAsync(async token =>
			{
				while (m_Stack.Count > 1)
					await CloseImmediateAsync(m_Stack[m_Stack.Count - 2], token);
				await PopInternalAsync(token);
			}, linked.Token);
		}

		/// <summary>
		/// 画面のインスタンスを破棄する
		/// </summary>
		public async UniTask UnloadAsync(string assetPath, CancellationToken cancellationToken = default)
		{
			using var linked = CreateLinkedTokenSource(cancellationToken);
			await m_Queue.EnqueueAsync(async token =>
			{
				if (!m_Instances.TryGetValue(assetPath, out var element))
					return;
				if (m_Stack.Contains(element))
					throw new InvalidOperationException($"[{assetPath}] 開いている画面は解放できません");
				m_Instances.Remove(assetPath);
				element.OnRelease();
				Destroy(element.gameObject);
				if (m_LoadedPaths.Remove(assetPath) && Provider != null)
					await Provider.UnloadAsync(assetPath);
			}, linked.Token);
		}

		async UniTask<T> PushInternalAsync<T>(string assetPath, Action<T>? setup, CancellationToken cancellationToken)
			where T : UIElement
		{
			var element = await ResolveAsync(assetPath, cancellationToken);
			if (element is not T next)
				throw new InvalidOperationException($"[{assetPath}] {typeof(T).Name}ではありません");
			if (m_Stack.Contains(next))
				throw new InvalidOperationException($"[{assetPath}] 既に開いています");

			BeginTransition();
			try
			{
				var prev = m_Top.Value;
				if (prev != null)
					prev.OnPause();

				next.transform.SetAsLastSibling();
				TransitionUtils.Prepare(next.Transition, TransitionKind.Enter);
				SetInteractive(next, false);
				next.gameObject.SetActive(true);
				try
				{
					setup?.Invoke(next);
					await next.OnEnterAsync(cancellationToken);
				}
				catch
				{
					next.gameObject.SetActive(false);
					if (prev != null)
						prev.OnResume();
					throw;
				}

				m_Stack.Add(next);
				UpdateBackdrop();

				// 表示 → 消していいなら下になった画面を非表示にする (負荷対策)
				await PlayAsync(next, TransitionKind.Enter, cancellationToken);
				bool hidePrev = m_HideBelow && prev != null;
				if (hidePrev)
				{
					prev!.gameObject.SetActive(false);
				}

				m_Top.Value = next;
				next.OnEntered();
				return next;
			}
			finally
			{
				EndTransition();
			}
		}

		async UniTask PopInternalAsync(CancellationToken cancellationToken)
		{
			if (!TryGetTop(out var top))
				return;

			BeginTransition();
			try
			{
				await top.OnExitAsync(cancellationToken);
				m_Stack.RemoveAt(m_Stack.Count - 1);

				// 消していたなら下にある画面を表示に戻す → 上の画面を取り除く
				var below = m_Stack.Count > 0
					? m_Stack[m_Stack.Count - 1]
					: null;
				if (m_HideBelow && below != null)
				{
					below.gameObject.SetActive(true);
				}
				await PlayAsync(top, TransitionKind.Exit, cancellationToken);
				top.gameObject.SetActive(false);

				UpdateBackdrop();
				m_Top.Value = below;
				top.OnExited();
				top.OnClosed();
				if (below != null)
					below.OnResume();
			}
			finally
			{
				EndTransition();
			}
		}

		/// <summary>
		/// 演出なしで画面を取り除く
		/// </summary>
		async UniTask CloseImmediateAsync(UIElement element, CancellationToken cancellationToken)
		{
			await element.OnExitAsync(cancellationToken);
			m_Stack.Remove(element);
			element.gameObject.SetActive(false);
			UpdateBackdrop();
			m_Top.Value = m_Stack.Count > 0
				? m_Stack[m_Stack.Count - 1]
				: null;
			element.OnExited();
			element.OnClosed();
		}

		async UniTask<UIElement> ResolveAsync(string assetPath, CancellationToken cancellationToken)
		{
			if (!m_Instances.TryGetValue(assetPath, out var element))
			{
				if (Provider == null)
					throw new InvalidOperationException($"[{assetPath}] {nameof(IUIElementProvider)}が設定されていません");
				var prefab = await Provider.LoadAsync(assetPath, cancellationToken);
				element = Instantiate(prefab, m_Content, false);
				m_LoadedPaths.Add(assetPath);
				Attach(assetPath, element);
			}
			if (!element.IsInitialized)
			{
				await element.OnInitializeAsync(cancellationToken);
				element.IsInitialized = true;
			}
			return element;
		}

		void Attach(string assetPath, UIElement element)
		{
			var gameObject = element.gameObject;
			gameObject.SetActive(false);
			gameObject.GetOrAddComponent<CanvasGroup>();
			element.Transition = gameObject.GetOrAddComponent<TransitionBase, FadeTransition>();
			element.AssetPath = assetPath;
			element.Container = this;
			m_Instances.Add(assetPath, element);
		}

		void BeginTransition()
		{
			if (TryGetTop(out var top))
				SetInteractive(top, false);
		}

		void EndTransition()
		{
			if (m_Destroyed)
				return;
			if (TryGetTop(out var top))
				SetInteractive(top, true);
		}

		void UpdateBackdrop()
		{
			if (m_Backdrop == null)
				return;
			if (!TryGetTop(out var top))
			{
				m_Backdrop.gameObject.SetActive(false);
				return;
			}

			// 最前面の直下に移動する
			m_Backdrop.gameObject.SetActive(true);
			m_Backdrop.SetAsLastSibling();
			top.transform.SetAsLastSibling();
		}

		UniTask PlayAsync(UIElement? element, TransitionKind kind, CancellationToken cancellationToken)
			=> element != null
			? TransitionUtils.PlayAsync(element.Transition, kind, m_Duration, cancellationToken)
			: UniTask.CompletedTask;

		bool TryGetTop([NotNullWhen(true)] out UIElement? top)
		{
			top = m_Stack.Count > 0
				? m_Stack[m_Stack.Count - 1]
				: null;
			return top != null;
		}

		CancellationTokenSource CreateLinkedTokenSource(CancellationToken cancellationToken)
			=> CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, destroyCancellationToken);

		static void SetInteractive(UIElement element, bool interactive)
		{
			element.CanvasGroup.interactable = interactive;
			element.CanvasGroup.blocksRaycasts = interactive;
		}

		void Awake()
		{
			if (m_Backdrop != null)
				m_Backdrop.gameObject.SetActive(false);
		}

		void OnDestroy()
		{
			m_Destroyed = true;
			foreach (var element in m_Stack)
				element.OnClosed();
			foreach (var element in m_Instances.Values)
			{
				if (element == null)
					continue;
				element.OnRelease();
				Destroy(element.gameObject);
			}
			if (Provider != null)
				foreach (var assetPath in m_LoadedPaths)
					Provider.UnloadAsync(assetPath).Forget();
			m_Stack.Clear();
			m_Instances.Clear();
			m_LoadedPaths.Clear();
			m_Top.Dispose();
		}
	}
}
