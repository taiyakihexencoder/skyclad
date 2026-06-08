using Unity.Collections;
using Unity.Entities;

namespace skyclad.lunarscape.internalProc {
	[UpdateInGroup(typeof(LunarscapeSimulationSystemGroup))]
	public partial struct LunarscapeExitSystem : ISystem {
		private EntityQuery _query;
		private EntityQuery _lunarscapeQuery;

		void ISystem.OnCreate(ref SystemState state) {
			_query = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<LunarscapeExitRequest>()
				.Build(ref state);
			_lunarscapeQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<LunarscapeInstance>()
				.Build(ref state);

			state.RequireForUpdate(_query);
		}

		void ISystem.OnUpdate(ref SystemState state) {
			EntityCommandBuffer commandBuffer = CreateCommandBuffer(ref state);
			if (!_lunarscapeQuery.IsEmpty) {
				commandBuffer.DestroyEntity(_lunarscapeQuery, EntityQueryCaptureMode.AtPlayback);
				FinalizeLunarscape(ref state);
			}
			commandBuffer.DestroyEntity(_query, EntityQueryCaptureMode.AtPlayback);
		}
	
		private void FinalizeLunarscape(ref SystemState state) {
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
