using Unity.Entities;
using UnityEngine;

namespace skyclad {
	using System.Threading.Tasks;
	using internalProc;
	using Unity.Collections;

	/// <summary>
	/// MonoBehaviourで対応するものをまとめる
	/// </summary>
	internal sealed class SkycladBehaviour : MonoBehaviour {
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void Initialize() {
			GameObject obj = new GameObject("Skyclad Behaviour");
			obj.AddComponent<SkycladBehaviour>();
			DontDestroyOnLoad(obj);
		}

		private EntityQuery _loadUserDataRequestQuery;
		private EntityQuery _saveUserDataRequestQuery;
		private EntityQuery _unloadUserDataRequestQuery;

		private void Awake() {
			EntityManager entityManager = ECSUtilityInternal.EntityManager;
			_loadUserDataRequestQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<InternalRequestLoadUserDataComponent>()
				.Build(entityManager);
			_saveUserDataRequestQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<InternalRequestSaveUserDataComponent>()
				.Build(entityManager);
			_unloadUserDataRequestQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<InternalRequestUnloadUserDataComponent>()
				.Build(entityManager);
		}

		private void Update() {
			if (!_loadUserDataRequestQuery.IsEmpty) { LoadUserData(); }
			if (!_saveUserDataRequestQuery.IsEmpty) { SaveUserData(); }
			if (!_unloadUserDataRequestQuery.IsEmpty) { UnloadUserData(); }
		}

		/// <summary>
		/// ユーザーデータの読み込み
		/// </summary>
		private void LoadUserData() {
			NativeArray<InternalRequestLoadUserDataComponent> array = _loadUserDataRequestQuery.ToComponentDataArray<InternalRequestLoadUserDataComponent>(Allocator.Temp);
			int slot = array[0].slot;

			ECSUtilityInternal.ExecuteCommandBufferTemp(
				commandBuffer => {
					commandBuffer.DestroyEntity(_loadUserDataRequestQuery, EntityQueryCaptureMode.AtPlayback);
				}
			);

			_ = Task.Run(async () => {
				await userData.UserDataLoader.Load(slot);
			});
		}

		/// <summary>
		/// ユーザーデータの保存
		/// </summary>
		private void SaveUserData() {
			NativeArray<InternalRequestSaveUserDataComponent> array = _saveUserDataRequestQuery.ToComponentDataArray<InternalRequestSaveUserDataComponent>(Allocator.Temp);
			int slot = array[0].slot;

			ECSUtilityInternal.ExecuteCommandBufferTemp(
				commandBuffer => {
					commandBuffer.DestroyEntity(_saveUserDataRequestQuery, EntityQueryCaptureMode.AtPlayback);
				}
			);

			_ = Task.Run(async () => {
				await userData.UserDataLoader.Save(slot);
			});
		}

		/// <summary>
		/// ユーザーデータのアンロード
		/// </summary>
		private void UnloadUserData() {
			ECSUtilityInternal.ExecuteCommandBufferTemp(
				commandBuffer => {
					commandBuffer.DestroyEntity(_unloadUserDataRequestQuery, EntityQueryCaptureMode.AtPlayback);
				}
			);

			_ = Task.Run(async () => {
				await userData.UserDataLoader.Unload();
			});
		}
	}
}