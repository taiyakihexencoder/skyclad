using System.Diagnostics;
using Unity.Collections;
using Unity.Entities;

namespace skyclad {
	public static class DebugEntityExtensions {
		[Conditional("UNITY_EDITOR")]
		public static void Name(this Entity entity, EntityManager entityManager, FixedString64Bytes name) {
			entityManager.SetName(entity, name);
		}

		[Conditional("UNITY_EDITOR")]
		public static void Name(this Entity entity, ref SystemState state, FixedString64Bytes name) {
			state.EntityManager.SetName(entity, name);
		}

		[Conditional("UNITY_EDITOR")]
		public static void Name(this Entity entity, EntityCommandBuffer commandBuffer, FixedString64Bytes name) {
			commandBuffer.SetName(entity, name);
		}
	}
}