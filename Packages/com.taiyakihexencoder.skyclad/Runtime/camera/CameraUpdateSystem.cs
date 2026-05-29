using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace skyclad {
	using internalProc;
	[UpdateInGroup(typeof(SkycladCameraLateUpdateSystemGroup))]
	public partial struct CameraUpdateSystem : ISystem {
		private Entity _cameraEntity;

		void ISystem.OnCreate(ref SystemState state) {
			_cameraEntity = state.CreateEntityBuilder()
				.AddRW<InternalCameraParameter, Parent, LocalTransform, LocalToWorld>()
				.Build(
					"Camera",
					InternalCameraParameter.Default
				);
			SkycladWorld.AddToRoot(_cameraEntity);
		}

		void ISystem.OnUpdate(ref SystemState state) {
			Transform cameraTransform = Camera.main.transform;

			RefRW<InternalCameraParameter> parameter = SystemAPI.GetComponentRW<InternalCameraParameter>(_cameraEntity);

			float3 destinationPosition;
			quaternion destinationRotation;
			switch(parameter.ValueRO.mode) {
				case InternalCameraParameter.CameraMode.Fixed: {
					InternalCameraParameter.FixedParameter fixedParameter = parameter.ValueRO.fixedParameter;
					destinationPosition = fixedParameter.position;
					destinationRotation = fixedParameter.rotation;
					break;
				}
				case InternalCameraParameter.CameraMode.Follow: {
					InternalCameraParameter.FollowParameter followParameter = parameter.ValueRO.followParameter;
					if (SystemAPI.Exists(followParameter.target)) {
						RefRO<LocalToWorld> localToWorld = SystemAPI.GetComponentRO<LocalToWorld>(followParameter.target);
						followParameter.CalculateDestination(
							localToWorld.ValueRO.Position, localToWorld.ValueRO.Rotation,
							out destinationPosition, out destinationRotation
						);
					} else {
						destinationPosition = cameraTransform.position;
						destinationRotation = cameraTransform.rotation;
					}
					break;
				}
				default: {
					destinationPosition = cameraTransform.position;
					destinationRotation = cameraTransform.rotation;
					break;
				}
			}

			InternalCameraParameter.LerpParameter lerpParameter = parameter.ValueRO.lerpParameter;
			if (lerpParameter.active) {
				lerpParameter.CalcPositionAndRotation(
					destinationPosition, destinationRotation,
					out float3 position, out quaternion rotation
				);
				cameraTransform.SetPositionAndRotation(position, rotation);

				parameter.ValueRW.position = position;
				parameter.ValueRW.rotation = rotation;

				parameter.ValueRW.lerpParameter = lerpParameter.Update(state.World.Time.DeltaTime);
			} else {
				cameraTransform.SetPositionAndRotation(destinationPosition, destinationRotation);
			}
		}
	
		void ISystem.OnDestroy(ref SystemState state) {
		}

		private readonly EntityCommandBuffer CreateCommandBuffer(ref SystemState state) {
			return SystemAPI
				.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
				.CreateCommandBuffer(state.World.Unmanaged);
		}
	}
}
