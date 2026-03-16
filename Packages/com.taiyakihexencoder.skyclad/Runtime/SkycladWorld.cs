using Unity.Entities;
using Unity.Transforms;

namespace skyclad {
	public static class SkycladWorld {
		private static Entity _rootEntity = Entity.Null;
		internal static Entity RootEntity {
			get {
				if (_rootEntity == Entity.Null) {
					World world = World.DefaultGameObjectInjectionWorld;
					EntityManager entityManager = world.EntityManager;

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

		public static void Start() {
			EntityManager entityManager = SkycladUtility.ECS.EntityManager;

			EntityArchetype archetype = entityManager.CreateArchetype(
				ComponentType.ReadWrite<RequestLoadDioramaComponent>()
			);

			foreach(int dioramaId in DioramaId.LaunchDioramas) {
				Entity entity = entityManager.CreateEntity(archetype);
				entityManager.SetComponentData(
					entity, 
					new RequestLoadDioramaComponent {
						id = dioramaId,
					}
				);
			}
		}

		public static void End() {
			EntityManager entityManager = SkycladUtility.ECS.EntityManager;
			EntityArchetype archetype = entityManager.CreateArchetype(
				ComponentType.ReadWrite<RequestUnloadDioramaComponent>()
			);
			foreach(int dioramaId in DioramaId.LaunchDioramas) {
				Entity entity = entityManager.CreateEntity(archetype);
				entityManager.SetComponentData(
					entity,
					new RequestUnloadDioramaComponent {
						id = dioramaId,
					}
				);
			}
		}
	}
}