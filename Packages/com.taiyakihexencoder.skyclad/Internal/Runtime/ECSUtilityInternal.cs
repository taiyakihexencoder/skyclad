using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace skyclad.internalProc {
	public static class ECSUtilityInternal {
		public static World World => World.DefaultGameObjectInjectionWorld;
		public static EntityManager EntityManager => World.EntityManager;

		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void SetDebugName(this EntityCommandBuffer commandBuffer, Entity entity, FixedString64Bytes name) {
			commandBuffer.SetName(entity, name);
		}

		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void SetDebugName(this EntityManager entityManager, Entity entity, FixedString64Bytes name) {
			entityManager.SetName(entity, name);
		}

		public static void ExecuteCommandBufferTemp(System.Action<EntityCommandBuffer> action) {
			EntityCommandBuffer commandBuffer = new EntityCommandBuffer(Allocator.Temp);
			action(commandBuffer);
			commandBuffer.Playback(EntityManager);
			commandBuffer.Dispose();
		}

		public static void ExecuteCommandBufferTempJob(System.Func<EntityCommandBuffer, JobHandle> job) {
			EntityCommandBuffer commandBuffer = new EntityCommandBuffer(Allocator.TempJob);
			job(commandBuffer).Complete();
			commandBuffer.Playback(EntityManager);
			commandBuffer.Dispose();
		}

		public static void ForEach<T>(this EntityQuery query, System.Action<T> action) where T: unmanaged, IComponentData {
			NativeArray<T> array = query.ToComponentDataArray<T>(Allocator.Temp);
			foreach(T component in array) {
				action(component);
			}
			array.Dispose();
		}

		public static void ForEach<T>(this EntityQuery query, System.Action<int, T> action) where T: unmanaged, IComponentData {
			NativeArray<T> array = query.ToComponentDataArray<T>(Allocator.Temp);
			for(int i = 0; i < array.Length; ++i) {
				action(i, array[i]);
			}
			array.Dispose();
		}

		public static void ForEach(this EntityQuery query, System.Action<Entity> action) {
			NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
			foreach (Entity entity in entities) {
				action(entity);
			}
			entities.Dispose();
		}

		public static void ForEach(this EntityQuery query, System.Action<int, Entity> action) {
			NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
			for (int i = 0; i < entities.Length; ++i) {
				action(i, entities[i]);
			}
			entities.Dispose();
		}

		public static void DestroyEntityInQuery(EntityQuery query) {
			EntityCommandBuffer commandBuffer = new EntityCommandBuffer(Allocator.Temp);
			commandBuffer.DestroyEntity(query, EntityQueryCaptureMode.AtPlayback);
			commandBuffer.Playback(EntityManager);
			commandBuffer.Dispose();
		}

		public static DestroyJob Destroy(ref this EntityCommandBuffer commandBuffer) {
			return new DestroyJob {
				commandBuffer = commandBuffer,
			};
		}

		public static DestroyParallelJob Destroy(ref this EntityQuery query, EntityCommandBuffer.ParallelWriter commandBuffer) {
			return new DestroyParallelJob {
				commandBuffer = commandBuffer,
			};
		}
	}

	public partial struct DestroyJob : IJobEntity {
		public EntityCommandBuffer commandBuffer;

		void Execute(in Entity entity) {
			commandBuffer.DestroyEntity(entity);
		}
	}

	public partial struct DestroyParallelJob : IJobEntity {
		public EntityCommandBuffer.ParallelWriter commandBuffer;

		void Execute([EntityIndexInQuery]int sortKey, in Entity entity) {
			commandBuffer.DestroyEntity(sortKey, entity);
		}
	}
}