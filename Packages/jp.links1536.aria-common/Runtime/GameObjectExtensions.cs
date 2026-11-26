using UnityEngine;

namespace Aria
{
	public static class GameObjectExtensions
	{
		public static T GetOrAddComponent<T>(this GameObject self)
			where T : Component
			=> self.TryGetComponent<T>(out var component)
			? component
			: self.AddComponent<T>();

		public static TBase GetOrAddComponent<TBase, TFallback>(this GameObject self)
			where TBase : Component
			where TFallback : TBase
			=> self.TryGetComponent<TBase>(out var component)
			? component
			: self.AddComponent<TFallback>();
	}
}
