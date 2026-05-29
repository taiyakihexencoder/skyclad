using Unity.Entities;
using UnityEngine;

namespace skyclad {
	using internalProc;
	public static class SkycladWorld {
		internal static void AddToRoot(Entity entity) { WorldInternal.AddToRoot(entity); }

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