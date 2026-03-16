using Unity.Collections;
using Unity.Entities;
using Unity.Physics;

namespace skyclad {
	[UpdateInGroup(typeof(SkycladDioramaSystemGroup))]
	public partial struct LoadDioramaSystem : ISystem {
		private EntityQuery query;

		private EntityQuery waitQuery;
		private EntityQuery subTaskQuery;

		private EntityQuery physicsCharacterQuery;

		void ISystem.OnCreate(ref SystemState state) {
			query = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<RequestLoadDioramaComponent>()
				.WithNone<WaitTaskComponent>()
				.Build(ref state);

			waitQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<RequestLoadDioramaComponent, WaitTaskComponent>()
				.Build(ref state);

			subTaskQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<DioramaLoadElementComponent>()
				.Build(ref state);

			physicsCharacterQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<PhysicsCollider, CharacterSpawnParameterElement>()
				.Build(ref state);

			state.RequireForUpdate<RequestLoadDioramaComponent>();
		}

		void ISystem.OnUpdate(ref SystemState state) {
			EntityCommandBuffer commandBuffer = CreateCommandBuffer(ref state);

			if (!query.IsEmpty) {
				state.Dependency = new AddWaitTaskJob {
					commandBuffer = commandBuffer,
				}.Schedule(query, state.Dependency);

				// 読み込みの各ジョブを開始する
				state.Dependency = new LoadDioramaJob {
					commandBuffer = commandBuffer,
				}.Schedule(query, state.Dependency);
			} else if (!waitQuery.IsEmpty && subTaskQuery.IsEmpty) {
				state.Dependency = new EnablePhysicsJob {
					commandBuffer = CreateCommandBuffer(ref state).AsParallelWriter(),
				}.ScheduleParallel(physicsCharacterQuery, state.Dependency);

				// 読込が完了
				state.Dependency = new SkycladECSUtility.DestroyJob {
					commandBuffer = commandBuffer,
				}.Schedule(waitQuery, state.Dependency);
			}
		}

		private readonly EntityCommandBuffer CreateCommandBuffer(ref SystemState state) {
			return SystemAPI
				.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
				.CreateCommandBuffer(state.World.Unmanaged);
		}

		partial struct EnablePhysicsJob : IJobEntity {
			public EntityCommandBuffer.ParallelWriter commandBuffer;

			void Execute(in Entity entity, [EntityIndexInQuery] int sortKey) {
				commandBuffer.SetSharedComponent(
					sortKey,
					entity, 
					new PhysicsWorldIndex {
						Value = SkycladUtility.ECS.ENABLED_PHYSICS_INDEX,
					}
				);
			}
		}
	}
}