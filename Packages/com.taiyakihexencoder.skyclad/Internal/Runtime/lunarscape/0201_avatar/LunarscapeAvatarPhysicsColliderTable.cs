using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace skyclad.lunarscape.internalProc {
	public sealed class LunarscapeAvatarPhysicsColliderTable : UnityEngine.ScriptableObject {
		public const string resourceAddress = "lunarscape/LunarscapeAvatarPhysicsColliderTable";

		public const int EMPTY_COLLIDER = -1;

		[System.Serializable]
		public struct PhysicsColliderSettings {
			public int id;
			public string name;
			public float radius;
			public float height;
		}

		[UnityEngine.SerializeField]
		private PhysicsColliderSettings[] _colliders = new PhysicsColliderSettings[0];
		public PhysicsColliderSettings[] Colliders => _colliders; 

		internal bool TryGetBlob(int id, out BlobAssetReference<Collider> blob) {
			foreach (PhysicsColliderSettings collider in _colliders) {
				if (collider.id == id) {
					blob = CapsuleCollider.Create(
						new CapsuleGeometry {
							Radius = collider.radius,
							Vertex0 = new float3(0.0f, collider.radius, 0.0f),
							Vertex1 = new float3(0.0f, collider.height - collider.radius, 0.0f),
						},
						new CollisionFilter {
							BelongsTo = LunarscapeInternalConst.SCENE_LAYER_PHYSICS_OBJECT,
							CollidesWith = LunarscapeInternalConst.SCENE_LAYER_COLLIDES_WITH_PHYSICS_OBJECT,
						}
					);
					blob.Value.SetCollisionResponse(CollisionResponsePolicy.CollideRaiseCollisionEvents);
					return true;
				}
			}
			blob = BlobAssetReference<Collider>.Null;
			return false;
		}
	}
}