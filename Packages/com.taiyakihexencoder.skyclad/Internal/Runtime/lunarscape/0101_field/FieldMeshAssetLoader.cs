using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using skyclad.internalProc;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace skyclad.lunarscape.internalProc {
	public sealed class FieldMeshAssetLoader {
		public struct MeshInfo {
			public BlobAssetReference<Collider> collider;
			public string name;
		}

		public struct FieldRes {
			public string guid;
			public MeshInfo[] meshes;
		}

		private ConcurrentDictionary<int, string> _addressTable;
		private ConcurrentDictionary<int, FieldRes> _fields;

		private List<int> _loadCache;

		private Material _material;
		private CollisionFilter _collisionFilter;

		private EntityArchetype _archetype;
		private EntityArchetype _meshArchetype;

		private EntityQuery _singletonQuery;

		public FieldMeshAssetLoader(Material material, CollisionFilter collisionFilter) {
			_material = material;
			_collisionFilter = collisionFilter;
			_fields = new ConcurrentDictionary<int, FieldRes>();
			_addressTable = new ConcurrentDictionary<int, string>();
			_loadCache = new List<int>();

			_archetype = ECSUtilityInternal.EntityManager.CreateArchetype(
				ComponentType.ReadWrite<LocalToWorld>(),
				ComponentType.ReadWrite<LocalTransform>(),
				ComponentType.ReadWrite<LinkedEntityGroup>(),
				ComponentType.ReadWrite<LunarscapeFieldComponent>()
			);

			_meshArchetype = ECSUtilityInternal.EntityManager.CreateArchetype(
				ComponentType.ReadWrite<LunarscapeFieldMeshComponent>(),
				ComponentType.ReadWrite<PhysicsCollider>(),
				ComponentType.ReadWrite<LocalTransform>(),
				ComponentType.ReadWrite<LocalToWorld>(),
				ComponentType.ReadWrite<Parent>()
			);

			_singletonQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<SingletonLunarscapeField>()
				.Build(ECSUtilityInternal.EntityManager);

			UnityEngine.Application.quitting += UnloadAll;
		}

		/// <summary>
		/// 親部分のみ生成
		/// </summary>
		/// <param name="id"></param>
		/// <param name="address"></param>
		/// <returns></returns>
		public async Task CreateHeaderEntities() {
			int rowCount = 0;
			int[] id = null;
			float3[] position = null;
			quaternion[] rotation = null;
			float3[] boundsMin = null;
			float3[] boundsMax = null;
			string[] name = null;
			await ResourceUtilityInternal.LoadTemp<LunarscapeFieldTable>(
				LunarscapeFieldTable.RES_ADDRESS,
				asset => {
					rowCount = asset.Rows.Length;
					id = new int[rowCount];
					position = new float3[rowCount];
					rotation = new quaternion[rowCount];
					boundsMin = new float3[rowCount];
					boundsMax = new float3[rowCount];
					name = new string[rowCount];
					LunarscapeFieldTable.Row[] rows = asset.Rows;
					for(int i = 0; i < rowCount; ++i) {
						id[i] = rows[i].id;
						position[i] = rows[i].position;
						rotation[i] = rows[i].rotation;
						boundsMin[i] = rows[i].boundsMin;
						boundsMax[i] = rows[i].boundsMax;
						name[i] = rows[i].name;

						_addressTable.TryAdd(rows[i].id, rows[i].address);
					}
				}
			);

			AsyncUtilityInternal.Post(
				() => {
					ECSUtilityInternal.ExecuteCommandBufferTemp(
						commandBuffer => {
							for(int i = 0; i < rowCount; ++i) {
								Entity entity = commandBuffer.CreateEntity(_archetype);
								commandBuffer.SetComponent(entity, LocalTransform.FromPositionRotation(position[i], rotation[i]));
								commandBuffer.SetComponent(entity, new LocalToWorld { Value = float4x4.identity, });
								commandBuffer.SetComponent(
									entity, 
									new LunarscapeFieldComponent {
										id = id[i],
										active = false,
										boundsMin = boundsMin[i],
										boundsMax = boundsMax[i],
									}
								);
								commandBuffer.SetDebugName(entity, name[i]);
							}
						}
					);
				}
			);
		}

		public async Task<FieldRes> RequestLoadField(int id) {
			// loadCacheで読込の順番を記憶しておく
			// loadCacheにidが存在すれば既にロード済なので再ロードは不要
			// 読込の有無に関係なく、指定したidをloadCacheの末尾に置くことで最後に読み込んだ扱いにする。

			FieldRes fieldRes;
			if (LoadCacheMoveToLatest(id)) {
				fieldRes = _fields[id];
			} else {
				// サブアセットはAddressablesで親からまとめて読み込めないので一個ずつ取得する必要がある
				string address = _addressTable[id];
				string guid = "";
				string[] subassetList = new string[0];
				await ResourceUtilityInternal.LoadTemp<FieldMeshAsset>(
					address,
					asset => {
						guid = asset.guid;
						subassetList = asset.subassets;
					}
				);
				
				UnityEngine.Mesh[] meshList = await ResourceUtilityInternal.LoadSubAssets<UnityEngine.Mesh>(address, subassetList);
				MeshInfo[] meshes = new MeshInfo[meshList.Length];

				AsyncUtilityInternal.Send(() => {
					for (int i = 0; i < meshList.Length; ++i) {
						BlobAssetReference<Collider> collider = MeshCollider.Create(meshList[i], _collisionFilter, _material);
						collider.Value.SetCollisionResponse(CollisionResponsePolicy.Collide);
						meshes[i] = new MeshInfo {
							collider = collider,
							name = meshList[i].name,
						};
					}
				});

				fieldRes = new FieldRes{ 
					guid = guid,
					meshes = meshes, 
				};
				_fields.TryAdd(id, fieldRes);

				AsyncUtilityInternal.Post(() => {
					AddToLoadCache(id);
				});
			}
			return fieldRes;
		}

		public void CreateMeshEntities(Entity rootEntity, int id, FieldRes fieldRes) {
			ECSUtilityInternal.ExecuteCommandBufferTemp(commandBuffer => {
				DynamicBuffer<LinkedEntityGroup> group = commandBuffer.SetBuffer<LinkedEntityGroup>(rootEntity);

				LunarscapeFieldMeshComponent fieldMeshComponent = new LunarscapeFieldMeshComponent { meshId = id, };
				LocalTransform localTransform = LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1.0f);
				LocalToWorld localToWorld = new LocalToWorld { Value = float4x4.identity, };
				PhysicsWorldIndex physicsWorldIndex = new PhysicsWorldIndex{ Value = LunarConst.ENABLED_PHYSICS_INDEX, };
				Parent parent = new Parent { Value = rootEntity, };
				for (int i = 0; i < fieldRes.meshes.Length; ++i) {
					Entity entity = commandBuffer.CreateEntity(_meshArchetype);
					commandBuffer.SetComponent(entity, fieldMeshComponent);
					commandBuffer.SetComponent(entity, new PhysicsCollider { Value = fieldRes.meshes[i].collider, });
					commandBuffer.SetComponent(entity, localTransform);
					commandBuffer.SetComponent(entity, localToWorld);
					commandBuffer.SetComponent(entity, parent);
					commandBuffer.AddSharedComponent(entity, physicsWorldIndex);
					commandBuffer.SetDebugName(entity, fieldRes.meshes[i].name);

					group.Add(new LinkedEntityGroup { Value = entity, });
				}
			});
		}

		public void UnloadAll() {
			foreach(int id in _fields.Keys) {
				foreach(MeshInfo mesh in _fields[id].meshes) {
					mesh.collider.Dispose();
				}
				ResourceUtilityInternal.Unload(_addressTable[id]);
			}
			_fields.Clear();
			_loadCache.Clear();
		}

		/// <summary>
		/// _loadCacheに追加。
		/// 追加時にサイズが超過する場合は先頭をアンロードする。
		/// </summary>
		/// <param name="id"></param>
		private void AddToLoadCache(int id) {
			if (_singletonQuery.TryGetSingleton(out SingletonLunarscapeField singleton)) {
				if (_loadCache.Count >= singleton.cacheFieldMeshSize) {
					UnloadFieldResource(_loadCache[0]);
					_loadCache.RemoveAt(0);
				}
				_loadCache.Add(id);
			} else {
				D.LogE("SingletonLunarscapeField not found.");
			}
		}

		/// <summary>
		/// _loadCacheに残っていれば末尾に移動する。
		/// なければ何もしない。
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		private bool LoadCacheMoveToLatest(int id) {
			if (_loadCache.Remove(id)) {
				_loadCache.Add(id);
				return true;
			} else {
				return false;
			}
		}

		private void UnloadFieldResource(int id) {
			if (_fields.TryGetValue(id, out FieldRes field)) {
				foreach(MeshInfo mesh in field.meshes) {
					mesh.collider.Dispose();
				}
				ResourceUtilityInternal.Unload(_addressTable[id]);

				// フィールドとセットのコンテンツのアンロード
				ECSUtilityInternal.ExecuteCommandBufferTemp(commandBuffer => {
					Entity requestEntity = commandBuffer.CreateEntity();
					commandBuffer.AddComponent(
						requestEntity, 
						new LunarscapeUnloadTableGroupRequest { guid = field.guid, }
					);
				});
				_fields.TryRemove(id, out FieldRes _);
			}
		}

	}
}