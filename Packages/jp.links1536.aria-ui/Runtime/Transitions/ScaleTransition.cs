#nullable enable
using UnityEngine;

namespace Aria.UI.Transitions
{
	public sealed class ScaleTransition : TransitionBase
	{
		[SerializeField] Vector3 m_ShownScale = Vector3.one;
		[SerializeField] Vector3 m_HiddenScale = Vector3.zero;
		[SerializeField] AnimationCurve m_Curve = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);

		public override void Apply(TransitionKind kind, float progress)
		{
			float rate = m_Curve.Evaluate(progress);
			if (kind == TransitionKind.Exit)
				rate = 1.0f - rate;
			transform.localScale = Vector3.LerpUnclamped(m_HiddenScale, m_ShownScale, rate);
		}
	}
}
