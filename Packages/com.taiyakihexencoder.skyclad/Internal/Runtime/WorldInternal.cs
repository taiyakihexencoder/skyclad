using Unity.Entities;
using Unity.Transforms;

namespace skyclad.internalProc {
	public static class WorldInternal {
		private static Entity _rootEntity = Entity.Null;
		public static Entity RootEntity {
			get {
				if (_rootEntity == Entity.Null) {
					EntityManager entityManager = ECSUtilityInternal.World.EntityManager;

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

		public static void AddToRoot(Entity entity) {
			EntityManager entityManager = ECSUtilityInternal.World.EntityManager;
			if (!entityManager.HasComponent<Parent>(entity)) {
				entityManager.AddComponent<Parent>(entity);
			}

			entityManager.SetComponentData(
				entity,
				new Parent { Value = RootEntity, }
			);
		}
	}
}