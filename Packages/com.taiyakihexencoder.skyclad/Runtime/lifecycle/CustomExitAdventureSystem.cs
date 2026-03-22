using Unity.Collections;
using Unity.Entities;

namespace skyclad.lifecycle {
	[UpdateInGroup(typeof(SkycladLifecycleSystemGroup))]
	public partial struct CustomExitAdventureSystem : ISystem {
		private EntityQuery awaitQuery;
		private EntityQuery query;

		void ISystem.OnCreate(ref SystemState state) {
			query = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<BeforeWorldUnload>()
				.WithNone<WaitTaskComponent>()
				.Build(ref state);
			state.RequireForUpdate(query);
		}

		void ISystem.OnUpdate(ref SystemState state) {
			state.Dependency = new AddWaitTaskJob {
				commandBuffer = CreateCommandBuffer(ref state),
			}.Schedule(query, state.Dependency);
			BaseLifecycle.EndWorldProcess(state.World);
		}
	
		void ISystem.OnDestroy(ref SystemState state) {
		}

		private readonly EntityCommandBuffer CreateCommandBuffer(ref SystemState state) {
			return SystemAPI
				.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
				.CreateCommandBuffer(state.World.Unmanaged);
		}

		partial struct AddWaitTaskJob : IJobEntity {
			public EntityCommandBuffer commandBuffer;

			void Execute(in Entity entity) {
				commandBuffer.AddComponent<WaitTaskComponent>(entity);
			}
		}

	}

}
