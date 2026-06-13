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
		private EntityArchetype _playerArchetype;

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
			_playerArchetype = ECSUtilityInternal.EntityManager.CreateArchetype(
				ComponentType.ReadOnly<LunarscapeFieldObservePoint>(),
				ComponentType.ReadOnly<LunarscapeParentingRequest>(),
				ComponentType.ReadWrite<LocalTransform>(),
				ComponentType.ReadWrite<LocalToWorld>(),
				ComponentType.ReadWrite<Parent>()
			);
		}

		private void Update() {
			if (!_enterQuery.IsEmpty) { 
				EnterLunarscape();
			}
		}

		private void EnterLunarscape() {
			NativeArray<Entity> entities = _enterQuery.ToEntityArray(Allocator.Temp);

			// Entry Pointの抽出
			DynamicBuffer<LunarscapeEntrySetting> entryPointBuffer = ECSUtilityInternal.EntityManager.GetBuffer<LunarscapeEntrySetting>(entities[0]);
			LunarscapeEntrySetting[] entries = new LunarscapeEntrySetting[entryPointBuffer.Length];
			for(int i = 0; i < entries.Length; ++i) {
				entries[i] = entryPointBuffer[i];
			}

			entities.Dispose();

			// リクエストの削除
			ECSUtilityInternal.ExecuteCommandBufferTemp(commandBuffer => {
				commandBuffer.DestroyEntity(_enterQuery, EntityQueryCaptureMode.AtPlayback);
			});

			// 開始処理
			_ = Task.Run(async () => {
				await EnterLunarscapeInternal(entries);
			});
		}

		private async Task EnterLunarscapeInternal(LunarscapeEntrySetting[] entries) {
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

			// エントリーの作成
			Task[] parallelEntryTasks = new Task[entries.Length];
			for(int i = 0; i < entries.Length; ++i) {
				parallelEntryTasks[i] = CreatePlayerEntity(entries[i]);
			}
			await Task.WhenAll(parallelEntryTasks);

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

		private async Task CreatePlayerEntity(LunarscapeEntrySetting entry) {
			AsyncUtilityInternal.Post(() => {
				ECSUtilityInternal.ExecuteCommandBufferTemp(commandBuffer => {
					Entity entity = commandBuffer.CreateEntity(_playerArchetype);
					commandBuffer.SetComponent(entity, LocalTransform.FromPositionRotation(entry.position, entry.rotation));
					commandBuffer.SetComponent(entity, new LocalToWorld{ Value = float4x4.TRS(entry.position, entry.rotation, new float3(1f,1f,1f))});
					commandBuffer.SetDebugName(entity, entry.entryName);
				});
			});
			await Task.Yield();
		}
	}
}