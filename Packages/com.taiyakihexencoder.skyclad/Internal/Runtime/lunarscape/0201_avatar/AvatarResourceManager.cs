using System.Collections.Generic;
using System.Threading.Tasks;
using skyclad.internalProc;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace skyclad.lunarscape.internalProc {
	public static class AvatarResourceManager {
		private static Dictionary<int, BlobAssetReference<Collider>> _physicsColliders;

		private static LunarscapeAvatarPhysicsColliderTable _colliderTable;
		private static LunarscapeAvatarTable _avatarTable;

		private static EntityArchetype _archetype;

		private static Entity _prefabRoot;
		private static List<int> _generatedPrefabList;

		/// <summary>
		/// 読込済のLoadTableのグループ。
		/// Prefabを削除して良いか確認するために使う。
		/// </summary>
		private static Dictionary<string, IList<int>> _avatarLoadedTable;

		static AvatarResourceManager() {
			_physicsColliders = new Dictionary<int, BlobAssetReference<Collider>>();
			_avatarLoadedTable = new Dictionary<string, IList<int>>();
			UnityEngine.Application.quitting += UnloadTables;
		}

		public static void Init() {
			EntityManager entityManager = ECSUtilityInternal.EntityManager;
			_prefabRoot = entityManager.CreateEntity(
				ComponentType.ReadWrite<LocalTransform>(),
				ComponentType.ReadWrite<LocalToWorld>(),
				ComponentType.ReadWrite<Parent>(),
				ComponentType.ReadWrite<LunarscapeParentingRequest>()
			);
			entityManager.SetDebugName(_prefabRoot, "Prefabs");

			_archetype = ECSUtilityInternal.EntityManager.CreateArchetype(
				ComponentType.ReadOnly<Prefab>(),
				
				ComponentType.ReadWrite<Parent>(),
				ComponentType.ReadWrite<LocalTransform>(),
				ComponentType.ReadWrite<LocalToWorld>(),

				ComponentType.ReadWrite<PhysicsCollider>(),
				ComponentType.ReadWrite<PhysicsMass>(),
				ComponentType.ReadWrite<PhysicsVelocity>(),
				ComponentType.ReadWrite<PhysicsGravityFactor>(),

				ComponentType.ReadWrite<LunarscapeAvatar>()
			);

			_generatedPrefabList = new List<int>();
		}

		public static async Task LoadTables() {
			if (await ResourceUtilityInternal.Exists(LunarscapeAvatarPhysicsColliderTable.resourceAddress)) {
				_colliderTable = await ResourceUtilityInternal.Load<LunarscapeAvatarPhysicsColliderTable>(LunarscapeAvatarPhysicsColliderTable.resourceAddress);
			} else {
				D.LogW("LunarscapeAvatarPhysicsColliderTable not exists.");
			}

			if (await ResourceUtilityInternal.Exists(LunarscapeAvatarTable.resourceAddress)) {
				_avatarTable = await ResourceUtilityInternal.Load<LunarscapeAvatarTable>(LunarscapeAvatarTable.resourceAddress);
			} else {
				D.LogW("LunarscapeAvatarTable not exists.");
			}
		}

		private static bool TryGetPhysicsCollider(int id, out BlobAssetReference<Collider> blob) {
			if (! _physicsColliders.TryGetValue(id, out blob)) {
				if (_colliderTable.TryGetBlob(id, out blob)) {
					_physicsColliders.Add(id, blob);
					return true;
				} else {
					D.LogW($"Collider not exists. id={id}");
					return false;
				}
			} else {
				return true;
			}
		}

		internal static void UnloadTables() {
			_generatedPrefabList?.Clear();

			foreach(BlobAssetReference<Collider> blob in _physicsColliders.Values) {
				if (blob.IsCreated) { blob.Dispose(); }
			}

			if (_colliderTable != null) {
				_physicsColliders.Clear();
				_colliderTable = null;
				ResourceUtilityInternal.Unload(LunarscapeAvatarPhysicsColliderTable.resourceAddress);
			}

			if (_avatarTable != null) {
				_avatarTable = null;
				ResourceUtilityInternal.Unload(LunarscapeAvatarTable.resourceAddress);
			}
		}

		public static void CreatePrefab(int id) {
			if (!_generatedPrefabList.Contains(id)) {
				if (TryGetAvaterSetting(id, out LunarscapeAvatarTable.AvatarSettings setting)) {
					if (TryGetPhysicsCollider(setting.collider, out BlobAssetReference<Collider> collider)) {
						ECSUtilityInternal.ExecuteCommandBufferTemp(commandBuffer => {
							Entity entity = commandBuffer.CreateEntity(_archetype);
							commandBuffer.SetComponent(entity, new PhysicsCollider { Value = collider, });
							commandBuffer.SetComponent(entity, new Parent{ Value = _prefabRoot, });
							commandBuffer.SetComponent(entity, LocalTransform.FromPosition(float3.zero));
							commandBuffer.SetComponent(entity, new LocalToWorld{ Value = float4x4.identity, });
							commandBuffer.SetComponent(entity, new PhysicsGravityFactor { Value = 1f, });
							commandBuffer.SetComponent(entity, PhysicsMass.CreateDynamic(MassProperties.UnitSphere, 50.0f));
							commandBuffer.SetComponent(entity, new LunarscapeAvatar { id = setting.id, });
							commandBuffer.AddSharedComponent(entity, new PhysicsWorldIndex { Value = LunarConst.ENABLED_PHYSICS_INDEX, });
							commandBuffer.SetDebugName(entity, setting.name);
						});

						_generatedPrefabList.Add(id);
					}
				} else {
					D.LogW($"id = {id} is not found.");
				}
			}
		}

		public static void AddLoadPrefabTable(string guid, IList<int> list) {
			if (! _avatarLoadedTable.ContainsKey(guid)) {
				_avatarLoadedTable.Add(guid, list);
			}
		}

		public static void RemoveLoadPrefabTable(string guid) {
			if (_avatarLoadedTable.TryGetValue(guid, out IList<int> avatarIds)) {
				// テーブルを削除
				_avatarLoadedTable.Remove(guid);

				// ほかで読込がされていないprefabのリストを取得
				List<int> removeIds = new List<int>(avatarIds);

				foreach(IList<int> ids in _avatarLoadedTable.Values) {
					foreach(int id in ids) {
						removeIds.Remove(id);
					}
				}

				// 不要になったprefabを削除
				ECSUtilityInternal.ExecuteCommandBufferTemp(commandBuffer => {
					foreach(int avatarId in removeIds) {
						Entity entity = commandBuffer.CreateEntity();
						commandBuffer.AddComponent(entity, new LunarscapeAvatarDeletePrefabRequest { id = avatarId, });
					}
				});
			}
		}

		private static bool TryGetAvaterSetting(int id, out LunarscapeAvatarTable.AvatarSettings data) {
			foreach(LunarscapeAvatarTable.AvatarSettings setting in _avatarTable.Settings) {
				if (setting.id == id) {
					data = setting;
					return true;
				}
			}
			data = default;
			return false;
		}
	}
}