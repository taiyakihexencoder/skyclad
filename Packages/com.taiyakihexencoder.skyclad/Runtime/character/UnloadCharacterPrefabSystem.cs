using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace skyclad.character {
	using internalProc;
	/// <summary>
	/// 個別のキャラクターPrefabのアンロード
	/// </summary>
	[UpdateInGroup(typeof(SkycladCharacterSystemGroup))]
	public partial struct UnloadCharcterPrefabSystem : ISystem {
		private EntityQuery prefabQuery;
		private EntityQuery instanceQuery;
		private EntityQuery requestQuery;

		void ISystem.OnCreate(ref SystemState state) {
			prefabQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithOptions(EntityQueryOptions.IncludePrefab)
				.WithAllRW<CharacterPrefabLoadCounterComponent>()
				.WithAll<Prefab>()
				.Build(ref state);

			instanceQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<CharacterSpawnParameterElement>()
				.Build(ref state);

			requestQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<RequestUnloadCharacterPrefabComponent>()
				.Build(ref state);
			state.RequireForUpdate(requestQuery);
		}

		void ISystem.OnUpdate(ref SystemState state) {
			NativeArray<RequestUnloadCharacterPrefabComponent> requests 
				= requestQuery.ToComponentDataArray<RequestUnloadCharacterPrefabComponent>(Allocator.TempJob);

			// 処理が競合するため並列複数JobではEntityCommandBufferを分ける必要がある
			EntityCommandBuffer deleteInstanceCommandBuffer = CreateCommandBuffer(ref state);
			JobHandle deleteInstance = new DeleteInstanceJob {
				requests = requests,
				commandBuffer = deleteInstanceCommandBuffer.AsParallelWriter(),
			}.ScheduleParallel(instanceQuery, state.Dependency);

			EntityCommandBuffer unloadPrefabCommandBuffer = CreateCommandBuffer(ref state);
			JobHandle unloadPrefab = new UnloadPrefabJob {
				requests = requests,
				commandBuffer = unloadPrefabCommandBuffer,
			}.Schedule(prefabQuery, state.Dependency);

			state.Dependency = JobHandle.CombineDependencies(deleteInstance, unloadPrefab);
			state.Dependency = requests.Dispose(state.Dependency);

			EntityCommandBuffer commandBuffer = CreateCommandBuffer(ref state);

			state.Dependency = commandBuffer.Destroy()
				.Schedule(requestQuery, state.Dependency);
		}

		private readonly EntityCommandBuffer CreateCommandBuffer(ref SystemState state) {
			return SystemAPI
				.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
				.CreateCommandBuffer(state.World.Unmanaged);
		}

		/// <summary>
		/// 生成したインスタンスを破棄する
		/// </summary>
		partial struct DeleteInstanceJob : IJobEntity {
			[ReadOnly] public NativeArray<RequestUnloadCharacterPrefabComponent> requests;
			public EntityCommandBuffer.ParallelWriter commandBuffer;

			void Execute(in Entity entity, [EntityIndexInQuery] int sortKey, RefRO<CharacterSpawnParameterElement> spawnParameter) {
				foreach(RequestUnloadCharacterPrefabComponent request in requests) {
					if (spawnParameter.ValueRO.dioramaId == request.dioramaId) {
						commandBuffer.DestroyEntity(sortKey, entity);
						break;
					}
				}
			}
		}

		/// <summary>
		/// Prefabをアンロードする
		/// </summary>
		partial struct UnloadPrefabJob : IJobEntity {
			[ReadOnly] public NativeArray<RequestUnloadCharacterPrefabComponent> requests;
			public EntityCommandBuffer commandBuffer;

			void Execute(in Entity entity, RefRW<CharacterPrefabLoadCounterComponent> component) {
				foreach(RequestUnloadCharacterPrefabComponent request in requests) {
					if (request.characterId == component.ValueRO.characterId) {
						if (component.ValueRO.loadCounter > 0) {
							component.ValueRW.loadCounter--;
						} else {
							commandBuffer.RemoveComponent<CharacterPrefabLoadCounterComponent>(entity);
							commandBuffer.DestroyEntity(entity);
						}
						break;
					}
				}
			}
		}
	}
}