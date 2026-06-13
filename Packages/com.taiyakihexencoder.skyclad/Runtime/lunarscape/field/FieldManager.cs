using System.Threading.Tasks;
using Unity.Entities;
using Unity.Physics;

namespace skyclad.lunarscape {
	using internalProc;
	using skyclad.internalProc;
	using Unity.Mathematics;
	using Unity.Transforms;

	public static class FieldManager {
		private static FieldMeshAssetLoader _loader;
		private static EntityArchetype _singletonArchetype;
		private static EntityArchetype _observePointArchetype;

		internal static void CreateInstance() {
			_loader = new FieldMeshAssetLoader(
				material: Material.Default,
				collisionFilter: new CollisionFilter {
					BelongsTo = SceneLayer.Terrain,
					CollidesWith = SceneLayerCollidesWith.Terrain,
				}
			);
			
			EntityManager entityManager = ECSUtilityInternal.EntityManager;
			_singletonArchetype = entityManager.CreateArchetype(
				ComponentType.ReadWrite<SingletonLunarscapeField>(),
				ComponentType.ReadWrite<LunarscapeParentingRequest>(),
				ComponentType.ReadWrite<Parent>(),
				ComponentType.ReadWrite<LocalTransform>(),
				ComponentType.ReadWrite<LocalToWorld>()
			);

			_observePointArchetype = entityManager.CreateArchetype(
				ComponentType.ReadWrite<LunarscapeFieldObservePoint>(),
				ComponentType.ReadWrite<LunarscapeParentingRequest>(),
				ComponentType.ReadWrite<Parent>(),
				ComponentType.ReadWrite<LocalTransform>(),
				ComponentType.ReadWrite<LocalToWorld>()
			);
		}

		internal static async Task CreateSettingsSingleton() {
			AsyncUtilityInternal.Send(
				() => {
					ECSUtilityInternal.ExecuteCommandBufferTemp(commandBuffer => {
						LunarscapeSettings._Field settings = LunarscapeSettings.Get().Field;

						Entity entity = commandBuffer.CreateEntity(_singletonArchetype);
						commandBuffer.SetComponent(entity, new SingletonLunarscapeField {
							loadFieldDistance = settings.LoadFieldDistance,
							unloadFieldDistance = settings.UnloadFieldDistance,
							cacheFieldMeshSize = settings.FieldMeshCacheSize,
						});
						commandBuffer.SetComponent(entity, LocalTransform.FromPosition(float3.zero));
						commandBuffer.SetComponent(entity, new LocalToWorld{ Value = float4x4.identity});
						commandBuffer.SetDebugName(entity, "Field Singleton");
					});
				}
			);
			await Task.Yield();
		}

		internal static async Task CreateObservePoint(int index, float3 point) {
			AsyncUtilityInternal.Post(
				() => {
					ECSUtilityInternal.ExecuteCommandBufferTemp(commandBuffer => {
						Entity entity = commandBuffer.CreateEntity(_observePointArchetype);
						commandBuffer.SetComponent(entity, LocalTransform.FromPosition(float3.zero));
						commandBuffer.SetComponent(entity, new LocalToWorld{ Value = float4x4.identity, } );
						commandBuffer.SetComponent(entity, new LunarscapeFieldObservePoint{ 
							index = index,
							position = point,
						});
						commandBuffer.SetDebugName(entity, $"Field Observe Point{index.ToString("00")}");
					});
				}
			);
			await Task.Yield();
		}

		internal static async Task CreateHeaderEntities() {
			await _loader.CreateHeaderEntities();
		}

		internal static async Task CreateMeshEntities(Entity rootEntity, int id) {
			await _loader.CreateMeshEntities(rootEntity, id);
		}

		internal static void UnloadAll() {
			_loader.UnloadAll();
		}
	}
}