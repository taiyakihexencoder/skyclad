using Unity.Collections;
using Unity.Entities;

namespace skyclad.input {
	/// <summary>
	/// 入力を検知するエンティティの入力ON/OFFを切り替える
	/// </summary>
	[UpdateInGroup(typeof(SkycladInputSystemGroup))]
	public partial struct UpdateInputListenerSystem : ISystem {
		private Entity inputControlEntity;

		private EntityQuery query;

		void ISystem.OnCreate(ref SystemState state) {
			EntityManager entityManager = state.EntityManager;
			inputControlEntity = entityManager.CreateEntity(
				entityManager.CreateArchetype(
					typeof(InputListenerBufferElement)
				)
			);

#if UNITY_EDITOR
			entityManager.SetName(inputControlEntity, "Input Control Entity");
#endif

			query = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<RequestUpdateInputListenerComponent>()
				.Build(ref state);
			state.RequireForUpdate(query);
		}

		void ISystem.OnUpdate(ref SystemState state) {
			EntityCommandBuffer commandBuffer = CreateCommandBuffer(ref state);

			DynamicBuffer<InputListenerBufferElement> inputListeners = SystemAPI.GetBuffer<InputListenerBufferElement>(inputControlEntity);

			Entity target;
			bool exists;
			foreach((
				RefRO<RequestUpdateInputListenerComponent> request, 
				Entity updateInputListenerEntity
			) in SystemAPI.Query<RefRO<RequestUpdateInputListenerComponent>>().WithEntityAccess()) {
				target = request.ValueRO.target;
				exists = false;
				for(int i = 0; i < inputListeners.Length; ++i) {
					if (inputListeners[i].target == target) {
						inputListeners[i] = new InputListenerBufferElement {
							target = target,
							enabled = request.ValueRO.enabled,
						};
						exists = true;
						break;
					}
				}

				if (!exists) {
					inputListeners.Add(
						new InputListenerBufferElement {
							target = target,
							enabled = request.ValueRO.enabled,
						}
					);
				}
			}

			commandBuffer.DestroyEntity(query, EntityQueryCaptureMode.AtPlayback);
		}

		readonly void ISystem.OnDestroy(ref SystemState state) {
			state.EntityManager.DestroyEntity(inputControlEntity);
		}

		private readonly EntityCommandBuffer CreateCommandBuffer(ref SystemState state) {
			return SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.World.Unmanaged);
		}

		public static Entity UpdateRequest(
			EntityCommandBuffer commandBuffer,
			Entity target,
			bool enabled
		) {
			Entity entity = commandBuffer.CreateEntity();
			commandBuffer.AddComponent(
				entity, 
				new RequestUpdateInputListenerComponent{
					target = target,
					enabled = enabled,
				}
			);
#if UNITY_EDITOR
			commandBuffer.SetName(entity, "Update Input Listener Entity");
#endif
			return entity;
		}

		public static Entity UpdateRequest(
			EntityManager entityManager, 
			Entity target,
			bool enabled
		) {
			Entity entity = entityManager.CreateEntity(
				entityManager.CreateArchetype(
					ComponentType.ReadWrite<RequestUpdateInputListenerComponent>()
				)
			);

			entityManager.SetComponentData(
				entity,
				new RequestUpdateInputListenerComponent {
					target = target,
					enabled = enabled,
				}
			);

#if UNITY_EDITOR
			entityManager.SetName(entity, "Update Input Listener Entity");
#endif
			return entity;
		}
	}
}