using Unity.Collections;
using Unity.Entities;

namespace skyclad {
	using internalProc;

	[UpdateInGroup(typeof(SkycladDioramaSystemGroup))]
	public partial struct UnloadDioramaSystem : ISystem {
		private EntityQuery query;

		private EntityQuery waitQuery;
		private EntityQuery subTaskQuery;

		void ISystem.OnCreate(ref SystemState state) {
			query = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<RequestUnloadDioramaComponent>()
				.WithNone<WaitTaskComponent>()
				.Build(ref state);

			waitQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<RequestUnloadDioramaComponent, WaitTaskComponent>()
				.Build(ref state);

			subTaskQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<DioramaUnloadElementComponent>()
				.Build(ref state);

			state.RequireForUpdate<RequestUnloadDioramaComponent>();
		}

		void ISystem.OnUpdate(ref SystemState state) {
			EntityCommandBuffer commandBuffer = CreateCommandBuffer(ref state);

			if (!query.IsEmpty) {
				state.Dependency = new AddWaitTaskJob {
					commandBuffer = commandBuffer,
				}.Schedule(query, state.Dependency);

				// Unloadの各ジョブを開始する
				state.Dependency = new UnloadDioramaJob {
					commandBuffer = commandBuffer,
				}.Schedule(query, state.Dependency);
			} else if (!waitQuery.IsEmpty && subTaskQuery.IsEmpty) {
				// Unload完了
				state.Dependency = commandBuffer.Destroy()
					.Schedule(waitQuery, state.Dependency);
			}
		}

		private readonly EntityCommandBuffer CreateCommandBuffer(ref SystemState state) {
			return SystemAPI
				.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
				.CreateCommandBuffer(state.World.Unmanaged);
		}
	}
}