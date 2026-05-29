using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine.Assertions;
using UnityEngine.XR;

namespace skyclad.field {
	public static class FieldMeshLoader {
		/// <summary>
		/// フィールドメッシュのコンポーネントセット
		/// </summary>
		private static EntityArchetype _archetype;

		/// <summary>
		/// fieldMeshIdごとの生成済BlobAssetReferenceリスト
		/// </summary>
		private static Dictionary<int, BlobAssetReference<Collider>[]> _blobs;

		private static CollisionFilter _collisionFilter => new CollisionFilter {
			BelongsTo = Layer.Terrain,
			CollidesWith = Layer.CollidesWith.Terrain,
		};

		private static Material _material => Material.Default;

		[UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Init() {
			_archetype = SkycladUtility.ECS.EntityManager.CreateArchetype(
				new ComponentType[] {
					ComponentType.ReadWrite<PhysicsCollider>(),
					ComponentType.ReadWrite<LocalTransform>(),
					ComponentType.ReadWrite<LocalToWorld>(),
					ComponentType.ReadWrite<Parent>(),
					ComponentType.ReadWrite<FieldMeshComponent>(),
				}
			);

			_blobs = new Dictionary<int, BlobAssetReference<Collider>[]>();
		}

		/// <summary>
		/// 破棄されていないBlobを破棄する。
		/// 
		/// リソースのアンロードで解放するが念のため。
		/// </summary>
		public static void DisposeAllResource() {
			foreach(BlobAssetReference<Collider>[] blobArray in _blobs.Values) {
				foreach(BlobAssetReference<Collider> blob in blobArray) {
					if (blob.IsCreated) {
						blob.Dispose();
					}
				}
			}
			_blobs.Clear();
		}

		public static void StartLoad(Entity requestEntity, in RequestLoadFieldMeshComponent request) {
			int meshId = request.meshId;
			Task.Run(async() => await LoadInternal(meshId, requestEntity));
		}

		private static async Task LoadInternal(int fieldMeshId, Entity requestEntity) {
			string address = FieldMeshId.GetAddress(fieldMeshId);
			if (address != null) {
				IList<UnityEngine.Mesh> meshes = await SkycladUtility.Resource.Load<IList<UnityEngine.Mesh>>(address);

				SkycladUtility.Async.Post(
					() => {
						CollisionFilter filter = _collisionFilter;
						Material material = _material;

						// コライダーがあるならそのまま使う、なければ生成する
						if (_blobs.TryGetValue(fieldMeshId, out BlobAssetReference<Collider>[] colliders)) {
							Assert.AreEqual(meshes.Count, colliders.Length);
						} else {
							colliders = new BlobAssetReference<Collider>[meshes.Count];
							for(int i = 0; i < meshes.Count; ++i) {
								colliders[i] = MeshCollider.Create(meshes[i], filter, material);
								colliders[i].Value.SetCollisionResponse(CollisionResponsePolicy.Collide);
							}
							_blobs.Add(fieldMeshId, colliders);
						}

						EntityManager entityManager = SkycladUtility.ECS.EntityManager;
						IEntityBuilder builder = entityManager.CreateEntityBuilder(_archetype);
						for(int i = 0; i < meshes.Count; ++i) {
							Entity entity = builder.Build(
								meshes[i].name,
								new PhysicsCollider { Value = colliders[i], },
								LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1.0f),
								new LocalToWorld{ Value = float4x4.identity, },
								new FieldMeshComponent { meshId = fieldMeshId, }
							);
							entityManager.AddSharedComponent(
								entity,
								new PhysicsWorldIndex{ Value = SkycladUtility.ECS.ENABLED_PHYSICS_INDEX,}
							);
						}

						SkycladUtility.ECS.ExecuteCommandBufferTemp(
							(commandBuffer) => {
								commandBuffer.DestroyEntity(requestEntity);
							}
						);
					}
				);
			}
		}

		public static void UnloadMeshResources(int fieldMeshId) {
			if (_blobs.TryGetValue(fieldMeshId, out BlobAssetReference<Collider>[] blobArray)) {
				foreach(BlobAssetReference<Collider> blob in blobArray) {
					if (blob.IsCreated) {
						blob.Dispose();
					}
				}
				_blobs.Remove(fieldMeshId);
			}

			string address = FieldMeshId.GetAddress(fieldMeshId);
			if (address != null) {
				SkycladUtility.Resource.Unload(address);
			}
		}
	}
}