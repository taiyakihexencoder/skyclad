using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace skyclad.bullet {
	public struct BulletGroup : IComponentData {
		public int groupId;
		public int loadCounter;
	}

	public struct Bullet : IComponentData {
		public int bulletId;
		public BlobAssetReference<SkycladDataBlob<BulletParameter>> master;
		public int masterIndex;
		public float elapsedSeconds;
		public BulletStyle style;
	}

	/// <summary>
	/// Bullet Prefabのロード
	/// </summary>
	public struct RequestLoadBulletGroupPrefabComponent : IComponentData {
		public int groupId;
		public int dioramaId;
		public FixedString64Bytes name;
		public uint hitBoxLayer;
		public uint hitLayer;
	}

	/// <summary>
	/// 個別のBulletのロード
	/// </summary>
	public struct LoadBulletBufferElement : IBufferElementData {
		public int bulletId;
		public FixedString64Bytes name;
		public int dataIndex;
		public BulletHitBoxType hitBoxType;
		public float3 extent;
		public BulletStyle style;
	}

	/// <summary>
	/// Bullet Prefabのアンロード
	/// </summary>
	public struct RequestUnloadBulletGroupPrefabComponent : IComponentData {
		public int groupId;
	}

	/// <summary>
	/// PrefabからBulletを生成
	/// </summary>
	public struct RequestCreateBullet : IComponentData {
		public int bulletId;
		public float3 position;
		public quaternion rotation;
	}
}