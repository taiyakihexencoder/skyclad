using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace skyclad.character {
	[UpdateInGroup(typeof(SkycladCharacterSystemGroup))]
	public partial struct CharacterSpawnerSystem : ISystem {
 		private EntityQuery query;
		private EntityQuery requestQuery;

		void ISystem.OnCreate(ref SystemState state) {
			query = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<SpawnerComponent>()
				.Build(ref state);
			state.RequireForUpdate(query);

			requestQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<RequestSpawnComponent, CharacterSpawnParameterElement>()
				.Build(ref state);
			state.RequireForUpdate(requestQuery);
		}

		void ISystem.OnUpdate(ref SystemState state) {
			EntityCommandBuffer.ParallelWriter commandBuffer = CreateCommandBuffer(ref state).AsParallelWriter();
			NativeArray<SpawnerComponent> spawners = query.ToComponentDataArray<SpawnerComponent>(Allocator.TempJob);

			state.Dependency = new SpawnJob {
				spawners = spawners,
				commandBuffer = commandBuffer,
			}.ScheduleParallel(requestQuery, state.Dependency);
			state.Dependency = spawners.Dispose(state.Dependency);
		}

		private readonly EntityCommandBuffer CreateCommandBuffer(ref SystemState state) {
			return SystemAPI
				.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
				.CreateCommandBuffer(state.World.Unmanaged);
		}

		partial struct SpawnJob : IJobEntity {
			[ReadOnly] public NativeArray<SpawnerComponent> spawners;
			public EntityCommandBuffer.ParallelWriter commandBuffer;

			void Execute(
				in Entity entity, 
				[EntityIndexInQuery] int sortKey, 
				RefRO<RequestSpawnComponent> request,
				RefRO<CharacterSpawnParameterElement> spawnParameter
			) {
				foreach(SpawnerComponent spawner in spawners) {
					if (spawner.spawnId == request.ValueRO.spawnId) {
						Entity instance = commandBuffer.Instantiate(sortKey, spawner.prefab);
						commandBuffer.SetComponent(
							sortKey, 
							instance,
							LocalTransform.FromPositionRotationScale(
								request.ValueRO.position,
								request.ValueRO.rotation,
								request.ValueRO.scale
							)
						);
						commandBuffer.SetComponent(
							sortKey,
							instance,
							new LocalToWorld{ 
								Value = float4x4.TRS(
									request.ValueRO.position, 
									request.ValueRO.rotation, 
									new float3(request.ValueRO.scale, request.ValueRO.scale, request.ValueRO.scale)
								)
							}
						);
						commandBuffer.SetComponent(
							sortKey,
							instance,
							spawnParameter.ValueRO
						);

						/// Parent.Value = Entity.NullにするとLocalToWorldが更新されなくなるため、
						/// 削除しておく必要がある。
						commandBuffer.RemoveComponent<Parent>(
							sortKey,
							instance
						);
						break;
					}
				}
				commandBuffer.DestroyEntity(sortKey, entity);
			}
		}
	}
}