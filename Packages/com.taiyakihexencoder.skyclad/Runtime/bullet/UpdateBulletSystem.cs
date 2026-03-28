using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace skyclad.bullet {
	[UpdateInGroup(typeof(SkycladFixedStepSimulationSystemGroup))]
	public partial struct UpdateBulletSystem : ISystem {
		private EntityQuery query;

		void ISystem.OnCreate(ref SystemState state) {
			query = new EntityQueryBuilder(Allocator.Temp)
				.WithAllRW<Bullet, PhysicsVelocity>()
				.WithAll<LocalTransform>()
				.Build(ref state);
			state.RequireForUpdate(query);
		}

		void ISystem.OnUpdate(ref SystemState state) {
			state.Dependency = new UpdateJob {
				commandBuffer = CreateCommandBuffer(ref state).AsParallelWriter(),
				dt = state.World.Time.DeltaTime,
			}.ScheduleParallel(query, state.Dependency);
		}
	
		void ISystem.OnDestroy(ref SystemState state) {
		}

		private readonly EntityCommandBuffer CreateCommandBuffer(ref SystemState state) {
			return SystemAPI
				.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
				.CreateCommandBuffer(state.World.Unmanaged);
		}

		partial struct UpdateJob : IJobEntity {
			public EntityCommandBuffer.ParallelWriter commandBuffer;
			[ReadOnly] public float dt;

			void Execute(
				in Entity entity, 
				[EntityIndexInQuery] int sortKey,
				RefRW<Bullet> bullet, 
				RefRW<PhysicsVelocity> physicsVelocity,
				RefRO<LocalTransform> localTransform
			) {
				switch (bullet.ValueRO.style.trail) {
					case BulletTrail.None: {
						break;
					}
					case BulletTrail.Straight: {
						quaternion rotation = localTransform.ValueRO.Rotation;
						physicsVelocity.ValueRW.Linear = math.mul(rotation, new float3(0.0f, 0.0f, bullet.ValueRO.style.speed));
						break;
					}
				}

				if (bullet.ValueRO.style.lifeTime > 0f) {
					if (bullet.ValueRO.elapsedSeconds < bullet.ValueRO.style.lifeTime) {
						bullet.ValueRW.elapsedSeconds += dt;
					} else {
						commandBuffer.DestroyEntity(sortKey, entity);
					}
				}
			}
		}
	}
}
