#nullable enable
using UnityEngine;

namespace Aria.UI.Transitions
{
	public abstract class TransitionBase : MonoBehaviour
	{
		/// <summary>
		/// 進行度に応じた見た目を反映する
		/// </summary>
		public abstract void Apply(TransitionKind kind, float progress);
	}
}
