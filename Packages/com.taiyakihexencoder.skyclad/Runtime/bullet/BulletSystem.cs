using Unity.Collections;
using Unity.Entities;

namespace skyclad.bullet {
	/// <summary>
	/// 攻撃のヒット処理
	/// </summary>
	[UpdateInGroup(typeof(BulletHitGroup))]
	public partial struct BulletSystem : ISystem {
		private EntityQuery query;

		void ISystem.OnCreate(ref SystemState state) {
			query = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<Bullet, collider.ColliderTriggerEnterEvent>()
				.Build(ref state);
			state.RequireForUpdate(query);
		}

		void ISystem.OnUpdate(ref SystemState state) {
			state.Dependency = new HitJob {
				commandBuffer = CreateCommandBuffer(ref state).AsParallelWriter(),
			}.ScheduleParallel(query, state.Dependency);
		}
	
		void ISystem.OnDestroy(ref SystemState state) {
		}

		private readonly EntityCommandBuffer CreateCommandBuffer(ref SystemState state) {
			return SystemAPI
				.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
				.CreateCommandBuffer(state.World.Unmanaged);
		}

		partial struct HitJob : IJobEntity {
			public EntityCommandBuffer.ParallelWriter commandBuffer;

			void Execute([EntityIndexInQuery] int sortKey, RefRO<Bullet> bullet, DynamicBuffer<collider.ColliderTriggerEnterEvent> evts) {
				BulletParameter parameter = bullet.ValueRO.master.Value.records[bullet.ValueRO.masterIndex];
				foreach(collider.ColliderTriggerEnterEvent evt in evts) {
					Entity hitEntity = evt.Other;
				}
			}
		}
	}
}
