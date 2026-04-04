using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;

namespace skyclad.character {
	public partial struct DigestHitSystem : ISystem {
		private EntityQuery query;

		void ISystem.OnCreate(ref SystemState state) {
			query = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<Parent, HitQueue>()
				.WithAllRW<CharacterHitBox>()
				.Build(ref state);
			state.RequireForUpdate(query);
		}

		void ISystem.OnUpdate(ref SystemState state) {
			state.Dependency = new DigestHitJob {
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

		partial struct DigestHitJob : IJobEntity {
			[ReadOnly] public float dt;
			public EntityCommandBuffer.ParallelWriter commandBuffer;

			void Execute(
				in Entity entity,
				[EntityIndexInQuery] int sortKey,
				RefRO<Parent> parent,
				RefRW<CharacterHitBox> hitBox,
				ref DynamicBuffer<HitQueue> queue
			) {
				// 無効になったHitBoxの状態を更新
				// 一定時間が経過したら戻す
				if (! hitBox.ValueRO.valid) {
					hitBox.ValueRW.invincibleSeconds -= dt;
					if (hitBox.ValueRO.invincibleSeconds < 0.0f) {
						hitBox.ValueRW.invincibleSeconds = 0.0f;
						hitBox.ValueRW.valid = true;
						commandBuffer.SetSharedComponent(
							sortKey,
							entity,
							new PhysicsWorldIndex { Value = SkycladUtility.ECS.ENABLED_PHYSICS_INDEX, }
						);
					}
				}

				if (queue.Length > 0) {
					if (hitBox.ValueRO.valid) {
						// HitLogに追加
						commandBuffer.AppendToBuffer(
							sortKey,
							parent.ValueRO.Value,
							new HitLog {
								hitBox = entity,
								parameter = queue[0].parameter,
							}
						);

						// HitBoxを一定時間無効化
						hitBox.ValueRW.valid = false;
						hitBox.ValueRW.invincibleSeconds = SkycladUtility.ECS.DAMAGED_INVINCIBLE_SECONDS;
						commandBuffer.SetSharedComponent(
							sortKey,
							entity,
							new PhysicsWorldIndex { Value = SkycladUtility.ECS.DISABLED_PHYSICS_INDEX, }
						);
					}
					queue.Clear();
				}
			}
		}
	}

}
