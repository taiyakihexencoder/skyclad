using Unity.Entities;
using Unity.Mathematics;

namespace skyclad.internalProc {
	public struct InternalCameraParameter : IComponentData {
		public CameraMode mode;
		public LerpParameter lerpParameter;
		public FollowParameter followParameter;
		public FixedParameter fixedParameter;

		public float3 position;
		public quaternion rotation;

		public enum CameraMode {
			Fixed,
			Follow,
		}

		public struct LerpParameter {
			public bool active;
			public float3 basePoint;
			public quaternion baseRotation;
			public float elapsed;
			public float seconds;

			public readonly void CalcPositionAndRotation(
				in float3 dstPosition, 
				in quaternion dstRotation,
				out float3 resPosition,
				out quaternion resRotation
			) {
				float alpha = elapsed / seconds;
				resPosition = math.lerp(basePoint, dstPosition, alpha);
				resRotation = math.slerp(baseRotation, dstRotation, alpha);
			}

			public LerpParameter Update(float dt) {
				float current = elapsed + dt;
				if (current >= seconds) {
					return new LerpParameter {
						active = false,
						basePoint = basePoint,
						baseRotation = baseRotation,
						elapsed = 0.0f,
						seconds = seconds,
					};
				} else {
					return new LerpParameter {
						active = active,
						basePoint = basePoint,
						baseRotation = baseRotation,
						elapsed = elapsed + dt,
						seconds = seconds,
					};
				}
			}
		}

		public struct FollowParameter {
			public Entity target;
			public float3 lookOffset;

			public float distance;
			public quaternion cameraDirection;

			public readonly void CalculateDestination(
				in float3 targetPosition, 
				in quaternion targetRotation,
				out float3 resPosition,
				out quaternion resRotation
			) {
				resPosition = targetPosition + 
					math.mul(targetRotation, lookOffset) +
					math.mul(cameraDirection, new float3(0f,0f, - distance));
				resRotation = cameraDirection;
			}
		}

		public struct FixedParameter {
			public float3 position;
			public quaternion rotation;
		}

		public static InternalCameraParameter Default => new InternalCameraParameter {
			mode = CameraMode.Fixed,
				lerpParameter = new LerpParameter {
					active = false,
					basePoint = float3.zero,
					baseRotation = quaternion.identity,
					elapsed = 0.0f,
					seconds = 0.0f,
				},

				fixedParameter = new FixedParameter {
					position = float3.zero,
					rotation = quaternion.identity,
				},

				followParameter = new FollowParameter {
					target = Entity.Null,
					lookOffset = float3.zero,

					distance = 10.0f,
					cameraDirection = quaternion.identity,
				},
		};
	}
}