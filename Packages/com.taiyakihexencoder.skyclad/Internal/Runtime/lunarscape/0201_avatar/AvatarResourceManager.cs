using System.Collections.Generic;
using System.Threading.Tasks;
using skyclad.internalProc;
using Unity.Entities;
using Unity.Physics;

namespace skyclad.lunarscape.internalProc {
	public static class AvatarResourceManager {
		private static Dictionary<int, BlobAssetReference<Collider>> _physicsColliders;

		private static LunarscapeAvatarPhysicsColliderTable _table;

		static AvatarResourceManager() {
			_physicsColliders = new Dictionary<int, BlobAssetReference<Collider>>();
			UnityEngine.Application.quitting += UnloadTables;
		}

		public static async Task LoadTables() {
			if (await ResourceUtilityInternal.Exists(LunarscapeAvatarPhysicsColliderTable.resourceAddress)) {
				_table = await ResourceUtilityInternal.Load<LunarscapeAvatarPhysicsColliderTable>(LunarscapeAvatarPhysicsColliderTable.resourceAddress);
			} else {
				D.LogW("LunarscapeAvatarPhysicsColliderTable not exists.");
			}
		}

		public static BlobAssetReference<Collider> GetPhysicsCollider(int index) {
			if (! _physicsColliders.TryGetValue(index, out BlobAssetReference<Collider> collider)) {
				collider = _table.GetAsBlob(index);
				_physicsColliders.Add(index, collider);
			}
			return collider;
		}

		internal static void UnloadTables() {
			foreach(BlobAssetReference<Collider> blob in _physicsColliders.Values) {
				if (blob.IsCreated) { blob.Dispose(); }
			}

			if (_table != null) {
				_physicsColliders.Clear();
				_table = null;
				ResourceUtilityInternal.Unload(LunarscapeAvatarPhysicsColliderTable.resourceAddress);
			}
		}
	}
}