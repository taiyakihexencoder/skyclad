using Unity.Collections;
using Unity.Entities;

namespace skyclad.lunarscape.internalProc {
	[UpdateInGroup(typeof(LunarscapeSimulationFieldSystemGroup))]
	public partial struct LunarscapeFieldUpdateSystem : ISystem {
		private EntityQuery _query;
		private EntityQuery _pointQuery;

		void ISystem.OnCreate(ref SystemState state) {
			_query = new EntityQueryBuilder(Allocator.Temp)
				.WithAllRW<LunarscapeFieldComponent>()
				.WithAll<LinkedEntityGroup>()
				.Build(ref state);
			state.RequireForUpdate(_query);

			_pointQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<LunarscapeFieldObservePoint>()
				.Build(ref state);
			state.RequireForUpdate(_pointQuery);
		}

		void ISystem.OnUpdate(ref SystemState state) {
			if (SystemAPI.TryGetSingleton(out SingletonLunarscapeField singleton)) {
				NativeArray<LunarscapeFieldObservePoint> points = _pointQuery.ToComponentDataArray<LunarscapeFieldObservePoint>(Allocator.TempJob);

				state.Dependency = new UpdateJob {
					distanceToLoad = singleton.loadFieldDistance,
					distanceToUnload = singleton.unloadFieldDistance,
					points = points,
					commandBuffer = CreateCommandBuffer(ref state).AsParallelWriter(),
				}.ScheduleParallel(_query, state.Dependency);

				state.Dependency = points.Dispose(state.Dependency);

			}
		}
	
		void ISystem.OnDestroy(ref SystemState state) {
		}

		partial struct UpdateJob : IJobEntity {
			[ReadOnly] public float distanceToLoad;
			[ReadOnly] public float distanceToUnload;
			[ReadOnly] public NativeArray<LunarscapeFieldObservePoint> points;
			public EntityCommandBuffer.ParallelWriter commandBuffer;

			void Execute([EntityIndexInQuery] int sortKey, in Entity entity, RefRW<LunarscapeFieldComponent> field, ref DynamicBuffer<LinkedEntityGroup> children) {
				float distanceToLoadSq = distanceToLoad * distanceToLoad;
				foreach (LunarscapeFieldObservePoint point in points) {
					if (field.ValueRO.active) {
						if (point.position.x < field.ValueRO.boundsMin.x - distanceToUnload ||
							point.position.y < field.ValueRO.boundsMin.y - distanceToUnload ||
							point.position.z < field.ValueRO.boundsMin.z - distanceToUnload ||
							field.ValueRO.boundsMin.x + distanceToUnload < point.position.x ||
							field.ValueRO.boundsMin.y + distanceToUnload < point.position.y ||
							field.ValueRO.boundsMin.z + distanceToUnload < point.position.z
						) {
							field.ValueRW.active = false;

							// Mesh Entityを削除
							foreach(LinkedEntityGroup child in children) {
								commandBuffer.DestroyEntity(sortKey, child.Value);
							}
						}
					} else {
						if (field.ValueRO.boundsMin.x - distanceToLoad <= point.position.x &&
							field.ValueRO.boundsMin.y - distanceToLoad <= point.position.y &&
							field.ValueRO.boundsMin.z - distanceToLoad <= point.position.z &&
							point.position.x <= field.ValueRO.boundsMin.x + distanceToLoad &&
							point.position.y <= field.ValueRO.boundsMin.y + distanceToLoad &&
							point.position.z <= field.ValueRO.boundsMin.z + distanceToLoad
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
						}
					}

					if (!field.ValueRO.active &&
						field.ValueRO.boundsMin.x - distanceToLoad <= point.position.x &&
						field.ValueRO.boundsMin.y - distanceToLoad <= point.position.y &&
						field.ValueRO.boundsMin.z - distanceToLoad <= point.position.z &&
						point.position.x <= field.ValueRO.boundsMin.x + distanceToLoad &&
						point.position.y <= field.ValueRO.boundsMin.y + distanceToLoad &&
						point.position.z <= field.ValueRO.boundsMin.z + distanceToLoad
					) {
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
					} else if (
						point.position.x < field.ValueRO.boundsMin.x - distanceToUnload ||
						point.position.y < field.ValueRO.boundsMin.y - distanceToUnload ||
						point.position.z < field.ValueRO.boundsMin.z - distanceToUnload ||
						field.ValueRO.boundsMin.x + distanceToUnload < point.position.x ||
						field.ValueRO.boundsMin.y + distanceToUnload < point.position.y ||
						field.ValueRO.boundsMin.z + distanceToUnload < point.position.z
					) {
						// Mesh Entityを削除
						foreach(LinkedEntityGroup child in children) {
							commandBuffer.DestroyEntity(sortKey, child.Value);
						}
					}
				}
			}
		}

		private readonly EntityCommandBuffer CreateCommandBuffer(ref SystemState state) {
			return SystemAPI
				.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
				.CreateCommandBuffer(state.World.Unmanaged);
		}
	}
}
