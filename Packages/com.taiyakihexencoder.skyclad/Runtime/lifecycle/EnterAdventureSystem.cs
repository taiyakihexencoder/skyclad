using Unity.Collections;
using Unity.Entities;

namespace skyclad.lifecycle {
	/// <summary>
	/// インゲーム開始処理まとめ
	/// </summary>
	[UpdateInGroup(typeof(SkycladLifecycleSystemGroup))]
	public partial struct EnterAdventureSystem : ISystem {
		private Entity procedureEntity;

		EntityArchetype loadDioramaArchetype;

		private EntityQuery query;
		private EntityQuery waitQuery;
		private EntityQuery systemQuery;
		private EntityQuery customQuery;

		void ISystem.OnCreate(ref SystemState state) {
			EntityManager entityManager = state.EntityManager;
			procedureEntity = CreateSingleton(ref state);

			loadDioramaArchetype = entityManager.CreateArchetype(
				new ComponentType[] {
					ComponentType.ReadOnly<EnterAdventureSystemLoadComponent>(),
					ComponentType.ReadWrite<RequestLoadDioramaComponent>(),
				}
			);

			query = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<RequestEnterAdventureComponent>()
				.WithNone<WaitTaskComponent>()
				.Build(ref state);

			waitQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<RequestEnterAdventureComponent, WaitTaskComponent>()
				.Build(ref state);

			systemQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<EnterAdventureSystemLoadComponent>()
				.Build(ref state);

			customQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<AfterWorldLoad>()
				.Build(ref state);

			state.RequireForUpdate<RequestEnterAdventureComponent>();

			// セーブデータ読み込み待ち
			state.RequireForUpdate<userData.UserDataComponent>();
		}

		void ISystem.OnUpdate(ref SystemState state) {
			RefRW<EnterAdventureProcedure> procedure = SystemAPI.GetComponentRW<EnterAdventureProcedure>(procedureEntity);

			switch (procedure.ValueRO.state) {
				case EnterAdventureState.Idle: {
					using (new Process("Enter Adventure Adventure Load")) {
						SkycladDataTables.OnStartAdventure();

						procedure.ValueRW.state = EnterAdventureState.LoadSystem;
						state.Dependency = new AddWaitTaskJob {
							commandBuffer = CreateCommandBuffer(ref state),
						}.Schedule(query, state.Dependency);
						StartLoadSystem(ref state);
					}
					break;
				}
				case EnterAdventureState.LoadSystem: {
					if (systemQuery.IsEmpty) {
						using (new Process("Enter Adventure Custom Load")) {
							procedure.ValueRW.state = EnterAdventureState.LoadCustom;
							StartLoadCustom(ref state);
						}
					}
					break;
				}
				case EnterAdventureState.LoadCustom: {
					if (customQuery.IsEmpty) {
						using (new Process("Enter Adventure Completed")) {
							procedure.ValueRW.state = EnterAdventureState.Idle;
							state.Dependency = new DestroyJob {
								commandBuffer = CreateCommandBuffer(ref state),
							}.Schedule(waitQuery, state.Dependency);
						}
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
			entityManager.AddComponent<EnterAdventureProcedure>(singleton);
			entityManager.SetComponentData(
				singleton,
				new EnterAdventureProcedure{
					state = EnterAdventureState.Idle,
				}
			);

#if UNITY_EDITOR
			entityManager.SetName(singleton, "Enter Adventure Process State");
#endif
			return singleton;
		}

		/// <summary>
		/// システムのロード
		/// </summary>
		/// <param name="state"></param>
		private void StartLoadSystem(ref SystemState state) {
			EntityManager entityManager = state.EntityManager;

			foreach(int dioramaId in DioramaId.LaunchDioramas ?? new int[0]) {
				Entity entity = entityManager.CreateEntity(loadDioramaArchetype);
				entityManager.SetComponentData(
					entity,
					new RequestLoadDioramaComponent {
						id = dioramaId,
					}
				);
#if UNITY_EDITOR
				entityManager.SetName(entity, $"Load Diorama:{dioramaId}");
#endif
			}
		}

		/// <summary>
		/// カスタムロード処理
		/// </summary>
		/// <param name="state"></param>
		private void StartLoadCustom(ref SystemState state) {
			EntityManager entityManager = state.EntityManager;
			Entity customProcessEntity = entityManager.CreateEntity();
			entityManager.AddComponent<AfterWorldLoad>(customProcessEntity);
#if UNITY_EDITOR
			entityManager.SetName(customProcessEntity, "Custom Process After World Load");
#endif
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
	public struct RequestEnterAdventureComponent : IComponentData {}

	/// <summary>
	/// 進捗管理
	/// </summary>
	public struct EnterAdventureProcedure : IComponentData { 
		public EnterAdventureState state;
	}

	/// <summary>
	/// 進捗状態
	/// </summary>
	public enum EnterAdventureState {
		LoadSystem, // システム読み込み
		LoadCustom, // カスタム実装処理
		Idle, // 動いていない
	}
}