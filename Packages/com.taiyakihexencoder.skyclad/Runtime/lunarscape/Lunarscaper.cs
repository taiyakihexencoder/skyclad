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

		private EntityQuery enterQuery;
		private EntityQuery exitQuery;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void CreateInstance() {
			if (_instance == null) {
				GameObject obj = new GameObject("Lunarscaper");
				_instance = obj.AddComponent<Lunarscaper>();

				// （仮）デバッグ用の開始処理
				ECSUtilityInternal.ExecuteCommandBufferTemp(commandBuffer => {
					Enter(commandBuffer, new float3(1f,4f,1f));
				});
			}
		}

		public static void Enter(
			EntityCommandBuffer commandBuffer,
			float3 position
		) {
			Entity entity = commandBuffer.CreateEntity();
			commandBuffer.AddComponent<LunarscapeEnterRequest>(entity);
			DynamicBuffer<LunarscapeEntryPoint> entryPoint = commandBuffer.AddBuffer<LunarscapeEntryPoint>(entity);
			entryPoint.Add(new LunarscapeEntryPoint{ position = position, });
		}

		public static void Enter(
			EntityCommandBuffer commandBuffer,
			NativeArray<float3> positions
		) {
			Entity entity = commandBuffer.CreateEntity();
			commandBuffer.AddComponent<LunarscapeEnterRequest>(entity);
			DynamicBuffer<LunarscapeEntryPoint> entryPoint = commandBuffer.AddBuffer<LunarscapeEntryPoint>(entity);
			foreach (float3 position in positions) {
				entryPoint.Add(new LunarscapeEntryPoint{ position = position, });
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

			enterQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<LunarscapeEnterRequest>()
				.Build(ECSUtilityInternal.EntityManager);
			exitQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<LunarscapeExitRequest>()
				.Build(ECSUtilityInternal.EntityManager);
		}

		private void Update() {
			if (!enterQuery.IsEmpty) { 
				EnterLunarscape();
			}
		}

		private void EnterLunarscape() {
			NativeArray<Entity> entities = enterQuery.ToEntityArray(Allocator.Temp);

			// Entry Pointの抽出
			DynamicBuffer<LunarscapeEntryPoint> entryPointBuffer = ECSUtilityInternal.EntityManager.GetBuffer<LunarscapeEntryPoint>(entities[0]);
			float3[] entryPoint = new float3[entryPointBuffer.Length];
			for(int i = 0; i < entryPoint.Length; ++i) {
				entryPoint[i] = entryPointBuffer[i].position;
			}

			entities.Dispose();

			// リクエストの削除
			ECSUtilityInternal.ExecuteCommandBufferTemp(commandBuffer => {
				commandBuffer.DestroyEntity(enterQuery, EntityQueryCaptureMode.AtPlayback);
			});

			// 開始処理
			_ = Task.Run(async () => {
				await EnterLunarscapeInternal(entryPoint);
			});
		}

		private async Task EnterLunarscapeInternal(float3[] entryPoint) {
			AsyncUtilityInternal.Send(() => {
				FieldManager.CreateInstance();
			});

			List<Task> parallelFieldTasks = new List<Task> {
				// フィールドシングルトンの作成
				FieldManager.CreateSettingsSingleton(),

				// フィールドヘッダの作成
				FieldManager.CreateHeaderEntities()
			};

			for (int i = 0; i < entryPoint.Length; ++i) {
				parallelFieldTasks.Add(FieldManager.CreateObservePoint(i, entryPoint[i]));
			}
			await Task.WhenAll(parallelFieldTasks);

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
	}
}