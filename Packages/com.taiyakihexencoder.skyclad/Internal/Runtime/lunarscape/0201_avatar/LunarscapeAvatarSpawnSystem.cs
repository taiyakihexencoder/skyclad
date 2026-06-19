using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace skyclad.lunarscape.internalProc {
	[UpdateInGroup(typeof(LunarscapeSimulationAvatarSystemGroup))]
	public partial struct LunarscapeAvatarSpawnSystem : ISystem {
		private EntityQuery _requestQuery;
		private EntityQuery _prefabQuery;

		void ISystem.OnCreate(ref SystemState state) {
			_requestQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<LunarscapeAvatarSpawnRequest>()
				.Build(ref state);
			_prefabQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithOptions(EntityQueryOptions.IncludePrefab)
				.WithAll<Prefab, LunarscapeAvatar>()
				.Build(ref state);
		}

		void ISystem.OnUpdate(ref SystemState state) {
			NativeArray<LunarscapeAvatarSpawnRequest> requestList = _requestQuery.ToComponentDataArray<LunarscapeAvatarSpawnRequest>(Allocator.TempJob);
			EntityCommandBuffer commandBuffer = CreateCommandBuffer(ref state);

			state.Dependency = new SpawnJob {
				commandBuffer = commandBuffer.AsParallelWriter(),
				requests = requestList,
			}.ScheduleParallel(_prefabQuery, state.Dependency);

			state.Dependency = requestList.Dispose(state.Dependency);

			// delete query
			state.Dependency = new DeleteRequestJob {
				commandBuffer = commandBuffer,
			}.Schedule(_requestQuery, state.Dependency);
		}
	
		void ISystem.OnDestroy(ref SystemState state) {
		}

		private readonly EntityCommandBuffer CreateCommandBuffer(ref SystemState state) {
			return SystemAPI
				.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
				.CreateCommandBuffer(state.World.Unmanaged);
		}

		partial struct SpawnJob : IJobEntity {
			[ReadOnly] public NativeArray<LunarscapeAvatarSpawnRequest> requests;
			public EntityCommandBuffer.ParallelWriter commandBuffer;

			void Execute(in Entity entity, [EntityIndexInQuery] int sortKey, RefRO<LunarscapeAvatar> avatar) {
				foreach(LunarscapeAvatarSpawnRequest request in requests){
					if (request.id == avatar.ValueRO.id) {
						Entity instance = commandBuffer.Instantiate(sortKey, entity);
						commandBuffer.SetComponent(sortKey, instance, LocalTransform.FromPositionRotation(request.position, request.rotation));
						commandBuffer.RemoveComponent<Parent>(sortKey, instance);

						if (request.playerIndex >= 0) {
							commandBuffer.AddComponent<LunarscapeFieldObservePoint>(sortKey, instance);
						}
					}
				}
			}
		}

		partial struct DeleteRequestJob : IJobEntity {
			public EntityCommandBuffer commandBuffer;

			void Execute(in Entity entity) {
				commandBuffer.DestroyEntity(entity);
			}
		}
	}

}
