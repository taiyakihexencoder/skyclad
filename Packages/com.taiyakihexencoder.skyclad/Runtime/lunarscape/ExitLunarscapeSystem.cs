using skyclad.lunarscape.internalProc;
using Unity.Collections;
using Unity.Entities;

namespace skyclad.lunarscape {
	[UpdateInGroup(typeof(LunarscapeSimulationSystemGroup))]
	public partial struct ExitLunarscapeSystem : ISystem {
		private EntityQuery _query;

		void ISystem.OnCreate(ref SystemState state) {
			_query = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<LunarscapeExitRequest>()
				.Build(ref state);

			state.RequireForUpdate(_query);
		}

		void ISystem.OnUpdate(ref SystemState state) {
			EntityCommandBuffer commandBuffer = CreateCommandBuffer(ref state);
			state.Dependency = new DeleteJob {
				commandBuffer = commandBuffer,
			}.Schedule(_query, state.Dependency);
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

			void Execute(in Entity entity) {
				commandBuffer.DestroyEntity(entity);
			}
		}
	}

}
