using Unity.Entities;
using Unity.Mathematics;

namespace skyclad {
	public struct CameraParameter : IComponentData {
		public CameraMode mode;
		public LerpParameter lerpParameter;
		public FollowParameter followParameter;
		public FixedParameter fixedParameter;

		public float3 position;
		public quaternion rotation;

		public struct LerpParameter {
			public bool active;
			public float3 basePoint;
			public quaternion baseRotation;
			public float elapsed;
			public float seconds;
		}

		public struct FollowParameter {
			public Entity target;
			public float3 lookOffset;

			public float distance;
			public quaternion cameraDirection;
		}

		public struct FixedParameter {
			public float3 position;
			public quaternion rotation;
		}
	}

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
