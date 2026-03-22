using Unity.Collections;
using Unity.Entities;
using Unity.Physics;

namespace skyclad {
	/// <summary>
	/// PhysicsColliderに設定するBlobAssetReferenceを明示的に破棄する必要がある。
	/// ICleanupComponentDataを持っているエンティティは、DestroyEntityのあと、
	/// 削除フラグとICleanupComponentDataだけを残して生存し続けるので、
	/// このシステムでBlobAssetReferenceを破棄してから、
	/// ICleanupComponentDataを外すことでntityを削除させる。
	/// </summary>
	[UpdateInGroup(typeof(SkycladSimulationSystemGroup))]
	public partial struct DisposableGeometrySystem : ISystem {
		private EntityQuery query;

		void ISystem.OnCreate(ref SystemState state) {
			query = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<DisposableGeometry>()
				.WithNone<PhysicsCollider>()
				.Build(ref state);
			state.RequireForUpdate(query);
		}

		void ISystem.OnUpdate(ref SystemState state) {
			UnityEngine.Debug.Log($"DisposeCount:{query.CalculateEntityCount()}");

			state.Dependency = new DisposeJob {
				commandBuffer = CreateCommandBuffer(ref state).AsParallelWriter(),
			}.ScheduleParallel(query, state.Dependency);
		}
	
		void ISystem.OnDestroy(ref SystemState state) {
			// Unity Editorなど途中停止対応
			foreach(RefRO<DisposableGeometry> disposable in SystemAPI.Query<RefRO<DisposableGeometry>>()) {
				if (disposable.ValueRO.collider.IsCreated) {
					disposable.ValueRO.collider.Dispose();
				}
			}
		}

		private readonly EntityCommandBuffer CreateCommandBuffer(ref SystemState state) {
			return SystemAPI
				.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
				.CreateCommandBuffer(state.World.Unmanaged);
		}

		partial struct DisposeJob : IJobEntity {
			public EntityCommandBuffer.ParallelWriter commandBuffer;

			void Execute(in Entity entity, RefRO<DisposableGeometry> disposable, [EntityIndexInQuery] int sortKey) {
				if (disposable.ValueRO.collider.IsCreated) {
					disposable.ValueRO.collider.Dispose();
				}
				commandBuffer.RemoveComponent<DisposableGeometry>(sortKey, entity);
			}
		}
	}

	public struct DisposableGeometry : IComponentData, ICleanupComponentData {
		public BlobAssetReference<Collider> collider;
	}
}
