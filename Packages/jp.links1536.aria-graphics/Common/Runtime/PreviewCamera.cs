using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Aria.Common
{
	public class PreviewCamera : MonoBehaviour
	{
#if UNITY_EDITOR
		[UnityEditor.MenuItem("Create/Preview Camera")]
		static void CreateObject()
		{
			var obj = new GameObject("PreviewCamera");

			var camera = obj.AddComponent<Camera>();
			camera.fieldOfView = 50;

			var universalCamera = obj.AddComponent<UniversalAdditionalCameraData>();
			universalCamera.renderPostProcessing = true;
			universalCamera.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
			universalCamera.antialiasingQuality = AntialiasingQuality.High;
		}

#endif
		void Awake()
		{
			Destroy(this.gameObject);
		}
	}
}
