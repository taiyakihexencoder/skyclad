using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace skyclad {
	public static class SkycladWorld {
		private static Entity _rootEntity = Entity.Null;
		internal static Entity RootEntity {
			get {
				if (_rootEntity == Entity.Null) {
					EntityManager entityManager = SkycladUtility.ECS.EntityManager;

					_rootEntity = entityManager.CreateEntity(
						entityManager.CreateArchetype(
							new ComponentType[] {
								ComponentType.ReadWrite<LocalTransform>(),
								ComponentType.ReadWrite<LocalToWorld>(),
							}
						)
					);
					#if UNITY_EDITOR
					entityManager.SetName(_rootEntity, "Skyclad");
					#endif

				}
				return _rootEntity;
			}
		}

		internal static void AddToRoot(Entity entity) {
			EntityManager entityManager = SkycladUtility.ECS.EntityManager;
			if (!entityManager.HasComponent<Parent>(entity)) {
				entityManager.AddComponent<Parent>(entity);
			}

			entityManager.SetComponentData(
				entity,
				new Parent { Value = RootEntity, }
			);
		}

		/// <summary>
		/// アプリケーション開始
		/// </summary>
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void LaunchGame() {
			lifecycle.LifecycleFunctions.Launch(SkycladUtility.ECS.World);
			Application.quitting += QuitGame;
		}

		/// <summary>
		/// アプリケーション終了
		/// </summary>
		private static void QuitGame() {
			lifecycle.LifecycleFunctions.Quit(SkycladUtility.ECS.World);
		}

		/// <summary>
		/// インゲーム開始
		/// 非System用
		/// </summary>
		public static void EnterAdventure(int saveDataSlot) {
			lifecycle.LifecycleFunctions.EnterAdventure(SkycladUtility.ECS.World, saveDataSlot);
		}

		/// <summary>
		/// インゲーム開始
		/// System用
		/// </summary>
		public static void EnterAdventure(EntityCommandBuffer commandBuffer, int saveDataSlot) {
			lifecycle.LifecycleFunctions.EnterAdventure(commandBuffer, saveDataSlot);
		}

		/// <summary>
		/// インゲーム終了
		/// 非System用
		/// </summary>
		public static void ExitAdventure() {
			lifecycle.LifecycleFunctions.ExitAdventure(SkycladUtility.ECS.World);
		}

		/// <summary>
		/// インゲーム終了
		/// System用
		/// </summary>
		public static void ExitAdventure(EntityCommandBuffer commandBuffer) {
			lifecycle.LifecycleFunctions.ExitAdventure(commandBuffer);
		}
	}
}