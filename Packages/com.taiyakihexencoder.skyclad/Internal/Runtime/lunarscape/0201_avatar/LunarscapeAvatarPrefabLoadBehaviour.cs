using skyclad.internalProc;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace skyclad.lunarscape.internalProc {
	public sealed class LunarscapeAvatarPrefabLoadBehaviour : MonoBehaviour {
		private EntityQuery _spawnQuery;

		private EntityArchetype _archetype;

		private void Awake() {
			_spawnQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<LunarscapeAvatarSpawnRequest>()
				.Build(ECSUtilityInternal.EntityManager);

			_archetype = ECSUtilityInternal.EntityManager.CreateArchetype(
				ComponentType.ReadOnly<Prefab>(),
				
				ComponentType.ReadWrite<LocalTransform>(),
				ComponentType.ReadWrite<LocalToWorld>(),

				ComponentType.ReadWrite<PhysicsCollider>(),
				ComponentType.ReadWrite<PhysicsMass>(),
				ComponentType.ReadWrite<PhysicsVelocity>(),
				ComponentType.ReadWrite<PhysicsGravityFactor>()
			);
		}

		private void Update() {
			if (!_spawnQuery.IsEmpty) {
				
			}
		}

		private void ExecuteSpawnRequest() {
			NativeArray<LunarscapeAvatarSpawnRequest> requests = _spawnQuery.ToComponentDataArray<LunarscapeAvatarSpawnRequest>(Allocator.Temp);
			foreach(LunarscapeAvatarSpawnRequest request in requests) {
			}
		}
	}
}