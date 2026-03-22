using Unity.Collections;
using Unity.Entities;

namespace skyclad.lifecycle {
	/// <summary>
	/// インゲーム終了処理まとめ
	/// </summary>
	[UpdateInGroup(typeof(SkycladLifecycleSystemGroup))]
	public partial struct ExitAdventureSystem : ISystem {
		private Entity procedureEntity;

		EntityArchetype unloadDioramaArchetype;

		private EntityQuery query;
		private EntityQuery waitQuery;
		private EntityQuery systemQuery;
		private EntityQuery customQuery;

		void ISystem.OnCreate(ref SystemState state) {
			EntityManager entityManager = state.EntityManager;
			procedureEntity = CreateSingleton(ref state);

			unloadDioramaArchetype = entityManager.CreateArchetype(
				new ComponentType[] {
					ComponentType.ReadOnly<ExitAdventureSystemUnloadComponent>(),
					ComponentType.ReadWrite<RequestUnloadDioramaComponent>(),
				}
			);
			query = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<RequestExitAdventureComponent>()
				.WithNone<WaitTaskComponent>()
				.Build(ref state);

			waitQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<RequestExitAdventureComponent, WaitTaskComponent>()
				.Build(ref state);

			systemQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<ExitAdventureSystemUnloadComponent>()
				.Build(ref state);

			customQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<BeforeWorldUnload>()
				.Build(ref state);

			state.RequireForUpdate<RequestExitAdventureComponent>();
		}

		void ISystem.OnUpdate(ref SystemState state) {
			RefRW<ExitAdventureProcedure> procedure = SystemAPI.GetComponentRW<ExitAdventureProcedure>(procedureEntity);

			switch (procedure.ValueRO.state) {
				case ExitAdventureState.Idle: {
					procedure.ValueRW.state = ExitAdventureState.UnloadCustom;
					state.Dependency = new AddWaitTaskJob {
						commandBuffer = CreateCommandBuffer(ref state),
					}.Schedule(query, state.Dependency);
					StartUnloadCustom(ref state);
					break;
				}
				case ExitAdventureState.UnloadCustom: {
					if (customQuery.IsEmpty) {
						procedure.ValueRW.state = ExitAdventureState.UnloadSystem;
						state.Dependency = new DestroyJob {
							commandBuffer = CreateCommandBuffer(ref state),
						}.Schedule(waitQuery, state.Dependency);
						StartUnloadSystem(ref state);
					}
					break;
				}
				case ExitAdventureState.UnloadSystem: {
					if (systemQuery.IsEmpty) {
						procedure.ValueRW.state = ExitAdventureState.Idle;
						state.Dependency = new DestroyJob {
							commandBuffer = CreateCommandBuffer(ref state),
						}.Schedule(waitQuery, state.Dependency);
					}
					break;
				}
			}
		}

		private readonly EntityCommandBuffer CreateCommandBuffer(ref SystemState state) {
			return SystemAPI
				.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
				.CreateCommandBuffer(state.World.Unmanaged);
		}

		private Entity CreateSingleton(ref SystemState state) {
			EntityManager entityManager = state.EntityManager;

			Entity singleton = entityManager.CreateEntity();
			entityManager.AddComponent<ExitAdventureProcedure>(singleton);
			entityManager.SetComponentData(
				singleton,
				new ExitAdventureProcedure{
					state = ExitAdventureState.Idle,
				}
			);

#if UNITY_EDITOR
			entityManager.SetName(singleton, "Enter Adventure Process State");
#endif
			return singleton;
			
		}

		/// <summary>
		/// カスタムアンロード処理
		/// </summary>
		/// <param name="state"></param>
		private void StartUnloadCustom(ref SystemState state) {
			EntityManager entityManager = state.EntityManager;
			Entity customProcessEntity = entityManager.CreateEntity();
			entityManager.AddComponent<BeforeWorldUnload>(customProcessEntity);
#if UNITY_EDITOR
			entityManager.SetName(customProcessEntity, "Custom Process Before World Unload");
#endif			
		}

		/// <summary>
		/// システムのアンロード
		/// </summary>
		/// <param name="state"></param>
		private void StartUnloadSystem(ref SystemState state) {
			EntityManager entityManager = state.EntityManager;

			foreach(int dioramaId in DioramaId.LaunchDioramas) {
				Entity entity = entityManager.CreateEntity(unloadDioramaArchetype);
				entityManager.SetComponentData(
					entity,
					new RequestUnloadDioramaComponent {
						id = dioramaId,
					}
				);
#if UNITY_EDITOR
				entityManager.SetName(entity, $"Unload Diorama:{dioramaId}");
#endif
			}
		}

		partial struct AddWaitTaskJob : IJobEntity {
			public EntityCommandBuffer commandBuffer;

			void Execute(in Entity entity) {
				commandBuffer.AddComponent<WaitTaskComponent>(entity);
			}
		}

		partial struct DestroyJob : IJobEntity {
			public EntityCommandBuffer commandBuffer;

			void Execute(in Entity entity) {
				commandBuffer.DestroyEntity(entity);
			}
		}

	}

		/// <summary>
	/// システムを実行するRequest
	/// </summary>
	public struct RequestExitAdventureComponent : IComponentData {}

	/// <summary>
	/// 進捗管理
	/// </summary>
	public struct ExitAdventureProcedure : IComponentData { 
		public ExitAdventureState state;
	}

	/// <summary>
	/// 進捗状態
	/// </summary>
	public enum ExitAdventureState {
		UnloadCustom, // カスタム実装処理
		UnloadSystem, // システム読み込み
		Idle, // 動いていない
	}

}