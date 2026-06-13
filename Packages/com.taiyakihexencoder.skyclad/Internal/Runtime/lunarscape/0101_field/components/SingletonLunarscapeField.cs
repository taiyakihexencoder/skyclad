using Unity.Entities;

namespace skyclad.lunarscape.internalProc {
	public struct SingletonLunarscapeField : IComponentData {
		/// <summary>
		/// 領域のふちからこの距離を読込距離とする
		/// </summary>
		public float loadFieldDistance;

		/// <summary>
		/// 領域のふちからこの距離離れたら破棄する
		/// </summary>
		public float unloadFieldDistance;

		/// <summary>
		/// フィールドメッシュのキャッシュ数
		/// キャッシュ数を超過すると古いロードデータから削除される
		/// </summary>
		public int cacheFieldMeshSize;
	}
}