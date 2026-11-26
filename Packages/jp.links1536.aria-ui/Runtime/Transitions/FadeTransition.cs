#nullable enable
using UnityEngine;

namespace Aria.UI.Transitions
{
	[RequireComponent(typeof(CanvasGroup))]
	public sealed class FadeTransition : TransitionBase
	{
		[SerializeField] AnimationCurve m_Curve = AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 1.0f);

		CanvasGroup? m_CanvasGroup;

		CanvasGroup CanvasGroup
			=> m_CanvasGroup != null
			? m_CanvasGroup
			: m_CanvasGroup = GetComponent<CanvasGroup>();

		public override void Apply(TransitionKind kind, float progress)
		{
			float rate = m_Curve.Evaluate(progress);
			CanvasGroup.alpha = kind == TransitionKind.Enter
				? rate
				: 1.0f - rate;
		}
	}
}
