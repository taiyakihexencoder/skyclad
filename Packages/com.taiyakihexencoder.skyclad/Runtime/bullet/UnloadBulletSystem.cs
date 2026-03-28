using Unity.Collections;
using Unity.Entities;

namespace skyclad.bullet {
	[UpdateInGroup(typeof(SkycladBulletSystemGroup))]
	public partial struct UnloadBulletSystem : ISystem {
		private EntityQuery query;
		private EntityQuery bulletGroupQuery;

		void ISystem.OnCreate(ref SystemState state) {
			query = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<RequestUnloadBulletGroupPrefabComponent>()
				.Build(ref state);
			state.RequireForUpdate(query);

			bulletGroupQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAllRW<BulletGroup>()
				.Build(ref state);
		}

		void ISystem.OnUpdate(ref SystemState state) {
			NativeArray<RequestUnloadBulletGroupPrefabComponent> requests 
				= query.ToComponentDataArray<RequestUnloadBulletGroupPrefabComponent>(Allocator.TempJob); 

			EntityCommandBuffer commandBuffer = CreateCommandBuffer(ref state);

			state.Dependency = new UnloadJob {
				commandBuffer = commandBuffer,
				requests = requests,
			}.Schedule(bulletGroupQuery, state.Dependency);
			state.Dependency = requests.Dispose(state.Dependency);

			state.Dependency = new DestroyJob {
				commandBuffer = commandBuffer,
			}.Schedule(query, state.Dependency);
		}
	
		void ISystem.OnDestroy(ref SystemState state) {
			// エディタ再生対応
			NativeArray<Entity> bulletGroupEntities = bulletGroupQuery.ToEntityArray(Allocator.Temp);
			state.EntityManager.DestroyEntity(bulletGroupEntities);
			bulletGroupEntities.Dispose();
		}

		private readonly EntityCommandBuffer CreateCommandBuffer(ref SystemState state) {
			return SystemAPI
				.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
				.CreateCommandBuffer(state.World.Unmanaged);
		}

		partial struct UnloadJob : IJobEntity {
			[ReadOnly] public NativeArray<RequestUnloadBulletGroupPrefabComponent> requests;

			public EntityCommandBuffer commandBuffer;

			void Execute(in Entity entity, RefRW<BulletGroup> bulletGroup) {
				foreach(RequestUnloadBulletGroupPrefabComponent request in requests) {
					if (request.groupId == bulletGroup.ValueRO.groupId) {
						if (bulletGroup.ValueRO.loadCounter == 1) {
							commandBuffer.DestroyEntity(entity);
						} else {
							bulletGroup.ValueRW.loadCounter--;
						}
					}
				}
			}
		}

		partial struct DestroyJob : IJobEntity {
			public EntityCommandBuffer commandBuffer;

			void Execute(in Entity entity) {
				commandBuffer.DestroyEntity(entity);
			}
		}
	}

}
