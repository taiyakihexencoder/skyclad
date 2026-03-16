using Unity.Collections;
using Unity.Entities;

namespace skyclad.field {
	/// <summary>
	/// ロード開始と待機のみ、リクエストエンティティの削除は外で行う
	/// </summary>
	[UpdateInGroup(typeof(SkycladFieldSystemGroup))]
	public partial struct LoadFieldMeshSystem : ISystem {
		private EntityQuery query;
		private EntityQuery waitQuery;

		void ISystem.OnCreate(ref SystemState state) {
			query = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<RequestLoadFieldMeshComponent>()
				.WithNone<WaitTaskComponent>()
				.Build(ref state);
			state.RequireForUpdate(query);
		}

		void ISystem.OnUpdate(ref SystemState state) {
			EntityCommandBuffer commandBuffer = CreateCommandBuffer(ref state);
			state.Dependency = new StartLoadJob {
				commandBuffer = commandBuffer,
			}.Schedule(query, state.Dependency);
		}
	
		private readonly EntityCommandBuffer CreateCommandBuffer(ref SystemState state) {
			return SystemAPI
				.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
				.CreateCommandBuffer(state.World.Unmanaged);
		}

		partial struct StartLoadJob : IJobEntity {
			public EntityCommandBuffer commandBuffer;

			void Execute(in Entity entity, RefRO<RequestLoadFieldMeshComponent> request) {
				commandBuffer.AddComponent<WaitTaskComponent>(entity);
				FieldMeshLoader.StartLoad(entity, request.ValueRO);
			}
		}
	}
}
