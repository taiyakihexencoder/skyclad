using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace skyclad {
	public struct RequestLoadCharacterPrefabComponent : IComponentData {
		public int characterId;
		public FixedString64Bytes name;
		public int statusIndex;
		public BlobAssetReference<Collider> collider;
		public bool hasController;
		public int dioramaId;
	}

	public struct RequestCharacterPrefabHitBox : IBufferElementData {
		public float3 extent;
		public float3 offset;
	}

	public struct RequestUnloadCharacterPrefabComponent : IComponentData {
		public int characterId;
		public int dioramaId;
	}

	/// <summary>
	/// RequestLoadCharacterPrefabとセットで設定し、
	/// Prefabが作られたらSpawn Requestをする
	/// </summary>
	public struct SpawnAfterCreatePrefabBufferElement : IBufferElementData {
		public float3 position;
		public quaternion rotation;
	}

	/// <summary>
	/// キャラクターのSpawn時にインスタンスに直接持たせるパラメーター
	/// </summary>
	public struct CharacterSpawnParameterElement : IComponentData {
		/// <summary>
		/// 生成元のDiorama
		/// </summary>
		public int dioramaId;
	}

	/// <summary>
	/// RequestLoadCharacterPrefabGroupによってロードされている回数
	/// 0になったらロードされていないので削除できる
	/// </summary>
	public struct CharacterPrefabLoadCounterComponent : IComponentData, ICleanupComponentData {
		public int characterId;
		public int loadCounter;
	}

	/// <summary>
	/// ヒットボックス
	/// </summary>
	public struct CharacterHitBox : IComponentData, IEnableableComponent {
		
	}

	/// <summary>
	/// キャラクターステータスのマスターデータへの参照
	/// </summary>
	public struct CharacterStatusMasterReference : IComponentData {
		public BlobAssetReference<SkycladDataBlob<CharacterStatus>> blob;
		public int index;
	}

	/// <summary>
	/// 自動生成されるがスクリプト内で使用するのでpartial定義しておく
	/// </summary>
	public partial struct CharacterStatusTableExists : IComponentData { }
}