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

		internal static async Task CreateHeaderEntities() {
			await _loader.CreateHeaderEntities();
		}

		internal static async Task<FieldMeshAssetLoader.FieldRes> RequestLoad(int id) {
			return await _loader.RequestLoadField(id);
		}

		internal static void CreateFieldMeshEntities(Entity rootEntity, int id, FieldMeshAssetLoader.FieldRes fieldRes) {
			_loader.CreateMeshEntities(rootEntity, id, fieldRes);
		}

		internal static void UnloadAll() {
			_loader.UnloadAll();
		}
	}
}