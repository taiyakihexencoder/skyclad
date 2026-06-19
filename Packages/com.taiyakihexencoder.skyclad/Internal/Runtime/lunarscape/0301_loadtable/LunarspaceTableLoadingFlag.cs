using Unity.Entities;

namespace skyclad.lunarscape.internalProc {
	/// <summary>
	/// フィールドデータの読み込み中フラグ
	/// </summary>
	public struct LunarscapeTableLoadingFlag : IComponentData {
		public int id;
	}
}