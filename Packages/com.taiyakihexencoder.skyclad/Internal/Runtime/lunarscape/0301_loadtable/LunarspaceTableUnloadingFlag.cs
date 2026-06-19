using Unity.Entities;

namespace skyclad.lunarscape.internalProc {
	/// <summary>
	/// フィールドデータの破棄中フラグ
	/// </summary>
	public struct LunarscapeTableUnloadingFlag : IComponentData { 
		public int id;
	}
}