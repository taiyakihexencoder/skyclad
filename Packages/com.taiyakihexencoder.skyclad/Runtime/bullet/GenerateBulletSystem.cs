using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace skyclad.bullet {
	[UpdateInGroup(typeof(SkycladBulletSystemGroup))]
	public partial struct GenerateBulletSystem : ISystem {
		private EntityQuery query;
		private EntityQuery prefabQuery;

		void ISystem.OnCreate(ref SystemState state) {
			query = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<RequestCreateBullet>()
				.Build(ref state);
			state.RequireForUpdate(query);

			prefabQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<Prefab, Bullet>()
				.Build(ref state);
		}

		void ISystem.OnUpdate(ref SystemState state) {
			NativeArray<RequestCreateBullet> requests = query.ToComponentDataArray<RequestCreateBullet>(Allocator.TempJob);
			state.Dependency = new GenerateJob {
				requests = requests,
				commandBuffer = CreateCommandBuffer(ref state).AsParallelWriter(),
			}.ScheduleParallel(prefabQuery, state.Dependency);

			state.Dependency = requests.Dispose(state.Dependency);

			state.Dependency = new DestroyJob {
				commandBuffer = CreateCommandBuffer(ref state),
			}.Schedule(query, state.Dependency);
		}
	
		void ISystem.OnDestroy(ref SystemState state) {
		}

		private readonly EntityCommandBuffer CreateCommandBuffer(ref SystemState state) {
			return SystemAPI
				.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
				.CreateCommandBuffer(state.World.Unmanaged);
		}

		partial struct GenerateJob : IJobEntity {
			[ReadOnly] public NativeArray<RequestCreateBullet> requests;

			public EntityCommandBuffer.ParallelWriter commandBuffer;

			void Execute(in Entity entity, [EntityIndexInQuery] int sortKey, RefRO<Bullet> bullet) {
				foreach(RequestCreateBullet request in requests) {
					if (bullet.ValueRO.bulletId == request.bulletId) {
						Entity instance = commandBuffer.Instantiate(sortKey, entity);
						commandBuffer.RemoveComponent<Parent>(sortKey, instance);
						commandBuffer.SetComponent(
							sortKey, 
							instance,
							LocalTransform.FromPositionRotation(request.position, request.rotation)
						);
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