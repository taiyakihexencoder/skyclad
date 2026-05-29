using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace skyclad {
	using internalProc;
	public static partial class SkycladUtility {
		public static class ECS {
			public const uint DISABLED_PHYSICS_INDEX = 141;
			public const uint ENABLED_PHYSICS_INDEX = 0;
			public const float DAMAGED_INVINCIBLE_SECONDS = 0.25f;

			public static World World => ECSUtilityInternal.World;
			public static EntityManager EntityManager => ECSUtilityInternal.EntityManager;
			public static Entity CreateEntity(FixedString64Bytes name, System.Action<EntityManager, Entity> action) {
				EntityManager entityManager = EntityManager;
				Entity entity = entityManager.CreateEntity();
				entity.Name(entityManager, name);
				action(entityManager, entity);
				return entity;
			}

			public static Entity CreateEntity(FixedString64Bytes name, EntityArchetype archetype, System.Action<EntityManager, Entity> action) {
				EntityManager entityManager = EntityManager;
				Entity entity = entityManager.CreateEntity(archetype);
				entity.Name(entityManager, name);
				action(entityManager, entity);
				return entity;
			}

			/// <summary>
			/// フレーム内でのEntityCommandBufferの処理
			/// JobScheduleなどはExecuteCommandBufferTempJobを使う
			/// </summary>
			/// <param name="action"></param>
			public static void ExecuteCommandBufferTemp(System.Action<EntityCommandBuffer> action) {
				ECSUtilityInternal.ExecuteCommandBufferTemp(action);
			}
			
			public static void ExecuteCommandBufferTempJob(System.Func<EntityCommandBuffer, JobHandle> job) {
				ECSUtilityInternal.ExecuteCommandBufferTempJob(job);
			}
		}
	}
}