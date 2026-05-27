using System.Threading;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Authoring;

namespace skyclad.lifecycle {
	public static class LifecycleFunctions {
		/// <summary>
		/// ゲーム立ち上げ
		/// </summary>
		internal static void Launch(World world) {
			using (Process process = new Process("Launch process")) {
				SynchronizationContext mainContext = SynchronizationContext.Current;

				SkycladDataTables.Init(mainContext);
				character.CharacterCollider.InitColliders();
			
				EntityManager entityManager = world.EntityManager;

				using (process.Child("Initialize physics")) {
					PhysicsStep physicsStep = PhysicsStep.Default;
					physicsStep.SynchronizeCollisionWorld = 1;
					physicsStep.SimulationType = SimulationType.UnityPhysics;
					Entity entity = entityManager.CreateSingleton(physicsStep, "Physics");
			
					// Debug
					entityManager.AddComponentData(entity, new PhysicsDebugDisplayData { 
						DrawColliders = 1, 
					});
				}
			}

			// (仮)
			// EnterAdventure(world, 0);
		}

		/// <summary>
		/// インゲームの開始
		/// </summary>
		internal static void EnterAdventure(EntityCommandBuffer commandBuffer, int saveDataSlot) {
			Entity entity = commandBuffer.CreateEntity();
			commandBuffer.AddComponent<RequestEnterAdventureComponent>(entity);

			System.Threading.Tasks.Task.Run(
				async() => {
					await userData.UserDataLoader.Load(saveDataSlot);
				}
			);
		}

		/// <summary>
		/// インゲームの終了
		/// </summary>
		internal static void ExitAdventure(EntityCommandBuffer commandBuffer) {
			Entity entity = commandBuffer.CreateEntity();
			commandBuffer.AddComponent<RequestExitAdventureComponent>(entity);
		}

		/// <summary>
		/// ゲームの終了処理
		/// </summary>
		internal static void Quit(World world) {
			using (Process process = new Process("Quit process")) {
				SkycladDataTables.Dispose();
				character.CharacterCollider.DisposeColliders();

				// 念のためクリーンアップ
				SkycladUtility.Resource.UnloadAll();
				field.FieldMeshLoader.DisposeAllResource();
			}
		}
	}
}