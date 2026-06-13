using Unity.Entities;

namespace skyclad.lunarscape.internalProc {
	/// <summary>
	/// フィールド読み込みの基準となる地点情報
	/// 位置情報はLocalToWorldを読み取る
	/// </summary>
	public partial struct LunarscapeFieldObservePoint : IComponentData { }
}