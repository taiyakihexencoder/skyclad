using Unity.Collections;
using Unity.Entities;

namespace skyclad.lifecycle {
	/// <summary>
	/// BaseLifecycle.StartWorldProcessを呼び出す。
	/// </summary>
	[UpdateInGroup(typeof(SkycladLifecycleSystemGroup))]
	public partial struct CustomEnterAdventureSystem : ISystem {
		private EntityQuery query;

		void ISystem.OnCreate(ref SystemState state) {
			query = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<AfterWorldLoad>()
				.WithNone<WaitTaskComponent>()
				.Build(ref state);

			state.RequireForUpdate(query);
		}

		void ISystem.OnUpdate(ref SystemState state) {
			BaseLifecycle.StartWorldProcess(state.World);
			state.Dependency = new AddWaitTaskJob {
				commandBuffer = CreateCommandBuffer(ref state),
			}.Schedule(query, state.Dependency);
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