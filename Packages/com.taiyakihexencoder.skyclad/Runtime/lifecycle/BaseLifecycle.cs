using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;

namespace skyclad.lifecycle {
	public abstract class BaseLifecycle {
		private static BaseLifecycle _custom = null;

		public BaseLifecycle() {
			AssignLifecycle(this);
		}

		internal static void StartWorldProcess(World world) {
			Task.Run(
				async () => {
					if (_custom != null) {
						await _custom?.OnStartWorld(world);
					}

					SkycladUtility.Async.Post(
						() => {
							SkycladUtility.ECS.ExecuteCommandBufferTemp(
								(commandBuffer) => {
									EntityQueryBuilder qb = new EntityQueryBuilder(Allocator.Temp)
										.WithAll<AfterWorldLoad>();

									commandBuffer.DestroyEntity(qb.Build(world.EntityManager), EntityQueryCaptureMode.AtPlayback);
									qb.Dispose();
								}
							);
						}
					);
				}
			);
		}

		internal static void EndWorldProcess(World world) {
			Task.Run(
				async () => {
					if (_custom != null) {
						await _custom?.OnEndWorld(world);
					}

					SkycladUtility.Async.Post(
						() => {
							SkycladUtility.ECS.ExecuteCommandBufferTemp(
								(commandBuffer) => {
									EntityQueryBuilder qb = new EntityQueryBuilder(Allocator.Temp)
										.WithAll<BeforeWorldUnload>();

									commandBuffer.DestroyEntity(qb.Build(world.EntityManager), EntityQueryCaptureMode.AtPlayback);
									qb.Dispose();
								}
							);
						}
					);
				}
			);
		}

		private static void AssignLifecycle(BaseLifecycle custom) {
			_custom = custom;
		}

		protected abstract Task OnStartWorld(World world);

		protected abstract Task OnEndWorld(World world);
	}
}