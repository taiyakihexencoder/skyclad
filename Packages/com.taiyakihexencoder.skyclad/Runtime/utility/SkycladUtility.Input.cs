using Unity.Entities;

namespace skyclad {
	using input;
	public static partial class SkycladUtility {
		public static class Input {
			/// <summary>
			/// 入力エンティティの作成
			/// </summary>
			/// <param name="entityManager"></param>
			/// <param name="enabled"></param>
			/// <param name="axis"></param>
			/// <param name="mainButtonState"></param>
			/// <param name="sideButtonState"></param>
			/// <param name="otherButtonState"></param>
			/// <param name="downEvents"></param>
			/// <param name="upEvents"></param>
			/// <returns></returns>
			public static Entity CreateInputEntity(
				EntityManager entityManager,
				bool enabled,
				bool axis,
				bool mainButtonState,
				bool sideButtonState,
				bool otherButtonState,
				bool downEvents,
				bool upEvents
			) {
				int count = (axis ? 1 : 0)
					+ (mainButtonState ? 1 : 0)
					+ (sideButtonState ? 1 : 0)
					+ (otherButtonState ? 1 : 0)
					+ (downEvents ? 1 : 0)
					+ (upEvents ? 1 : 0);

				ComponentType[] types = new ComponentType[count];
				int n = 0;
				if (axis) {
					types[n] = ComponentType.ReadWrite<InputAxisStateComponent>();
					n++;
				}
				if (mainButtonState) {
					types[n] = ComponentType.ReadWrite<InputMainButtonStateComponent>();
					n++;
				}
				if (sideButtonState) {
					types[n] = ComponentType.ReadWrite<InputSideButtonStateComponent>();
					n++;
				}
				if (otherButtonState) {
					types[n] = ComponentType.ReadWrite<InputOtherButtonStateComponent>();
					n++;
				}
				if (downEvents) {
					types[n] = ComponentType.ReadWrite<InputButtonDownEventBufferElement>();
					n++;
				}
				if (upEvents) {
					types[n] = ComponentType.ReadWrite<InputButtonUpEventBufferElement>();
					n++;
				}

				Entity entity = entityManager.CreateEntity(
					entityManager.CreateArchetype(types)
				);

				if (enabled) {
					Entity requestEntity = entityManager.CreateEntity(
						entityManager.CreateArchetype(ComponentType.ReadWrite<RequestUpdateInputListenerComponent>())
					);
#if UNITY_EDITOR
					entityManager.SetName(entity, "Update Input Listener Request");
#endif
					entityManager.SetComponentData(
						requestEntity, 
						new RequestUpdateInputListenerComponent {
							target = entity,
							enabled = true,
						}
					);
				}

				return entity;
			}

			/// <summary>
			/// 入力エンティティの作成
			/// </summary>
			/// <param name="commandBuffer"></param>
			/// <param name="enabled"></param>
			/// <param name="axis"></param>
			/// <param name="mainButtonState"></param>
			/// <param name="sideButtonState"></param>
			/// <param name="otherButtonState"></param>
			/// <param name="downEvents"></param>
			/// <param name="upEvents"></param>
			/// <returns></returns>
			public static Entity CreateInputEntity(
				EntityCommandBuffer commandBuffer,
				bool enabled,
				bool axis,
				bool mainButtonState,
				bool sideButtonState,
				bool otherButtonState,
				bool downEvents,
				bool upEvents
			) {
				Entity entity = commandBuffer.CreateEntity();
				if (axis) {
					commandBuffer.AddComponent(entity, new InputAxisStateComponent());
				}
				if (mainButtonState) {
					commandBuffer.AddComponent(entity, new InputMainButtonStateComponent());
				}
				if (sideButtonState) {
					commandBuffer.AddComponent(entity, new InputSideButtonStateComponent());
				}
				if (otherButtonState) {
					commandBuffer.AddComponent(entity, new InputOtherButtonStateComponent());
				}
				if (downEvents) {
					commandBuffer.AddBuffer<InputButtonDownEventBufferElement>(entity);
				}
				if (upEvents) {
					commandBuffer.AddBuffer<InputButtonUpEventBufferElement>(entity);
				}

				if (enabled) {
					Entity requestEntity = commandBuffer.CreateEntity();
#if UNITY_EDITOR
					commandBuffer.SetName(entity, "Update Input Listener Request");
#endif
					commandBuffer.SetComponent(
						requestEntity, 
						new RequestUpdateInputListenerComponent {
							target = entity,
							enabled = true,
						}
					);
				}

				return entity;
			}

			/// <summary>
			/// 入力の無効化
			/// </summary>
			/// <param name="entityManager"></param>
			/// <param name="entity"></param>
			/// <param name="enabled"></param>
			public static void RequestInputEnabled(
				EntityManager entityManager,
				Entity entity,
				bool enabled
			) {
				Entity requestEntity = entityManager.CreateEntity(
					entityManager.CreateArchetype(ComponentType.ReadWrite<RequestUpdateInputListenerComponent>())
				);
#if UNITY_EDITOR
				entityManager.SetName(entity, "Update Input Listener Request");
#endif
				entityManager.SetComponentData(
					requestEntity, 
					new RequestUpdateInputListenerComponent {
						target = entity,
						enabled = enabled,
					}
				);
			}

			/// <summary>
			/// 入力の無効化
			/// </summary>
			/// <param name="commandBuffer"></param>
			/// <param name="entity"></param>
			/// <param name="enabled"></param>
			public static void RequestInputEnabled(
				EntityCommandBuffer commandBuffer,
				Entity entity,
				bool enabled
			) {
				Entity requestEntity = commandBuffer.CreateEntity();
#if UNITY_EDITOR
				commandBuffer.SetName(entity, "Update Input Listener Request");
#endif
				commandBuffer.SetComponent(
					requestEntity, 
					new RequestUpdateInputListenerComponent {
						target = entity,
						enabled = enabled,
					}
				);
			}
		}
	}
}