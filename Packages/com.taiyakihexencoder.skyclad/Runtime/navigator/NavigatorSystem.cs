using Unity.Collections;
using Unity.Entities;

namespace skyclad {
	using internalProc;
	/// <summary>
	/// NavigatorPush, NavigatorPopを使って遷移・戻りを実現する。
	/// Backstackのトップが変更されたフレームにSingletonのNavigatorTopChangedが有効になるので、
	/// ここで遷移先を別のSystemから検知し、遷移処理を実行する。
	/// </summary>
	[UpdateInGroup(typeof(SkycladSimulationSystemGroup))]
	public partial struct NavigatorSystem : ISystem {
		private EntityQuery pushQuery;
		private EntityQuery popQuery;
		private Entity backstackEntity;

		void ISystem.OnCreate(ref SystemState state) {
			pushQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<NavigatorPush>()
				.Build(ref state);

			popQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<NavigatorPop>()
				.Build(ref state);

			EntityManager entityManager = state.EntityManager;
			backstackEntity = entityManager.CreateEntity(
				ComponentType.ReadWrite<InternalNavigatorBackstack>(),
				ComponentType.ReadWrite<NavigatorTopChanged>()
			);
			entityManager.SetComponentEnabled<NavigatorTopChanged>(backstackEntity, false);
		}

		void ISystem.OnUpdate(ref SystemState state) {
			bool navigatorTopChanged = false;
			if (!pushQuery.IsEmpty) {
				NativeArray<NavigatorPush> pushes = pushQuery.ToComponentDataArray<NavigatorPush>(Allocator.Temp);
				DynamicBuffer<InternalNavigatorBackstack> backstack = SystemAPI.GetBuffer<InternalNavigatorBackstack>(backstackEntity);

				foreach(NavigatorPush push in pushes) {
					backstack.Push(push.path, push.popupTo, push.inclusive);
					navigatorTopChanged = true;
				}
				pushes.Dispose();
				EntityCommandBuffer commandBuffer = CreateCommandBuffer(ref state);
				commandBuffer.DestroyEntity(pushQuery, EntityQueryCaptureMode.AtPlayback);
			}

			if (!popQuery.IsEmpty) {
				DynamicBuffer<InternalNavigatorBackstack> backstack = SystemAPI.GetBuffer<InternalNavigatorBackstack>(backstackEntity);
				backstack.Pop(popQuery.CalculateEntityCount());

				navigatorTopChanged = true;
				EntityCommandBuffer commandBuffer = CreateCommandBuffer(ref state);
				commandBuffer.DestroyEntity(popQuery, EntityQueryCaptureMode.AtPlayback);
			}

			// 変更の検知
			EntityManager entityManager = state.EntityManager;
			entityManager.SetComponentEnabled<NavigatorTopChanged>(backstackEntity, navigatorTopChanged);
			if (navigatorTopChanged) {
				DynamicBuffer<InternalNavigatorBackstack> backstack = SystemAPI.GetBuffer<InternalNavigatorBackstack>(backstackEntity);
				entityManager.SetComponentData(
					backstackEntity, 
					new NavigatorTopChanged {
						path = backstack.Length > 0 ? backstack[backstack.Length-1].path : ""
					}
				);
			}
		}
	
		void ISystem.OnDestroy(ref SystemState state) { }

		private readonly EntityCommandBuffer CreateCommandBuffer(ref SystemState state) {
			return SystemAPI
				.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
				.CreateCommandBuffer(state.World.Unmanaged);
		}
	}
}
