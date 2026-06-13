using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

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
				.WithAll<LunarscapeFieldObservePoint, LocalToWorld>()
				.Build(ref state);
			state.RequireForUpdate(_pointQuery);
		}

		void ISystem.OnUpdate(ref SystemState state) {
			if (SystemAPI.TryGetSingleton(out SingletonLunarscapeField singleton)) {
				NativeArray<LocalToWorld> points = _pointQuery.ToComponentDataArray<LocalToWorld>(Allocator.TempJob);

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
			[ReadOnly] public NativeArray<LocalToWorld> points;
			public EntityCommandBuffer.ParallelWriter commandBuffer;

			void Execute([EntityIndexInQuery] int sortKey, in Entity entity, RefRW<LunarscapeFieldComponent> field, ref DynamicBuffer<LinkedEntityGroup> children) {
				float distanceToLoadSq = distanceToLoad * distanceToLoad;
				foreach (LocalToWorld point in points) {
					float3 pos = point.Position;
					if (field.ValueRO.active) {
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
					} else {
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
