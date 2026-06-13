using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace skyclad.lunarscape.internalProc {
	using skyclad.internalProc;

	[UpdateInGroup(typeof(LunarscapeSimulationFieldSystemGroup))]
	public partial struct LunarscapeFieldParentingSystem : ISystem {
		private EntityQuery query;

		private Entity _rootEntity;

		void ISystem.OnCreate(ref SystemState state) {
			query = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<LunarscapeFieldComponent>()
				.WithNone<Parent>()
				.Build(ref state);
			state.RequireForUpdate(query);
		}

		private void CreateRootEntity(EntityCommandBuffer commandBuffer) {
			commandBuffer.AddComponent<Parent>(_rootEntity);
			commandBuffer.AddComponent<LunarscapeParentingRequest>(_rootEntity);
			commandBuffer.AddComponent(_rootEntity, LocalTransform.Identity);
			commandBuffer.AddComponent(_rootEntity, new LocalToWorld{ Value = Unity.Mathematics.float4x4.identity, });
			commandBuffer.SetDebugName(_rootEntity, "Lunarscape Field");
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
				commandBuffer.AddComponent(entity, new Parent{ Value = parent, });
			}
		}

		private readonly EntityCommandBuffer CreateCommandBuffer(ref SystemState state) {
			return SystemAPI
				.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
				.CreateCommandBuffer(state.World.Unmanaged);
		}
	}
}
