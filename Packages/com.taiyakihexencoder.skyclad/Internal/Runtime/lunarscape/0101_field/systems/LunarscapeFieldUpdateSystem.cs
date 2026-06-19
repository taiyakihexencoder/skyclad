using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace skyclad.lunarscape.internalProc {
	/// <summary>
	/// LocalToWorldを参照するのでTransformSystemGroupよりも後に実行する。
	/// 読み込み状態が不正になるので、ロード中のアンロードは起きないようにする。
	/// </summary>
	[UpdateInGroup(typeof(LunarscapeSimulationAfterTransformSystemGroup))]
	public partial struct LunarscapeFieldUpdateSystem : ISystem {
		private EntityQuery _query;
		private EntityQuery _pointQuery;

		private EntityQuery _loadingQuery;
		private EntityQuery _unloadingQuery;

		void ISystem.OnCreate(ref SystemState state) {
			_query = new EntityQueryBuilder(Allocator.Temp)
				.WithAllRW<LunarscapeFieldComponent>()
				.WithAll<LinkedEntityGroup>()
				.Build(ref state);
			state.RequireForUpdate(_query);

			_pointQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<LunarscapeFieldObservePoint, LocalToWorld>()
				.Build(ref state);
			state.RequireForUpdate(_pointQuery);

			_loadingQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<LunarscapeTableLoadingFlag>()
				.Build(ref state);
		}

		void ISystem.OnUpdate(ref SystemState state) {
			if (SystemAPI.TryGetSingleton(out SingletonLunarscapeField singleton)) {
				NativeArray<LocalToWorld> points = _pointQuery.ToComponentDataArray<LocalToWorld>(Allocator.TempJob);

				EntityCommandBuffer.ParallelWriter commandBuffer = CreateCommandBuffer(ref state).AsParallelWriter();

				state.Dependency = new LoadJob {
					distanceToLoad = singleton.loadFieldDistance,
					points = points,
					commandBuffer = commandBuffer,
				}.ScheduleParallel(_query, state.Dependency);
				
				if (_loadingQuery.IsEmpty) {
					state.Dependency = new UnloadJob {
						distanceToUnload = singleton.unloadFieldDistance,
						points = points,
						commandBuffer = commandBuffer,
					}.ScheduleParallel(_query, state.Dependency);
				}

				state.Dependency = points.Dispose(state.Dependency);

			}
		}
	
		void ISystem.OnDestroy(ref SystemState state) {
		}

		private readonly EntityCommandBuffer CreateCommandBuffer(ref SystemState state) {
			return SystemAPI
				.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
				.CreateCommandBuffer(state.World.Unmanaged);
		}

		partial struct LoadJob : IJobEntity {
			[ReadOnly] public float distanceToLoad;
			[ReadOnly] public NativeArray<LocalToWorld> points;
			public EntityCommandBuffer.ParallelWriter commandBuffer;

			void Execute([EntityIndexInQuery] int sortKey, in Entity entity, RefRW<LunarscapeFieldComponent> field, ref DynamicBuffer<LinkedEntityGroup> children) {
				if (!field.ValueRO.active) {
					foreach (LocalToWorld point in points) {
						float3 pos = point.Position;
						if (field.ValueRO.boundsMin.x - distanceToLoad <= pos.x &&
							field.ValueRO.boundsMin.y - distanceToLoad <= pos.y &&
							field.ValueRO.boundsMin.z - distanceToLoad <= pos.z &&
							pos.x <= field.ValueRO.boundsMin.x + distanceToLoad &&
							pos.y <= field.ValueRO.boundsMin.y + distanceToLoad &&
							pos.z <= field.ValueRO.boundsMin.z + distanceToLoad
						) {
							field.ValueRW.active = true;

							// Mesh Entityを作成
							// ロードを含むのでLunarscapeFieldBehaviourで生成
							Entity requestEntity = commandBuffer.CreateEntity(sortKey);
							commandBuffer.AddComponent(
								sortKey, 
								requestEntity, 
								new LunarscapeFieldCreateRequest {
									entity = entity,
									id = field.ValueRO.id,
								}
							);

							// ロード中フラグ
							Entity waitEntity = commandBuffer.CreateEntity(sortKey);
							commandBuffer.AddComponent(
								sortKey,
								waitEntity,
								new LunarscapeTableLoadingFlag { id = field.ValueRO.id, }
							);
						}
					}
				}
			}
		}

		partial struct UnloadJob : IJobEntity {
			[ReadOnly] public float distanceToUnload;
			[ReadOnly] public NativeArray<LocalToWorld> points;
			public EntityCommandBuffer.ParallelWriter commandBuffer;

			void Execute([EntityIndexInQuery] int sortKey, in Entity entity, RefRW<LunarscapeFieldComponent> field, ref DynamicBuffer<LinkedEntityGroup> children) {
				if (field.ValueRO.active) {
					foreach (LocalToWorld point in points) {
					float3 pos = point.Position;
						if (pos.x < field.ValueRO.boundsMin.x - distanceToUnload ||
							pos.y < field.ValueRO.boundsMin.y - distanceToUnload ||
							pos.z < field.ValueRO.boundsMin.z - distanceToUnload ||
							field.ValueRO.boundsMin.x + distanceToUnload < pos.x ||
							field.ValueRO.boundsMin.y + distanceToUnload < pos.y ||
							field.ValueRO.boundsMin.z + distanceToUnload < pos.z
						) {
							field.ValueRW.active = false;

							// Mesh Entityを削除
							foreach(LinkedEntityGroup child in children) {
								commandBuffer.DestroyEntity(sortKey, child.Value);
							}
						}
					}
				}
			}
		}

	}
}
