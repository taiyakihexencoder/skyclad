using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine.Assertions;

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
								if (i != 0)
								{
									colliders[i] = MeshCollider.Create(meshes[i], filter, material);
								} else {
									colliders[i] = BoxCollider.Create(new BoxGeometry{ Center = new float3(0.0f, -5.0f, 0f), Orientation = quaternion.identity, Size = new float3(10, 1, 10)});
								}
								colliders[i].Value.SetCollisionResponse(CollisionResponsePolicy.Collide);
							}
							_blobs.Add(fieldMeshId, colliders);
						}

						for(int i = 0; i < meshes.Count; ++i) {
							SkycladUtility.ECS.CreateEntity(meshes[i].name, _archetype, (manager, e) => MakeEntity(fieldMeshId, manager, e, colliders[i]));
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

		private static void MakeEntity(int meshId, EntityManager entityManager, Entity entity, BlobAssetReference<Collider> collider) {
			entityManager.SetComponentData(entity, new PhysicsCollider { Value = collider, });

			entityManager.SetComponentData(
				entity,
				LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1.0f)
			);
			entityManager.SetComponentData(
				entity,
				new LocalToWorld{ Value = float4x4.identity, }
			);
			entityManager.SetComponentData(
				entity,
				new FieldMeshComponent { meshId = meshId, }
			);
			entityManager.AddSharedComponent(entity, new PhysicsWorldIndex{ Value = SkycladUtility.ECS.ENABLED_PHYSICS_INDEX,});
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
		}
	}
}