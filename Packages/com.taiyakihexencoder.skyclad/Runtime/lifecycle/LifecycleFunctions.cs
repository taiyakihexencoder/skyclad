using System.Threading;
using Unity.Entities;

namespace skyclad.lifecycle {
	using internalProc;
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
					SystemUtilityInternal.ApplyPhysics(
						world: world, 
						drawDebugColliders: true
					);
				}
			}

			// (仮)
			// EnterAdventure(world, 0);
		}

		/// <summary>
		/// インゲームの開始
		/// System以外
		/// </summary>
		internal static void EnterAdventure(World world, int saveDataSlot) {
			EntityManager entityManager = world.EntityManager;
			Entity entity = entityManager.CreateEntity();
			entityManager.AddComponent<RequestEnterAdventureComponent>(entity);

			// セーブデータ読み込み
			entityManager.CreateEntityBuilder()
				.AddRW<InternalRequestLoadUserDataComponent>()
				.Build(
					"request load user data", 
					new InternalRequestLoadUserDataComponent {
						slot = saveDataSlot,
					}
				);
		}

		/// <summary>
		/// インゲームの開始
		/// System用
		/// </summary>
		internal static void EnterAdventure(EntityCommandBuffer commandBuffer, int saveDataSlot) {
			Entity entity = commandBuffer.CreateEntity();
			commandBuffer.AddComponent<RequestEnterAdventureComponent>(entity);

			// セーブデータ読み込み
			Entity loadEntity = commandBuffer.CreateEntity();
			commandBuffer.AddComponent(
				loadEntity, 
				new InternalRequestLoadUserDataComponent {
					slot = saveDataSlot,
				}
			);
		} 

		/// <summary>
		/// インゲームの終了
		/// System以外
		/// </summary>
		internal static void ExitAdventure(World world) {
			EntityManager entityManager = world.EntityManager;
			Entity entity = entityManager.CreateEntity();
			entityManager.AddComponent<RequestExitAdventureComponent>(entity);
		}

		/// <summary>
		/// インゲームの終了
		/// System用
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
				// SkycladUtility.Resource.UnloadAll();
				field.FieldMeshLoader.DisposeAllResource();
			}
		}
	}
}