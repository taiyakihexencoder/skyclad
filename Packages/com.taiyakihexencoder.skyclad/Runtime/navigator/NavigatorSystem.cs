using Unity.Collections;
using Unity.Entities;

namespace skyclad {
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
				ComponentType.ReadWrite<NavigatorBackstack>(),
				ComponentType.ReadWrite<NavigatorTopChanged>()
			);
			entityManager.SetComponentEnabled<NavigatorTopChanged>(backstackEntity, false);
		}

		void ISystem.OnUpdate(ref SystemState state) {
			bool navigatorTopChanged = false;
			if (!pushQuery.IsEmpty) {
				NativeArray<NavigatorPush> pushes = pushQuery.ToComponentDataArray<NavigatorPush>(Allocator.Temp);
				DynamicBuffer<NavigatorBackstack> backstack = SystemAPI.GetBuffer<NavigatorBackstack>(backstackEntity);

				foreach(NavigatorPush push in pushes) {
					if (!push.popupTo.IsEmpty) {
						// popup
						for (int i = backstack.Length-1; i >= 0; i--) {
							if (backstack[i].path == push.popupTo) {
								for (int j = backstack.Length-1; j > i; --j) {
									backstack.RemoveAt(j);
								}

								if (push.inclusive) {
									backstack.RemoveAt(i);
								}
								break;
							}
						}
					}

					backstack.Add(
						new NavigatorBackstack{
							path = push.path,
						}
					);
					navigatorTopChanged = true;
				}
				pushes.Dispose();
				EntityCommandBuffer commandBuffer = CreateCommandBuffer(ref state);
				commandBuffer.DestroyEntity(pushQuery, EntityQueryCaptureMode.AtPlayback);
			}

			if (!popQuery.IsEmpty) {
				NativeArray<NavigatorPop> pops = popQuery.ToComponentDataArray<NavigatorPop>(Allocator.Temp);
				DynamicBuffer<NavigatorBackstack> backstack = SystemAPI.GetBuffer<NavigatorBackstack>(backstackEntity);

				foreach(NavigatorPop pop in pops) {
					if (backstack.Length > 0) {
						backstack.RemoveAt(backstack.Length-1);
					}
				}
				navigatorTopChanged = true;
				pops.Dispose();
				EntityCommandBuffer commandBuffer = CreateCommandBuffer(ref state);
				commandBuffer.DestroyEntity(pushQuery, EntityQueryCaptureMode.AtPlayback);
			}

			// 変更の検知
			EntityManager entityManager = state.EntityManager;
			entityManager.SetComponentEnabled<NavigatorTopChanged>(backstackEntity, navigatorTopChanged);
			if (navigatorTopChanged) {
				DynamicBuffer<NavigatorBackstack> backstack = SystemAPI.GetBuffer<NavigatorBackstack>(backstackEntity);
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
