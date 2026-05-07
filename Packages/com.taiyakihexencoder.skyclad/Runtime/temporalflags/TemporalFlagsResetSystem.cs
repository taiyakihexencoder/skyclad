using Unity.Collections;
using Unity.Entities;

namespace skyclad {
	/// <summary>
	/// TemporalFlagsをリセットする。
	/// </summary>
	[UpdateInGroup(typeof(SkycladSimulationSystemGroup))]
	public partial struct TemporalFlagsResetSystem : ISystem {
		private EntityQuery query;
		private Entity temporalFlagsEntity;

		void ISystem.OnCreate(ref SystemState state) {
			EntityManager entityManager = state.EntityManager;
			temporalFlagsEntity = entityManager.CreateEntity(
				entityManager.CreateArchetype(
					ComponentType.ReadWrite<TemporalFlags>()
				)
			);

#if UNITY_EDITOR
			entityManager.SetName(temporalFlagsEntity, "Temporal Flags");
#endif

			query = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<TemporalFlagsResetRequest>()
				.Build(ref state);
			state.RequireForUpdate(query);
		}

		void ISystem.OnUpdate(ref SystemState state) {
			RefRW<TemporalFlags> flags = SystemAPI.GetComponentRW<TemporalFlags>(temporalFlagsEntity);

			EntityCommandBuffer commandBuffer = CreateCommandBuffer(ref state);
			foreach (RefRO<TemporalFlagsResetRequest> request in SystemAPI.Query<RefRO<TemporalFlagsResetRequest>>()) {
				flags.ValueRW.flag0to63 = request.ValueRO.flag0to63;
				flags.ValueRW.flag64to127 = request.ValueRO.flag64to127;
				flags.ValueRW.flag128to191 = request.ValueRO.flag128to191;
				flags.ValueRW.flag192to255 = request.ValueRO.flag192to255;
				break;
			}
			commandBuffer.DestroyEntity(query, EntityQueryCaptureMode.AtPlayback);
		}

		void ISystem.OnDestroy(ref SystemState state) {
			if (state.EntityManager.Exists(temporalFlagsEntity)) {
				state.EntityManager.DestroyEntity(temporalFlagsEntity);
				temporalFlagsEntity = Entity.Null;
			}
		}

		private readonly EntityCommandBuffer CreateCommandBuffer(ref SystemState state) {
			return SystemAPI
				.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
				.CreateCommandBuffer(state.World.Unmanaged);
		}
	}

}
