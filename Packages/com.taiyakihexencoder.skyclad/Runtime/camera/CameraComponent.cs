using Unity.Entities;
using Unity.Mathematics;

namespace skyclad {
	/// <summary>
	/// カメラモードの変更
	/// </summary>
	public struct RequestSwitchCameraModeTag : IComponentData{}

	/// <summary>
	/// カメラモードの変更(Fixed)
	/// </summary>
	public struct CameraModeFixed : IComponentData {
		public float3 position;
		public quaternion rotation;

		public float lerpSeconds;
	}

	/// <summary>
	/// カメラモードの変更(Follow)
	/// </summary>
	public struct CameraModeFollow : IComponentData {
		public Entity target;
		public float3 lookOffset;

		public float distance;
		public quaternion cameraDirection;

		public float lerpSeconds;
	}
}
