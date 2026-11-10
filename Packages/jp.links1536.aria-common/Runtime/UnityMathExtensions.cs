using UnityEngine;

namespace Aria.Common
{
	public static class UnityMathExtensions
	{
		public static bool IsZero(this Vector2Int self)
			=> self.x == 0
			&& self.y == 0;

		public static bool IsZero(this Vector3Int self)
			=> self.x == 0
			&& self.y == 0
			&& self.z == 0;

		public static bool IsZero(this Vector2 self)
			=> Mathf.Approximately(self.x, float.Epsilon)
			&& Mathf.Approximately(self.y, float.Epsilon);
		public static bool IsZero(this Vector3 self)
			=> Mathf.Approximately(self.x, float.Epsilon)
			&& Mathf.Approximately(self.y, float.Epsilon)
			&& Mathf.Approximately(self.z, float.Epsilon);
		public static bool IsZero(this Vector4 self)
			=> Mathf.Approximately(self.x, float.Epsilon)
			&& Mathf.Approximately(self.y, float.Epsilon)
			&& Mathf.Approximately(self.z, float.Epsilon)
			&& Mathf.Approximately(self.w, float.Epsilon);
	}
}
