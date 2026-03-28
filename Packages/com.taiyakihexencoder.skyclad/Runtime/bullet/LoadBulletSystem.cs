using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace skyclad.bullet {
	[UpdateInGroup(typeof(SkycladBulletSystemGroup))]
	public partial struct LoadBulletSystem : ISystem {
		private EntityQuery query;
		private EntityQuery bulletGroupQuery;

		private EntityArchetype bulletGroupArchetype;
		private EntityArchetype bulletArchetype;

		private Entity bulletGroupsEntity;

		void ISystem.OnCreate(ref SystemState state) {
			bulletGroupsEntity = state.EntityManager.CreateEntity(
				state.EntityManager.CreateArchetype(
					ComponentType.ReadWrite<LocalTransform>(),
					ComponentType.ReadWrite<LocalToWorld>(),
					ComponentType.ReadWrite<Parent>()
				)
			);
			SkycladWorld.AddToRoot(bulletGroupsEntity);
#if UNITY_EDITOR
			state.EntityManager.SetName(bulletGroupsEntity, "Bullet");
#endif

			bulletGroupArchetype = state.EntityManager.CreateArchetype(
				new ComponentType[] {
					ComponentType.ReadWrite<BulletGroup>(),
					ComponentType.ReadWrite<Parent>(),
					ComponentType.ReadWrite<LinkedEntityGroup>(),
					ComponentType.ReadWrite<LocalTransform>(),
					ComponentType.ReadWrite<LocalToWorld>(),
				}
			);

			bulletArchetype = state.EntityManager.CreateArchetype(
				new ComponentType[] {
					ComponentType.ReadOnly<Prefab>(),
					ComponentType.ReadWrite<Bullet>(),
					ComponentType.ReadWrite<Parent>(),
					ComponentType.ReadWrite<LocalTransform>(),
					ComponentType.ReadWrite<LocalToWorld>(),
					ComponentType.ReadWrite<DisposableGeometry>(),
					ComponentType.ReadWrite<PhysicsCollider>(),
					ComponentType.ReadWrite<collider.ColliderTriggerEvent>(),
					ComponentType.ReadWrite<collider.ColliderTriggerEnterEvent>(),
					// 動かなくてもPhysicsVelocityは必須。
					// 当たる側かTrigger側になければお互いstatic扱いとなって判定が行われず、
					// 当たる側は親をPhysicsVelocityで動かす都合つけられないため。
					ComponentType.ReadWrite<PhysicsVelocity>(),
				}
			);

			query = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<RequestLoadBulletGroupPrefabComponent, LoadBulletBufferElement>()
				.Build(ref state);
			state.RequireForUpdate(query);

			bulletGroupQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAllRW<BulletGroup>()
				.Build(ref state);
		}

		void ISystem.OnUpdate(ref SystemState state) {
			EntityCommandBuffer commandBuffer = CreateCommandBuffer(ref state);

			NativeArray<RequestLoadBulletGroupPrefabComponent> requestArray = query.ToComponentDataArray<RequestLoadBulletGroupPrefabComponent>(Allocator.TempJob);
			NativeArray<BulletGroup> bulletGroupArray = bulletGroupQuery.ToComponentDataArray<BulletGroup>(Allocator.TempJob);

			state.Dependency = new UpdateLoadCounterJob {
				commandBuffer = commandBuffer,
				requests = requestArray,
			}.Schedule(bulletGroupQuery, state.Dependency);
			state.Dependency = requestArray.Dispose(state.Dependency);

			state.Dependency = new LoadBulletGroupJob {
				bulletGroupsEntity = bulletGroupsEntity,
				commandBuffer = commandBuffer,
				bulletGroupArchetype = bulletGroupArchetype,
				bulletArchetype = bulletArchetype,
				existingGroups = bulletGroupArray,
			}.Schedule(query, state.Dependency);
			state.Dependency = bulletGroupArray.Dispose(state.Dependency);
		}
	
		void ISystem.OnDestroy(ref SystemState state) {
			if (state.EntityManager.Exists(bulletGroupsEntity)) {
				state.EntityManager.DestroyEntity(bulletGroupsEntity);
			}
		}

		private readonly EntityCommandBuffer CreateCommandBuffer(ref SystemState state) {
			return SystemAPI
				.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
				.CreateCommandBuffer(state.World.Unmanaged);
		}

		partial struct LoadBulletGroupJob : IJobEntity {
			[ReadOnly] public Entity bulletGroupsEntity;
			[ReadOnly] public NativeArray<BulletGroup> existingGroups;
			[ReadOnly] public EntityArchetype bulletGroupArchetype;
			[ReadOnly] public EntityArchetype bulletArchetype;

			public EntityCommandBuffer commandBuffer;

			void Execute(
				in Entity entity,
				RefRO<RequestLoadBulletGroupPrefabComponent> request, 
				ref DynamicBuffer<LoadBulletBufferElement> bullets
			) {
				bool exists = false;
				foreach(BulletGroup group in existingGroups) {
					if (group.groupId == request.ValueRO.groupId) {
						exists = true;
						break;
					}
				}

				if (!exists) {
					Entity bulletGroupEntity = commandBuffer.CreateEntity(bulletGroupArchetype);
					commandBuffer.SetComponent(
						bulletGroupEntity,
						new BulletGroup {
							groupId = request.ValueRO.groupId,
							loadCounter = 1,
						}
					);
					commandBuffer.SetComponent(bulletGroupEntity, LocalTransform.FromPosition(float3.zero));
					commandBuffer.SetComponent(bulletGroupEntity, new LocalToWorld{ Value = float4x4.identity, });
					commandBuffer.SetComponent(bulletGroupEntity, new Parent{ Value = bulletGroupsEntity, });
					DynamicBuffer<LinkedEntityGroup> linkedEntityGroup = commandBuffer.SetBuffer<LinkedEntityGroup>(bulletGroupEntity);
					linkedEntityGroup.Add(bulletGroupEntity);
#if UNITY_EDITOR
					commandBuffer.SetName(bulletGroupEntity, request.ValueRO.name);
#endif

					CollisionFilter filter = new CollisionFilter {
						BelongsTo = request.ValueRO.hitBoxLayer,
						CollidesWith = request.ValueRO.hitLayer,
					};
					Material material = Material.Default;
					
					foreach(LoadBulletBufferElement bullet in bullets) {
						Entity bulletEntity = commandBuffer.CreateEntity(bulletArchetype);
#if UNITY_EDITOR
						commandBuffer.SetName(bulletEntity, bullet.name);
#endif
						linkedEntityGroup.Add(bulletEntity);
						commandBuffer.SetComponent(
							bulletEntity,
							new Parent { Value = bulletGroupEntity, }
						);
						commandBuffer.SetComponent(
							bulletEntity,
							new Bullet {
								bulletId = bullet.bulletId,
								master = SkycladDataTables.bullet,
								masterIndex = bullet.dataIndex,
								elapsedSeconds = 0.0f,
								style = bullet.style,
							}
						);
						BlobAssetReference<Collider> collider = BlobAssetReference<Collider>.Null;
					
						switch (bullet.hitBoxType) {
							case BulletHitBoxType.Box: {
								collider = BoxCollider.Create(
									geometry: new BoxGeometry {
										BevelRadius = 0.0f,
										Center = float3.zero,
										Orientation = quaternion.identity,
										Size = bullet.extent,
									},
									filter: filter,
									material: material
								);
								break;
							}
							case BulletHitBoxType.Sphere: {
								collider = SphereCollider.Create(
									geometry: new SphereGeometry {
										Center = float3.zero,
										Radius = bullet.extent.x,
									},
									filter: filter,
									material: material
								);
								break;
							}
							case BulletHitBoxType.CylinderV: {
								collider = CylinderCollider.Create(
									geometry: new CylinderGeometry {
										BevelRadius = 0.0f,
										Center = float3.zero,
										Radius = bullet.extent.x,
										Height = bullet.extent.y,
										Orientation = quaternion.identity,
									},
									filter: filter,
									material: material
								);
								break;
							}
							case BulletHitBoxType.CylinderH: {
								collider = CylinderCollider.Create(
									geometry: new CylinderGeometry {
										BevelRadius = 0.0f,
										Center = float3.zero,
										Radius = bullet.extent.x,
										Height = bullet.extent.y,
										Orientation = quaternion.AxisAngle(new float3(1.0f, 0.0f, 0.0f), math.PIHALF)
									},
									filter: filter,
									material: material
								);
								break;
							}
						}

						collider.Value.SetCollisionResponse(CollisionResponsePolicy.RaiseTriggerEvents);
						commandBuffer.SetComponent(
							bulletEntity,
							new PhysicsCollider { Value = collider, }
						);
						commandBuffer.SetComponent(
							bulletEntity,
							new DisposableGeometry { collider = collider, }
						);
						commandBuffer.SetComponent(
							bulletEntity,
							new LocalToWorld{ Value = float4x4.identity, }
						);
						commandBuffer.SetComponent(
							bulletEntity,
							LocalTransform.FromPosition(float3.zero)
						);
						commandBuffer.AddSharedComponent(
							bulletEntity,
							new PhysicsWorldIndex{ Value = SkycladUtility.ECS.ENABLED_PHYSICS_INDEX, }
						);
					}

				}
				commandBuffer.DestroyEntity(entity);
			}
		}

		partial struct UpdateLoadCounterJob : IJobEntity {
			[ReadOnly] public NativeArray<RequestLoadBulletGroupPrefabComponent> requests;

			public EntityCommandBuffer commandBuffer;

			void Execute(RefRW<BulletGroup> bulletGroup) {
				foreach(RequestLoadBulletGroupPrefabComponent request in requests) {
					if (request.groupId == bulletGroup.ValueRO.groupId) {
						bulletGroup.ValueRW.loadCounter++;
						break;
					}
				}
			}
		}
	}
}
