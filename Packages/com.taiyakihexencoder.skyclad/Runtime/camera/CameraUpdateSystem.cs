using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace skyclad {
	[UpdateInGroup(typeof(SkycladCameraLateUpdateSystemGroup))]
	public partial struct CameraUpdateSystem : ISystem {
		private Entity _cameraEntity;

		void ISystem.OnCreate(ref SystemState state) {
			EntityManager entityManager = state.EntityManager;

			_cameraEntity = entityManager.CreateEntity(
				entityManager.CreateArchetype(
					new ComponentType[] {
						ComponentType.ReadWrite<CameraParameter>(),
						ComponentType.ReadWrite<Parent>(),
						ComponentType.ReadWrite<LocalTransform>(),
						ComponentType.ReadWrite<LocalToWorld>(),
					}
				)
			);

			SkycladWorld.AddToRoot(_cameraEntity);

			entityManager.SetComponentData(
				_cameraEntity,
				new CameraParameter {
					mode = CameraMode.Fixed,
					lerpParameter = new CameraParameter.LerpParameter {
						active = false,
						basePoint = float3.zero,
						baseRotation = quaternion.identity,
						elapsed = 0.0f,
						seconds = 0.0f,
					},
					fixedParameter = new CameraParameter.FixedParameter {
						position = float3.zero,
						rotation = quaternion.identity,
					},
					followParameter = new CameraParameter.FollowParameter {
						target = Entity.Null,
						lookOffset = float3.zero,

						distance = 10.0f,
						cameraDirection = quaternion.identity,
					},
				}
			);
		}

		void ISystem.OnUpdate(ref SystemState state) {
			Transform cameraTransform = Camera.main.transform;

			RefRW<CameraParameter> parameter = SystemAPI.GetComponentRW<CameraParameter>(_cameraEntity);

			float3 destinationPosition;
			quaternion destinationRotation;
			switch(parameter.ValueRO.mode) {
				case CameraMode.Fixed: {
					CameraParameter.FixedParameter fixedParameter = parameter.ValueRO.fixedParameter;
					destinationPosition = fixedParameter.position;
					destinationRotation = fixedParameter.rotation;
					break;
				}
				case CameraMode.Follow: {
					CameraParameter.FollowParameter followParameter = parameter.ValueRO.followParameter;
					if (SystemAPI.Exists(followParameter.target)) {
						RefRO<LocalToWorld> localToWorld = SystemAPI.GetComponentRO<LocalToWorld>(followParameter.target);
						destinationPosition = localToWorld.ValueRO.Position + 
							math.mul(localToWorld.ValueRO.Rotation, followParameter.lookOffset) +
							math.mul(followParameter.cameraDirection, new float3(0f, 0f, - followParameter.distance));
						destinationRotation = followParameter.cameraDirection;
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

			CameraParameter.LerpParameter lerpParameter = parameter.ValueRO.lerpParameter;
			if (lerpParameter.active) {
				float dt = state.World.Time.DeltaTime;
				float alpha = lerpParameter.elapsed / lerpParameter.seconds;
				float3 position = math.lerp(lerpParameter.basePoint, destinationPosition, alpha);
				quaternion rotation = math.slerp(lerpParameter.baseRotation, destinationRotation, alpha);
				cameraTransform.SetPositionAndRotation(position, rotation);

				parameter.ValueRW.position = position;
				parameter.ValueRW.rotation = rotation;

				lerpParameter.elapsed += dt;
				if (lerpParameter.elapsed >= lerpParameter.seconds) {
					lerpParameter.elapsed = 0.0f;
					lerpParameter.active = false;
				}
				parameter.ValueRW.lerpParameter = lerpParameter;
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
