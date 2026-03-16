using Unity.Entities;

namespace skyclad {
	/// <summary>
	/// 指定したDioramaを配置する
	/// </summary>
	public struct RequestLoadDioramaComponent : IComponentData {
		/// <summary>
		/// Diorama ID
		/// </summary>
		public int id;
	}

	/// <summary>
	/// 指定したDioramaを片づける
	/// </summary>
	public struct RequestUnloadDioramaComponent : IComponentData {
		/// <summary>
		/// Diorama ID
		/// </summary>
		public int id;
	}

	/// <summary>
	/// Dioramaのロード要素を示すコンポーネント
	/// </summary>
	public struct DioramaLoadElementComponent : IComponentData {
		/// <summary>
		/// Diorama ID
		/// </summary>
		public int id;
	}

	/// <summary>
	/// Dioramaのアンロード要素を示すコンポーネント
	/// </summary>
	public struct DioramaUnloadElementComponent : IComponentData {
		/// <summary>
		/// Diorama ID
		/// </summary>
		public int id;
	}
}