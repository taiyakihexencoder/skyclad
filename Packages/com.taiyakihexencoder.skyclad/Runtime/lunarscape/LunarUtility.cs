using Unity.Entities;

namespace skyclad.lunarscape {
	using skyclad.lunarscape.internalProc;
	public static class LunarUtility {
		/// <summary>
		/// Lunarscapeを起動する
		/// </summary>
		public static void Enter(EntityManager entityManager) {
			Entity entity = entityManager.CreateEntity();
			entityManager.AddComponent<LunarscapeEnterRequest>(entity);
		}

		/// <summary>
		/// Lunarscapeを起動する
		/// </summary>
		/// <param name="commandBuffer"></param>
		public static void Enter(EntityCommandBuffer commandBuffer) {
			Entity entity = commandBuffer.CreateEntity();
			commandBuffer.AddComponent<LunarscapeEnterRequest>(entity);
		}

		/// <summary>
		/// Lunarscapeを終了する
		/// </summary>
		public static void Exit(EntityManager entityManager) {
			Entity entity = entityManager.CreateEntity();
			entityManager.AddComponent<LunarscapeExitRequest>(entity);
		}

		/// <summary>
		/// Lunarscapeを終了する
		/// </summary>
		/// <param name="commandBuffer"></param>
		public static void Exit(EntityCommandBuffer commandBuffer) {
			Entity entity = commandBuffer.CreateEntity();
			commandBuffer.AddComponent<LunarscapeExitRequest>(entity);
		}
	}
}