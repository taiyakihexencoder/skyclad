using Unity.Collections;
using Unity.Entities;

namespace skyclad.lunarscape.internalProc {
	[UpdateInGroup(typeof(LunarscapeSimulationSystemGroup))]
	public partial struct LunarscapeEntranceSystem : ISystem {
		private EntityQuery _query;

		void ISystem.OnCreate(ref SystemState state) {
			_query = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<LunarscapeEnterRequest>()
				.Build(ref state);

			state.RequireForUpdate(_query);
		}

		void ISystem.OnUpdate(ref SystemState state) {
			EntityCommandBuffer commandBuffer = CreateCommandBuffer(ref state);
			if (! SystemAPI.TryGetSingleton(out LunarscapeInstance lunarscape)) {
				Entity instance = commandBuffer.CreateEntity();
				commandBuffer.AddComponent(instance, new LunarscapeInstance{ });

				InitLunarscape(ref state);
			}
			commandBuffer.DestroyEntity(_query, EntityQueryCaptureMode.AtPlayback);
		}
	
		private void InitLunarscape(ref SystemState state) {

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
