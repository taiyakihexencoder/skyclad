using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Entities;

namespace skyclad.userData {
	using internalProc;
	using Unity.Transforms;

	public static class UserDataLoader {
		private static Entity userDataEntity = Entity.Null;
		private static UserDataComponent loadedUserData = UserDataComponent.Default;

		/// <summary>
		/// セーブデータ読込（System）
		/// </summary>
		/// <param name="commandBuffer"></param>
		/// <param name="slot"></param>
		public static void RequestLoad(EntityCommandBuffer commandBuffer, int slot) {
			SaveUtilityInternal.UserData.CreateLoadRequest(commandBuffer, slot);
		}

		/// <summary>
		/// セーブデータ書込（System）
		/// </summary>
		/// <param name="commandBuffer"></param>
		/// <param name="slot"></param>
		public static void RequestSave(EntityCommandBuffer commandBuffer, int slot) {
			SaveUtilityInternal.UserData.CreateSaveRequest(commandBuffer, slot);
		}

		/// <summary>
		/// セーブデータのアンロード・エンティティ削除（System）
		/// </summary>
		/// <param name="commandBuffer"></param>
		public static void RequestUnload(EntityCommandBuffer commandBuffer) {
			SaveUtilityInternal.UserData.CreateUnloadRequest(commandBuffer);
		}

		/// <summary>
		/// セーブデータ読込
		/// </summary>
		/// <param name="slot"></param>
		/// <returns></returns>
		public static async Task Load(int slot) {
			AsyncUtilityInternal.Send(
				() => {
					if (!ECSUtilityInternal.EntityManager.Exists(userDataEntity)) {
						userDataEntity = ECSUtilityInternal.EntityManager.CreateEntityBuilder()
							.AddRW<UserDataComponent, Parent>()
							.Build("SaveData");

						SkycladWorld.AddToRoot(userDataEntity);
					}
				}
			);

			loadedUserData = UserDataComponent.Default;
			await SaveUtilityInternal.UserData.Load(
				slot,
				(bytes) => {
					loadedUserData = UserDataComponent.FromBinary(bytes);
				}
			);

			AsyncUtilityInternal.Send(
				() => {
					ECSUtilityInternal.EntityManager.SetComponentData(userDataEntity, loadedUserData);
				}
			);

		}

		/// <summary>
		/// セーブする
		/// </summary>
		/// <param name="slot"></param>
		/// <param name="userData"></param>
		/// <returns></returns>
		public static async Task Save(int slot) {
			bool exists = AsyncUtilityInternal.Send(
				() => ECSUtilityInternal.EntityManager.Exists(userDataEntity)
			);

			if (exists) {
				UserDataComponent userData = AsyncUtilityInternal.Send(
					() => SkycladUtility.ECS.EntityManager.GetComponentData<UserDataComponent>(userDataEntity)
				);
				userData.ToBinary(out byte[] bytes);
				await SaveUtilityInternal.UserData.Save(slot, bytes);
			}
		}

		/// <summary>
		/// セーブデータのアンロード・エンティティ削除
		/// </summary>
		public static async Task Unload() {
			await SaveUtilityInternal.UserData.Unload(userDataEntity);
		}

		/// <summary>
		/// ロードした時のセーブデータで上書きする（System）
		/// </summary>
		public static void Replace(EntityCommandBuffer commandBuffer) {
			commandBuffer.SetComponent(userDataEntity, loadedUserData);
		}

		/// <summary>
		/// ロードした時のセーブデータで上書きする
		/// </summary>
		public static void Replace() {
			ECSUtilityInternal.ExecuteCommandBufferTemp(
				commandBuffer => Replace(commandBuffer)
			);
		}

		/**
		 * 保存済スロットのリストを取得する。
		 */
		public static List<int> GetSavedSlots() {
			return SaveUtilityInternal.UserData.GetSavedSlots();
		}
	}
}