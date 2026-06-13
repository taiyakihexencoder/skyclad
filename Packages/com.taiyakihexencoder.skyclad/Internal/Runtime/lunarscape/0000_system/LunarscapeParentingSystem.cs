using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace skyclad.lunarscape.internalProc {
	using skyclad.internalProc;

	[UpdateInGroup(typeof(LunarscapeSimulationSystemGroup), OrderFirst = true)]
	public partial struct LunarscapeParentingSystem : ISystem {
		private EntityQuery query;

		private Entity _rootEntity;

		void ISystem.OnCreate(ref SystemState state) {
			_rootEntity = Entity.Null;
			query = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<LunarscapeParentingRequest>()
				.WithAllRW<Parent>()
				.Build(ref state);
			state.RequireForUpdate(query);
		}

		private void CreateRootEntity(EntityCommandBuffer commandBuffer) {
			commandBuffer.AddComponent(_rootEntity, LocalTransform.Identity);
			commandBuffer.AddComponent(_rootEntity, new LocalToWorld{ Value = Unity.Mathematics.float4x4.identity, });
			commandBuffer.SetDebugName(_rootEntity, "Lunarscape");
		}

		void ISystem.OnUpdate(ref SystemState state) {
			EntityCommandBuffer commandBuffer = CreateCommandBuffer(ref state);

			if (_rootEntity == Entity.Null) {
				_rootEntity = state.EntityManager.CreateEntity();
				CreateRootEntity(commandBuffer);
			}

			state.Dependency = new ParentingJob {
				parent = _rootEntity,
				commandBuffer = commandBuffer,
			}.Schedule(query, state.Dependency);
		}
	
		void ISystem.OnDestroy(ref SystemState state) { }

		partial struct ParentingJob : IJobEntity {
			public EntityCommandBuffer commandBuffer;
			public Entity parent;

			void Execute(in Entity entity) {
				commandBuffer.RemoveComponent<LunarscapeParentingRequest>(entity);
				commandBuffer.SetComponent(entity, new Parent{ Value = parent, });
			}
		}

		private readonly EntityCommandBuffer CreateCommandBuffer(ref SystemState state) {
			return SystemAPI
				.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
				.CreateCommandBuffer(state.World.Unmanaged);
		}
	}
}
