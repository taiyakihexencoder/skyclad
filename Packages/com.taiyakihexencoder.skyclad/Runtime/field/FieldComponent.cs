using Unity.Entities;

namespace skyclad.field {
	/// <summary>
	/// メッシュ読込リクエスト
	/// </summary>
	public struct RequestLoadFieldMeshComponent : IComponentData {
		public int meshId;
	}

	/// <summary>
	/// メッシュアンロードリクエスト
	/// </summary>
	public struct RequestUnloadFieldMeshComponent : IComponentData {
		public int meshId;
	}

	/// <summary>
	/// エンティティ識別用コンポーネント
	/// </summary>
	public struct FieldMeshComponent : IComponentData {
		public int meshId;
	}
}