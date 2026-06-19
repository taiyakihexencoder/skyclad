using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace skyclad.lunarscape {
	using skyclad.internalProc;
	using skyclad.lunarscape.internalProc;

	internal class Lunarscaper : MonoBehaviour {
		private static Lunarscaper _instance = null;

		private EntityQuery _enterQuery;

		private EntityQuery _loadTableGroupQuery;
		private EntityQuery _unloadTableGroupQuery;
		private EntityQuery _tableLoadingQuery;

		private LunarscapeEntrySetting[] _entrySettings = new LunarscapeEntrySetting[0];

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void CreateInstance() {
			if (_instance == null) {
				GameObject obj = new GameObject("Lunarscaper");
				_instance = obj.AddComponent<Lunarscaper>();

				// （仮）デバッグ用の開始処理
				ECSUtilityInternal.ExecuteCommandBufferTemp(commandBuffer => {
					Enter(
						commandBuffer, 
						new LunarscapeEntry {
							entryName = "Player",
							position = new float3(0f, 60f, 0f),
							rotation = quaternion.identity,
						}
					);
				});
			}
		}

		public static void Enter(
			EntityCommandBuffer commandBuffer,
			LunarscapeEntry entry
		) {
			Entity entity = commandBuffer.CreateEntity();
			commandBuffer.AddComponent<LunarscapeEnterRequest>(entity);
			DynamicBuffer<LunarscapeEntrySetting> entryPoint = commandBuffer.AddBuffer<LunarscapeEntrySetting>(entity);
			entryPoint.Add(
				new LunarscapeEntrySetting { 
					entryName = entry.entryName,
					position = entry.position, 
					rotation = entry.rotation,
				}
			);
		}

		public static void Enter(
			EntityCommandBuffer commandBuffer,
			DynamicBuffer<LunarscapeEntry> entries
		) {
			Entity entity = commandBuffer.CreateEntity();
			commandBuffer.AddComponent<LunarscapeEnterRequest>(entity);
			DynamicBuffer<LunarscapeEntrySetting> entryPoint = commandBuffer.AddBuffer<LunarscapeEntrySetting>(entity);
			foreach (LunarscapeEntry entry in entries) {
				entryPoint.Add(
					new LunarscapeEntrySetting {
						entryName = entry.entryName,
						position = entry.position,
						rotation = entry.rotation,
					}
				);
			}
		}

		public static void Exit(EntityCommandBuffer commandBuffer) {
			Entity entity = commandBuffer.CreateEntity();
			commandBuffer.AddComponent<LunarscapeExitRequest>(entity);
		}

		private void Awake() {
			DontDestroyOnLoad(gameObject);

			LunarscapeFieldBehaviour field = LunarscapeFieldBehaviour.CreateInstance();
			field.transform.SetParent(transform);

			_enterQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<LunarscapeEnterRequest>()
				.Build(ECSUtilityInternal.EntityManager);

			_loadTableGroupQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<LunarscapeLoadTableGroupRequest>()
				.Build(ECSUtilityInternal.EntityManager);
			_unloadTableGroupQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<LunarscapeUnloadTableGroupRequest>()
				.Build(ECSUtilityInternal.EntityManager);
			_tableLoadingQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<LunarscapeTableLoadingFlag>()
				.Build(ECSUtilityInternal.EntityManager);
		}

		private void Update() {
			if (!_enterQuery.IsEmpty) { 
				EnterLunarscape();
			}

			if (!_loadTableGroupQuery.IsEmpty) {
				_loadTableGroupQuery.ForEach<LunarscapeLoadTableGroupRequest>(
					request => {
						int id = request.id;
						string guid = request.guid.ToString();
						_ = Task.Run(() => LoadGroup(id, guid));
					}
				);
				ECSUtilityInternal.DestroyEntityInQuery(_loadTableGroupQuery);
			}

			if (!_unloadTableGroupQuery.IsEmpty) {
				_unloadTableGroupQuery.ForEach<LunarscapeUnloadTableGroupRequest>(
					request => {
						string guid = request.guid.ToString();
						UnloadGroup(guid);
					}
				);
				ECSUtilityInternal.DestroyEntityInQuery(_unloadTableGroupQuery);
			}
		}

		private void EnterLunarscape() {
			// Entry Pointの抽出
			LunarscapeEntrySetting[] entries = new LunarscapeEntrySetting[0];
			_enterQuery.ForEach(
				(index, entity) => {
					if (index == 0) {
						DynamicBuffer<LunarscapeEntrySetting> entryPointBuffer = ECSUtilityInternal.EntityManager.GetBuffer<LunarscapeEntrySetting>(entity);
						entries = new LunarscapeEntrySetting[entryPointBuffer.Length];
						for (int i = 0; i < entries.Length; ++i) {
							entries[i] = entryPointBuffer[i];
						}
					}
				}
			);

			// リクエストの削除
			ECSUtilityInternal.DestroyEntityInQuery(_enterQuery);

			// 開始処理
			_ = Task.Run(async () => {
				await EnterLunarscapeInternal(entries);
			});
		}

		private async Task EnterLunarscapeInternal(LunarscapeEntrySetting[] entries) {
			_entrySettings = entries;

			using (new Process("Setup Manager")) {
				await LunarscapeLoadTableManager.Init();

				AsyncUtilityInternal.Send(() => {
					FieldManager.CreateInstance();
				});
				
				List<Task> parallelFieldTasks = new List<Task> {
					// フィールドシングルトンの作成
					FieldManager.CreateSettingsSingleton(),

					// フィールドヘッダの作成
					FieldManager.CreateHeaderEntities()
				};

				await Task.WhenAll(parallelFieldTasks);

				AsyncUtilityInternal.Send(() => {
					AvatarResourceManager.Init();
				});
				await AvatarResourceManager.LoadTables();
			}

			using (new Process("Load Global")) {
				await LoadGroup(-1, LunarscapeLoadTable.GLOBAL_GUID);
			}

			// システムインスタンスの作成
			AsyncUtilityInternal.Post(() => {
				ECSUtilityInternal.ExecuteCommandBufferTemp(commandBuffer => {
					Entity entity = commandBuffer.CreateEntity();
					commandBuffer.AddComponent(entity, new LunarscapeInstance{});
					commandBuffer.AddComponent(entity, new LocalToWorld { Value = float4x4.identity, });
					commandBuffer.AddComponent(entity, LocalTransform.FromPosition(float3.zero));
					commandBuffer.AddComponent(entity, new LunarscapeParentingRequest{});
					commandBuffer.AddComponent(entity, new Parent{});
					commandBuffer.SetDebugName(entity, "Lunarscape Instance");
				});
			});
		}

		private async Task LoadGroup(int id, string guid) {
			using (new Process($"Load Table@id={id}({guid})")) {
				await LunarscapeLoadTableManager.Load(guid, async group => {
					await LoadGroupAvatars(group);
				 });

				AsyncUtilityInternal.Post(() => {
					ECSUtilityInternal.ExecuteCommandBufferTemp( commandBuffer => {
						_tableLoadingQuery.ForEach<LunarscapeTableLoadingFlag>((e, flag) => {
							if (flag.id == id) {
								commandBuffer.DestroyEntity(e);
							}
						});
					});
				});
			}
		}

		private async Task LoadGroupAvatars(LunarscapeLoadTable.LoadGroup group) {
			AsyncUtilityInternal.Send(() => {
				List<int> avatarIds = new List<int>();
				foreach(LunarscapeLoadTable.AvatarSpawn avatar in group.avatarSpawns) {
					if (! avatarIds.Contains(avatar.avatarId)) {
						AvatarResourceManager.CreatePrefab(avatar.avatarId);
						avatarIds.Add(avatar.avatarId);
					}

					float3 position;
					quaternion rotation;
					if (avatar.playerIndex >= 0) {
						if (avatar.playerIndex < _entrySettings.Length) {
							LunarscapeEntrySetting settings = _entrySettings[avatar.playerIndex];
							position = settings.position;
							rotation = settings.rotation;
						} else {
							position = float3.zero;
							rotation = quaternion.identity;
							D.LogW($"Player {avatar.playerIndex.ToString("00")} not provided.");
						}
					} else {
						position = avatar.position;
						rotation = avatar.rotation;
					}

					ECSUtilityInternal.ExecuteCommandBufferTemp(commandBuffer => {
						Entity entity = commandBuffer.CreateEntity();
						commandBuffer.AddComponent(
							entity, 
							new LunarscapeAvatarSpawnRequest {
								id = avatar.avatarId,
								position = position,
								rotation = rotation,
								playerIndex = avatar.playerIndex,
							}
						);
					});
				}
				AvatarResourceManager.AddLoadPrefabTable(group.fieldGuid, avatarIds);
			});
			await Task.Yield();
		}

		private void UnloadGroup(string guid) {
			using(new Process($"Unload Table@{guid}")) {
				AvatarResourceManager.RemoveLoadPrefabTable(guid);
			}
		}
	}
}