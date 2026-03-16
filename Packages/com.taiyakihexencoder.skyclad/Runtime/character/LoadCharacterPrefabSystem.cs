using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace skyclad.character {
	/// <summary>
	/// 個別のキャラクターPrefabのロード
	/// </summary>
	[StructLayout(LayoutKind.Auto)]
	[UpdateInGroup(typeof(SkycladCharacterSystemGroup))]
	public partial struct LoadCharacterPrefabSystem : ISystem {
		private Entity prefabsParentEntity;
		private Entity spawnersParentEntity;

		private EntityQuery query;
		private EntityQuery requestQuery;

		private EntityArchetype prefabArchetype;
		private EntityArchetype spawnerArchetype;

		void ISystem.OnCreate(ref SystemState state) {
			prefabArchetype = state.EntityManager.CreateArchetype(
				new ComponentType[] {
					ComponentType.ReadOnly<Prefab>(),
					ComponentType.ReadWrite<CharacterPrefabLoadCounterComponent>(),
					ComponentType.ReadWrite<LocalTransform>(),
					ComponentType.ReadWrite<LocalToWorld>(),
					ComponentType.ReadWrite<CharacterSpawnParameterElement>(),
					ComponentType.ReadWrite<CharacterStatusMasterReference>(),
					ComponentType.ReadWrite<PhysicsCollider>(),
					ComponentType.ReadWrite<PhysicsMass>(),
					ComponentType.ReadWrite<PhysicsVelocity>(),
					ComponentType.ReadWrite<PhysicsGravityFactor>(),
					ComponentType.ReadWrite<Parent>(),
					ComponentType.ReadWrite<collider.ColliderCollisionEvent>(),
					ComponentType.ReadWrite<collider.ColliderCollisionStayEvent>()
				}
			);

			spawnerArchetype = state.EntityManager.CreateArchetype(
				new ComponentType[] {
					ComponentType.ReadWrite<SpawnerComponent>(),
					ComponentType.ReadWrite<LocalTransform>(),
					ComponentType.ReadWrite<LocalToWorld>(),
					ComponentType.ReadWrite<Parent>(),
				}
			);

			// parent prefab
			prefabsParentEntity = state.EntityManager.CreateEntity(
				state.EntityManager.CreateArchetype(
					ComponentType.ReadWrite<LocalTransform>(),
					ComponentType.ReadWrite<LocalToWorld>(),
					ComponentType.ReadWrite<Parent>()
				)
			);

			state.EntityManager.SetComponentData(prefabsParentEntity, new Parent { Value = SkycladWorld.RootEntity, });
			#if UNITY_EDITOR
			state.EntityManager.SetName(prefabsParentEntity, "Prefabs");
			#endif

			// parent spawner
			spawnersParentEntity = state.EntityManager.CreateEntity(
				state.EntityManager.CreateArchetype(
					ComponentType.ReadWrite<LocalTransform>(),
					ComponentType.ReadWrite<LocalToWorld>(),
					ComponentType.ReadWrite<Parent>()
				)
			);
			state.EntityManager.SetComponentData(spawnersParentEntity, new Parent { Value = SkycladWorld.RootEntity, });
			#if UNITY_EDITOR
			state.EntityManager.SetName(spawnersParentEntity, "Spawners");
			#endif

			query = new EntityQueryBuilder(Allocator.Temp)
				.WithOptions(EntityQueryOptions.IncludePrefab)
				.WithAllRW<CharacterPrefabLoadCounterComponent>()
				.WithAll<Prefab>()
				.Build(ref state);

			requestQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<
					RequestLoadCharacterPrefabComponent,
					SpawnAfterCreatePrefabBufferElement
				>()
				.Build(ref state);
			state.RequireForUpdate(requestQuery);

			state.RequireForUpdate<CharacterStatusTableExists>();
		}

		void ISystem.OnUpdate(ref SystemState state) {
			EntityCommandBuffer commandBuffer = CreateCommandBuffer(ref state);

			// 存在チェック、なければ生成する
			foreach ((RefRO<RequestLoadCharacterPrefabComponent> request, DynamicBuffer<SpawnAfterCreatePrefabBufferElement> spawnAfterCreate) 
				in SystemAPI.Query<RefRO<RequestLoadCharacterPrefabComponent>, DynamicBuffer<SpawnAfterCreatePrefabBufferElement>>()) {

				Entity prefabEntity = Entity.Null;
				foreach (RefRO<CharacterPrefabLoadCounterComponent> prefab
					in SystemAPI.Query<RefRO<CharacterPrefabLoadCounterComponent>>()) {
					if (request.ValueRO.characterId == prefab.ValueRO.characterId) {
						break;
					}
				}

				if (prefabEntity == Entity.Null) {
					// インスタンスを生成
					prefabEntity = CreatePrefab(commandBuffer, request.ValueRO);

					// Spawnerを生成
					CreateSpawner(commandBuffer, request.ValueRO.characterId, request.ValueRO.name, prefabEntity);
				}

				foreach(SpawnAfterCreatePrefabBufferElement spawn in spawnAfterCreate) {
					Entity createRequestEntity = commandBuffer.CreateEntity();
					#if UNITY_EDITOR
					commandBuffer.SetName(createRequestEntity, $"SpawnRequest({request.ValueRO.name})");
					#endif
					commandBuffer.AddComponent(
						createRequestEntity, 
						new RequestSpawnComponent{
							spawnId = request.ValueRO.characterId,
							position = spawn.position,
							rotation = spawn.rotation,
							scale = 1,
						}
					);
					commandBuffer.AddComponent(
						createRequestEntity,
						new CharacterSpawnParameterElement {
							dioramaId = request.ValueRO.dioramaId,
						}
					);
				}
			}

			NativeArray<RequestLoadCharacterPrefabComponent> requests 
				= requestQuery.ToComponentDataArray<RequestLoadCharacterPrefabComponent>(Allocator.TempJob);

			// カウント
			state.Dependency = new LoadJob {
				requests = requests,
				commandBuffer = commandBuffer,
			}.Schedule(query, state.Dependency);
			state.Dependency = requests.Dispose(state.Dependency);

			state.Dependency = new SkycladECSUtility.DestroyJob {
				commandBuffer = commandBuffer,
			}.Schedule(requestQuery, state.Dependency);
		}

		private readonly EntityCommandBuffer CreateCommandBuffer(ref SystemState state) {
			return SystemAPI
				.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
				.CreateCommandBuffer(state.World.Unmanaged);
		}

		private Entity CreatePrefab(
			EntityCommandBuffer commandBuffer, 
			in RequestLoadCharacterPrefabComponent request
		) {
			Entity prefabEntity = commandBuffer.CreateEntity(prefabArchetype);
			commandBuffer.SetComponent(
				prefabEntity, 
				new Parent{ Value = prefabsParentEntity, }
			);
			commandBuffer.SetComponent(
				prefabEntity, 
				new CharacterPrefabLoadCounterComponent {
					characterId = request.characterId,
					loadCounter = 1,
				}
			);

			if (request.statusIndex >= 0) {
				commandBuffer.SetComponent(
					prefabEntity,
					new CharacterStatusMasterReference {
						blob = SkycladDataTables.characterStatus,
						index = request.characterId,
					}
				);
			} else {
				commandBuffer.RemoveComponent<CharacterStatusMasterReference>(prefabEntity);
			}

			if (request.collider != BlobAssetReference<Collider>.Null) {
				commandBuffer.SetComponent(
					prefabEntity,
					new PhysicsCollider { Value = request.collider, }
				);
				PhysicsMass mass = PhysicsMass.CreateDynamic(request.collider.Value.MassProperties, 50.0f);
				// 物理による回転は無効化する
				mass.InverseInertia = new float3(0.0f, 0.0f, 0.0f);
				commandBuffer.SetComponent(prefabEntity, mass);
				commandBuffer.SetComponent(
					prefabEntity,
					new PhysicsGravityFactor { Value = 2.5f, }
				);
				commandBuffer.SetComponent(
					prefabEntity,
					PhysicsVelocity.Zero
				);
				commandBuffer.AddSharedComponent(
					prefabEntity,
					new PhysicsWorldIndex{ Value = SkycladUtility.ECS.DISABLED_PHYSICS_INDEX, }
				);
			} else {
				commandBuffer.RemoveComponent<PhysicsCollider>(prefabEntity);
				commandBuffer.RemoveComponent<PhysicsMass>(prefabEntity);
				commandBuffer.RemoveComponent<PhysicsGravityFactor>(prefabEntity);
				commandBuffer.RemoveComponent<PhysicsVelocity>(prefabEntity);
				commandBuffer.RemoveComponent<collider.ColliderCollisionEvent>(prefabEntity);
				commandBuffer.RemoveComponent<collider.ColliderCollisionStayEvent>(prefabEntity);
			}

			#if UNITY_EDITOR
			commandBuffer.SetName(prefabEntity, request.name);
			#endif

			SetUniquePrefabParameter(commandBuffer, prefabEntity, request.characterId);

			return prefabEntity;
		}

		partial void SetUniquePrefabParameter(EntityCommandBuffer commandBuffer, Entity prefab, int characterId);

		private void CreateSpawner(EntityCommandBuffer commandBuffer, int characterId, FixedString64Bytes name, Entity prefab) {
			Entity spawnerEntity = commandBuffer.CreateEntity(spawnerArchetype);
			commandBuffer.SetComponent(
				spawnerEntity,
				new Parent{ Value = spawnersParentEntity, }
			);
			commandBuffer.SetComponent(
				spawnerEntity,
				new SpawnerComponent {
					spawnId = characterId,
					prefab = prefab,
				}
			);
			#if UNITY_EDITOR
			commandBuffer.SetName(spawnerEntity, $"Spawner ({name})");
			#endif
		}

		partial struct LoadJob : IJobEntity {
			[ReadOnly] public NativeArray<RequestLoadCharacterPrefabComponent> requests;
			public EntityCommandBuffer commandBuffer;

			void Execute(in Entity entity, RefRW<CharacterPrefabLoadCounterComponent> component) {
				foreach(RequestLoadCharacterPrefabComponent request in requests) {
					if (request.characterId == component.ValueRO.characterId) {
						component.ValueRW.loadCounter++;
						break;
					}
				}
			}
		}
	}
}