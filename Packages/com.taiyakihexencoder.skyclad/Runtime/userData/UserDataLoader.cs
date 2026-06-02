using System.IO;
using System.Threading.Tasks;
using Unity.Entities;
using UnityEngine;

namespace skyclad.userData {
	using internalProc;
	public static class UserDataLoader {
		private const string PATH = "transaction";
		private const string FILE_NAME = "userData_{0:D3}.bytes";

		private static Entity userDataEntity = Entity.Null;
		private static UserDataComponent loadedUserData = UserDataComponent.Default;

		/// <summary>
		/// セーブデータ読込
		/// </summary>
		/// <param name="slot"></param>
		/// <returns></returns>
		internal static async Task Load(int slot) {
			await Task.Run(async () => {
				string absPath = SkycladUtility.Async.Send(() => PreparePath(slot));

				loadedUserData = UserDataComponent.Default;
				try {
					if (File.Exists(absPath)) {
						byte[] bytes = await File.ReadAllBytesAsync(absPath);
						loadedUserData = UserDataComponent.FromBinary(bytes);

						SkycladUtility.Async.Log($"Load:{absPath}");
					}
				} catch (System.Exception e) {
					SkycladUtility.Async.LogError(e);
				}

				SkycladUtility.Async.Post(() => {
					ECSUtilityInternal.ExecuteCommandBufferTemp(
						commandBuffer => {
							userDataEntity = commandBuffer.CreateEntity();
							commandBuffer.SetName(userDataEntity, "User data");
							commandBuffer.AddComponent(userDataEntity, loadedUserData);
						}
					);
				});
			});
		}

		public static void RequestSave(EntityCommandBuffer commandBuffer, int slot) {
			Entity requestEntity = commandBuffer.CreateEntity();
			commandBuffer.SetName(requestEntity, "Request Save User Data");
			commandBuffer.AddComponent(
				requestEntity,
				new InternalRequestSaveUserDataComponent{
					slot = slot,
				}
			);
		}

		/// <summary>
		/// セーブする
		/// </summary>
		/// <param name="slot"></param>
		/// <param name="userData"></param>
		/// <returns></returns>
		internal static async Task Save(int slot) {
			if (
				SkycladUtility.Async.Send(
					() => SkycladUtility.ECS.EntityManager.Exists(userDataEntity)
				)
			) {
				UserDataComponent userData = UserDataComponent.Default;

				await Task.Run(async () => {
					string absPath = "";
					SkycladUtility.Async.Send(
						() => {
							UserDataComponent userData = SkycladUtility.ECS.EntityManager
								.GetComponentData<UserDataComponent>(userDataEntity);
							absPath = PreparePath(slot);
						}
					);

					try {
						userData.ToBinary(out byte[] bytes);
						await File.WriteAllBytesAsync(absPath, bytes);

						SkycladUtility.Async.Log($"Save:{absPath}");
					} catch (System.Exception e) {
						SkycladUtility.Async.LogError(e);
					}
				});
			}
		}

		/// <summary>
		/// エンティティを削除する
		/// </summary>
		internal static void Unload() {
			if (SkycladUtility.ECS.EntityManager.Exists(userDataEntity)) {
				SkycladUtility.ECS.ExecuteCommandBufferTemp(
					(commandBuffer) => commandBuffer.DestroyEntity(userDataEntity)
				);
			}
		}

		/// <summary>
		/// ロードした時のセーブデータで上書きする
		/// </summary>
		public static void Replace(EntityCommandBuffer commandBuffer) {
			commandBuffer.SetComponent(userDataEntity, loadedUserData);
		}

		private static string PreparePath(int slot) {
			string basePath = Application.streamingAssetsPath + Path.DirectorySeparatorChar + PATH;
			if (!Directory.Exists(basePath)) {
				Directory.CreateDirectory(basePath);
			}
			return basePath + Path.DirectorySeparatorChar + string.Format(FILE_NAME, slot);
		}
	}
}