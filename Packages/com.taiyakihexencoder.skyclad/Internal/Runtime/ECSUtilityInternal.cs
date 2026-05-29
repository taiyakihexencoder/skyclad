using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace skyclad.internalProc {
	public static class ECSUtilityInternal {
		public static World World => World.DefaultGameObjectInjectionWorld;
		public static EntityManager EntityManager => World.EntityManager;

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