using Unity.Collections;
using Unity.Entities;

namespace skyclad.lunarscape.internalProc {
	/// <summary>
	/// 指定したAvatarのPrefabを削除する
	/// </summary>
	[UpdateInGroup(typeof(LunarscapeSimulationAvatarSystemGroup))]
	public partial struct LunarscapeAvatarDeletePrefabSystem : ISystem {
		private EntityQuery _query;
		private EntityQuery _prefabQuery;

		void ISystem.OnCreate(ref SystemState state) {
			_query = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<LunarscapeAvatarDeletePrefabRequest>()
				.Build(ref state);
			state.RequireForUpdate(_query);

			_prefabQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithOptions(EntityQueryOptions.IncludePrefab)
				.WithAll<LunarscapeAvatar>()
				.Build(ref state);
		}

		void ISystem.OnUpdate(ref SystemState state) {
			EntityCommandBuffer commandBuffer = CreateCommandBuffer(ref state);

			NativeArray<LunarscapeAvatarDeletePrefabRequest> requests = _query.ToComponentDataArray<LunarscapeAvatarDeletePrefabRequest>(Allocator.Temp);

			state.Dependency = new DeleteJob {
				commandBuffer = commandBuffer,
				requests = requests,
			}.Schedule(_prefabQuery, state.Dependency);

			state.Dependency = requests.Dispose(state.Dependency);

			commandBuffer.DestroyEntity(_query, EntityQueryCaptureMode.AtPlayback);
		}
	
		void ISystem.OnDestroy(ref SystemState state) {
		}

		private readonly EntityCommandBuffer CreateCommandBuffer(ref SystemState state) {
			return SystemAPI
				.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
				.CreateCommandBuffer(state.World.Unmanaged);
		}

		partial struct DeleteJob : IJobEntity {
			public EntityCommandBuffer commandBuffer;
			[ReadOnly] public NativeArray<LunarscapeAvatarDeletePrefabRequest> requests;

			void Execute(in Entity entity, RefRO<LunarscapeAvatar> component) {
				foreach(LunarscapeAvatarDeletePrefabRequest request in requests) {
					if (component.ValueRO.id == request.id) {
						commandBuffer.DestroyEntity(entity);
						break;
					}
				}
			}
		}
	}
}
