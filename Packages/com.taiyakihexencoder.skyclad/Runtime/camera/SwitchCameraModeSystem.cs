using Unity.Entities;

namespace skyclad {
	using internalProc;
	[UpdateInGroup(typeof(SkycladCameraLateUpdateSystemGroup))]
	public partial struct SwitchCameraModeSystem : ISystem {
		void ISystem.OnCreate(ref SystemState state) {
			state.RequireForUpdate<RequestSwitchCameraModeTag>();
		}

		void ISystem.OnUpdate(ref SystemState state) {
			EntityCommandBuffer commandBuffer = CreateCommandBuffer(ref state);
			if (SystemAPI.TryGetSingletonRW(out RefRW<InternalCameraParameter> parameter)) {
				foreach((RefRO<RequestSwitchCameraModeTag> _, RefRO<CameraModeFixed> mode, Entity entity) 
					in SystemAPI.Query<RefRO<RequestSwitchCameraModeTag>, RefRO<CameraModeFixed>>().WithEntityAccess()) {
					parameter.ValueRW.fixedParameter = new InternalCameraParameter.FixedParameter {
						position = mode.ValueRO.position,
						rotation = mode.ValueRO.rotation,
					};
					parameter.ValueRW.lerpParameter = new InternalCameraParameter.LerpParameter {
						active = mode.ValueRO.lerpSeconds > 0.0f,
						seconds = mode.ValueRO.lerpSeconds,
						basePoint = parameter.ValueRO.position,
						baseRotation = parameter.ValueRO.rotation,
					};
					parameter.ValueRW.mode = InternalCameraParameter.CameraMode.Fixed;

					commandBuffer.DestroyEntity(entity);
				}

				foreach((RefRO<RequestSwitchCameraModeTag> _, RefRO<CameraModeFollow> mode, Entity entity)
					in SystemAPI.Query<RefRO<RequestSwitchCameraModeTag>, RefRO<CameraModeFollow>>().WithEntityAccess()) {
					parameter.ValueRW.followParameter = new InternalCameraParameter.FollowParameter {
						target = mode.ValueRO.target,
						lookOffset = mode.ValueRO.lookOffset,

						distance = mode.ValueRO.distance,
						cameraDirection = mode.ValueRO.cameraDirection,
					};
					parameter.ValueRW.lerpParameter = new InternalCameraParameter.LerpParameter {
						active = mode.ValueRO.lerpSeconds > 0.0f,
						seconds = mode.ValueRO.lerpSeconds,
						basePoint = parameter.ValueRO.position,
						baseRotation = parameter.ValueRO.rotation,
					};
					parameter.ValueRW.mode = InternalCameraParameter.CameraMode.Follow;

					commandBuffer.DestroyEntity(entity);
				}
			}
		}

		private readonly EntityCommandBuffer CreateCommandBuffer(ref SystemState state) {
			return SystemAPI
				.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
				.CreateCommandBuffer(state.World.Unmanaged);
		}
	}
}