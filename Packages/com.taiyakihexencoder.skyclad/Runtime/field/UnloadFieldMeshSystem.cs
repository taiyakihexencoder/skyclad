using Unity.Collections;
using Unity.Entities;

namespace skyclad.field {
	/// <summary>
	/// フィールドリソースのアンロード
	/// </summary>
	[UpdateInGroup(typeof(SkycladFieldSystemGroup))]
	public partial struct UnloadFieldMeshSystem : ISystem {
		private EntityQuery query;
		private EntityQuery fieldMeshQuery;

		void ISystem.OnCreate(ref SystemState state) {
			query = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<RequestUnloadFieldMeshComponent>()
				.Build(ref state);
			state.RequireForUpdate(query);

			fieldMeshQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<FieldMeshComponent>()
				.Build(ref state);
		}

		void ISystem.OnUpdate(ref SystemState state) {
			EntityCommandBuffer commandBuffer = CreateCommandBuffer(ref state);

			NativeArray<RequestUnloadFieldMeshComponent> unloadRequestArray = query.ToComponentDataArray<RequestUnloadFieldMeshComponent>(Allocator.TempJob);
			state.Dependency = new DeleteJob {
				commandBuffer = commandBuffer.AsParallelWriter(),
				unloadRequestArray = unloadRequestArray,
			}.Schedule(fieldMeshQuery, state.Dependency);
			state.Dependency = unloadRequestArray.Dispose(state.Dependency);

			state.Dependency = new ReleaseBlobsJob {
				commandBuffer = commandBuffer,
			}.Schedule(query, state.Dependency);
		}
	
		private readonly EntityCommandBuffer CreateCommandBuffer(ref SystemState state) {
			return SystemAPI
				.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
				.CreateCommandBuffer(state.World.Unmanaged);
		}

		partial struct DeleteJob : IJobEntity {
			public EntityCommandBuffer.ParallelWriter commandBuffer;
			[ReadOnly] public NativeArray<RequestUnloadFieldMeshComponent> unloadRequestArray;

			void Execute(in Entity entity, [EntityIndexInQuery] int sortKey, RefRO<FieldMeshComponent> component) {
				int meshId;
				for (int i = 0; i < unloadRequestArray.Length; ++i) {
					meshId = unloadRequestArray[i].meshId;
					if (component.ValueRO.meshId == meshId) {
						commandBuffer.DestroyEntity(sortKey, entity);
						break;
					}
				}
			}
		}

		partial struct ReleaseBlobsJob : IJobEntity {
			public EntityCommandBuffer commandBuffer;

			void Execute(in Entity entity, RefRO<RequestUnloadFieldMeshComponent> request) {
				FieldMeshLoader.UnloadMeshResources(request.ValueRO.meshId);
				commandBuffer.DestroyEntity(entity);
			}			
		}
	}
}
